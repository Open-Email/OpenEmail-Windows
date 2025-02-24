using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain;
using OpenEmail.Domain.Exceptions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.Core.Services
{
    public class MessageUploader : IMessageUploader
    {
        public ObservableCollection<MessageUploadProgress> MessageUploadQueue { get; } = [];

        private readonly IMessagesService _messagesService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly ILinksService _linksService;
        private readonly IClientFactory _clientFactory;
        private readonly IDialogService _dialogService;
        private readonly IAttachmentManager _attachmentManager;

        public MessageUploader(IMessagesService messagesService,
                               IApplicationStateService applicationStateService,
                               ILinksService linksService,
                               IClientFactory clientFactory,
                               IDialogService dialogService,
                               IAttachmentManager attachmentManager)
        {
            _messagesService = messagesService;
            _applicationStateService = applicationStateService;
            _linksService = linksService;
            _clientFactory = clientFactory;
            _dialogService = dialogService;
            _attachmentManager = attachmentManager;
        }

        public async Task UploadMessageAsync(Guid rootMessageId, List<ReaderUploadData> readersMap, IPlatformDispatcher platformDispatcher)
        {
            var message = await _messagesService.GetMessageAsync(rootMessageId).ConfigureAwait(false);

            if (message == null) return;

            var progress = MessageUploadQueue.FirstOrDefault(x => x.MessageId == rootMessageId);

            if (progress != null)
            {
                if (progress.Status == MessageStatus.UploadingFailed)
                {
                    // Remove the progress and try again.
                    platformDispatcher.ExecuteOnDispatcher(() => MessageUploadQueue.Remove(progress));
                }
                else if (progress.Status == MessageStatus.Uploading || progress.Status == MessageStatus.Completed)
                {
                    platformDispatcher.ExecuteOnDispatcher(() => _dialogService.ShowInfoBarMessage("Message already uploaded", "The message has already been uploaded or is still uploading.", InfoBarMessageSeverity.Warning));

                    return;
                }
            }

            try
            {
                platformDispatcher.ExecuteOnDispatcher(() => _dialogService.ShowInfoBarMessage("Sending", "Sending your message...", InfoBarMessageSeverity.Info, autoDismiss: false));

                var payloadSeal = new PayloadSeal(CryptoConstants.SYMMETRIC_CIPHER);

                // Get attachments for the message.

                var attachments = await _messagesService.GetMessageAttachmentsAsync(message.EnvelopeId).ConfigureAwait(false);

                // Attachments + root envelope.
                int totalParts = attachments.Count + 1;

                progress = MessageUploadProgress.CreateNew(rootMessageId, message.Subject, totalParts);

                platformDispatcher.ExecuteOnDispatcher(() => MessageUploadQueue.Insert(0, progress));

                if (attachments.Any())
                {
                    foreach (var attachment in attachments)
                    {
                        // Prepare attachment payload.

                        var attachmentContent = _attachmentManager.GetAttachmentChunkBytes(attachment);

                        var attachmentPayload = new MessageUploadPayload(attachment.Id,
                                                                         message.Author,
                                                                         attachment.Size.ToString(),
                                                                         message.Category,
                                                                         message.CreatedAt,
                                                                         message.Subject,
                                                                         message.SubjectId,
                                                                         attachment.ParentId,
                                                                         message.Readers,
                                                                         attachmentContent,
                                                                         readersMap,
                                                                         null,
                                                                         _applicationStateService.ActiveProfile,
                                                                         payloadSeal);
                        byte[] attachmentUploadContent = null;

                        if (message.IsBroadcast)
                        {
                            // No need for encryption. The message body is plain text for broadcast messages.
                            attachmentUploadContent = attachmentContent;
                        }
                        else
                        {
                            // Encrypt the message body using the envelope access key.
                            attachmentUploadContent = CryptoUtils.EncryptSymmetric(attachmentContent, attachmentPayload.MessageAccessKey);
                        }

                        try
                        {
                            await UploadMessageAsync(attachmentPayload, attachmentUploadContent).ConfigureAwait(false);
                        }
                        catch (MessageConflictException)
                        {
                            // Some parts of the attachment might've already been uploaded.
                            // We don't need to react to this because they'll be merged once everything is sent.
                        }

                        platformDispatcher.ExecuteOnDispatcher(() => progress.CurrentPart += 1);
                    }
                }

                var envelope = new MessageUploadPayload(message.EnvelopeId,
                                                        message.Author,
                                                        message.Size.ToString(),
                                                        message.Category,
                                                        message.CreatedAt,
                                                        message.Subject,
                                                        message.SubjectId,
                                                        string.Empty,
                                                        message.Readers,
                                                        Encoding.ASCII.GetBytes(message.Body),
                                                        readersMap,
                                                        attachments,
                                                        _applicationStateService.ActiveProfile,
                                                        payloadSeal);

                byte[] uploadContent = null;

                if (message.IsBroadcast)
                {
                    // No need for encryption. The message body is plain text for broadcast messages.
                    uploadContent = Encoding.UTF8.GetBytes(message.Body);
                }
                else
                {
                    // Encrypt the message body using the envelope access key.
                    uploadContent = CryptoUtils.EncryptSymmetric(Encoding.UTF8.GetBytes(message.Body), envelope.MessageAccessKey);
                }

                platformDispatcher.ExecuteOnDispatcher(() => progress.CurrentPart += 1);

                await UploadMessageAsync(envelope, uploadContent).ConfigureAwait(false);

                // Create notification for each reader except myself.

                var authorAddress = UserAddress.CreateFromAddress(_applicationStateService.ActiveProfile.Address);

                foreach (var reader in readersMap)
                {
                    if (reader.Address == _applicationStateService.ActiveProfile.Address) continue;

                    var readerAddress = UserAddress.CreateFromAddress(reader.Address);

                    var link = AccountLink.Create(authorAddress, readerAddress);

                    await _linksService.CreateNotificationAsync(link, readerAddress, authorAddress, reader).ConfigureAwait(false);
                }

                platformDispatcher.ExecuteOnDispatcher(() =>
                {
                    // Add completed progress.
                    progress.Status = MessageStatus.Completed;

                    // Remove the progress from the queue and re-add.
                    MessageUploadQueue.Remove(progress);
                    MessageUploadQueue.Insert(0, progress);

                    _dialogService.ShowInfoBarMessage("Sent", "Your message has been sent successfully.", InfoBarMessageSeverity.Success);
                });

                // Remove root message and attachments.
                await _messagesService.DeleteMessagePermanentAsync(rootMessageId).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new TriggerSynchronizationMessage());

                Debug.WriteLine($"Message envelope uploaded. {message.Subject}");
            }
            catch (MessageConflictException)
            {
                // Root message has already been uploaded.
                // TODO: Delete the draft and sync outbox.
            }
            catch (Exception ex)
            {
                // Failed to upload.
                platformDispatcher.ExecuteOnDispatcher(() =>
                {
                    progress.Status = MessageStatus.UploadingFailed;

                    // Remove the progress from the queue and re-add.
                    MessageUploadQueue.Remove(progress);
                    MessageUploadQueue.Insert(0, progress);

                    _dialogService.ShowInfoBarMessage("Failed to send",
                                                  $"Failed to send the message. Please try again.\n{ex.Message}",
                                                  InfoBarMessageSeverity.Error,
                                                  autoDismiss: false);
                });

                // TODO: Log errors.
                Debug.WriteLine($"Uploading message envelope failed. {ex.Message}");
            }
        }

        private async Task UploadMessageAsync(MessageUploadPayload envelope, byte[] uploadContent)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();

            var headerCollection = envelope.AsDictionary();

            headerCollection.Add("Content-Type", "application/octet-stream");

            var byteArrayContent = new ByteArrayContent(uploadContent);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await messagesClient.SendMessageAsync(UserAddress.CreateFromAddress(envelope.Author), byteArrayContent, headerCollection);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict) throw new MessageConflictException();

            response.EnsureSuccessStatusCode();
        }
    }
}
