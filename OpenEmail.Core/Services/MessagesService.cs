using System.Diagnostics;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Core.DataServices;
using OpenEmail.Core.Helpers;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.MessageEnvelope;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.PubSubMessages;
using SQLite;

namespace OpenEmail.Core.Services
{
    public class MessagesService : BaseDataService, IMessagesService
    {
        private readonly IClientFactory _clientFactory;
        private readonly ILinksService _linksService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IAttachmentManager _attachmentManager;
        private readonly IProfileDataService _profileDataService;
        private readonly IPreferencesService _preferencesService;
        private readonly IPublicClientService _publicClientService;

        public MessagesService(IDatabaseService<ISQLiteAsyncConnection> databaseService,
                               IClientFactory clientFactory,
                               ILinksService linksService,
                               IApplicationStateService applicationStateService,
                               IAttachmentManager attachmentManager,
                               IProfileDataService profileDataService,
                               IPreferencesService preferencesService,
                               IPublicClientService publicClientService) : base(databaseService)
        {
            _clientFactory = clientFactory;
            _linksService = linksService;
            _applicationStateService = applicationStateService;
            _attachmentManager = attachmentManager;
            _profileDataService = profileDataService;
            _preferencesService = preferencesService;
            _publicClientService = publicClientService;
        }

        public async Task<string[]> GetBroadcastMessageIdsAsync(UserAddress address)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            var messageIdsResponse = await messagesClient.GetBroadcastMessagesAsync(address);

            var messagesContent = await messageIdsResponse.Content.ReadAsStringAsync();

            // TODO: Handle empty messages
            if (string.IsNullOrEmpty(messagesContent)) return null;

            // Each line corresponds to a message
            return messagesContent.Split('\n');
        }

        private Task ResetDeliveryInformationAsync(string envelopeId) => Connection.ExecuteAsync($"DELETE FROM {nameof(MessageDeliveryInformation)} WHERE EnvelopeId = ?", envelopeId);

        public async Task HandleDeliveryInformationAsync(string envelopeId)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            var deliveryInformations = new List<DeliveryInfo>();
            var deliveryInfoResponse = await messagesClient.FetchMessageDeliveriesAsync(_applicationStateService.ActiveProfile.Account.Address, envelopeId);

            if (deliveryInfoResponse.IsSuccessStatusCode)
            {
                var deliveryInfoContent = await deliveryInfoResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(deliveryInfoContent)) return;

                var splitted = deliveryInfoContent.Split('\n');

                foreach (var item in splitted)
                {
                    deliveryInformations.Add(DeliveryInfo.FromString(item));
                }

                // Clear existing delivery information.
                await ResetDeliveryInformationAsync(envelopeId);
            }

            var dbDeliveryInformations = deliveryInformations.Select(a => new MessageDeliveryInformation
            {
                EnvelopeId = envelopeId,
                Link = a.Link,
                SeenAt = a.SeenAt,
                Id = Guid.NewGuid()
            }).ToList();

            await Connection.InsertAllAsync(dbDeliveryInformations).ConfigureAwait(false);

            var message = await GetMessageByEnvelopeIdAsync(envelopeId);

            if (message != null && !string.IsNullOrEmpty(message.Readers))
            {
                var readers = message.Readers.Split('\n').ToList();

                foreach (var reader in readers)
                {
                    var readerUserAddress = UserAddress.CreateFromAddress(reader);
                    var accountLink = AccountLink.Create(readerUserAddress, _applicationStateService.ActiveProfile.UserAddress);

                    var deliveryInfo = dbDeliveryInformations.FirstOrDefault(a => a.Link == accountLink.Link);

                    if (deliveryInfo == null)
                    {
                        // This user must be notified again.
                        var readerProfile = await _profileDataService.GetProfileDataAsync(readerUserAddress);

                        if (readerProfile == null)
                        {
                            Debug.WriteLine($"Can't find the profile data for {reader}");
                            continue;
                        }

                        var uploadData = readerProfile.CreateUploadPayload(reader);

                        await _linksService.CreateNotificationAsync(accountLink, readerUserAddress, _applicationStateService.ActiveProfile.UserAddress, uploadData)
                                           .ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task<string[]> GetEnvelopeIdsAsync(UserAddress address, AccountLink link)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            HttpResponseMessage messageIdsResponse = null;

            if (link != null)
            {
                messageIdsResponse = await messagesClient.GetMessageIdsAsync(address, link);
            }
            else
            {
                messageIdsResponse = await messagesClient.GetAuthoredMessagesAsync(address);
            }

            var messagesContent = await messageIdsResponse.Content.ReadAsStringAsync();

            if (!messageIdsResponse.IsSuccessStatusCode) return null;

            // TODO: Handle empty messages
            if (string.IsNullOrEmpty(messagesContent)) return null;

            // Each line corresponds to a message
            return messagesContent.Split('\n').Distinct().ToArray();
        }

        public async Task<AttachmentContentEnvelope> GetAttachmentContentEnvelopeAsync(UserAddress toAddress, AccountLink link, EnvelopeFile envelopeFile)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            HttpResponseMessage contentResponse = null;

            if (link != null)
            {
                contentResponse = await messagesClient.GetMessageContentAsync(toAddress, envelopeFile.Id, link);
            }
            else
            {
                contentResponse = await messagesClient.GetMessageContentAsync(toAddress, envelopeFile.Id);
            }

            if (!contentResponse.IsSuccessStatusCode)
            {
                var error = await contentResponse.Content.ReadAsStringAsync();
                Debugger.Break();
                return null;
            }

            var headerString = string.Join("\n", contentResponse.Headers.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
            var envelopeBase = new EnvelopeBase(headerString, _applicationStateService.ActiveProfile, link);

            var content = await contentResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return new AttachmentContentEnvelope(envelopeBase, content);
        }

        public async Task HandleMessageHeadersAsync(HttpResponseMessage headersResponse, string messageId, UserAddress accountAddress, AccountLink link)
        {
            if (!headersResponse.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Failed to handle message {messageId} ({headersResponse.StatusCode})");

                return;
            }

            var headerString = string.Join("\n", headersResponse.Headers.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
            var envelopeBase = new EnvelopeBase(headerString, _applicationStateService.ActiveProfile, link);

            bool isAttachmentEnvelope = envelopeBase.EnvelopeHeaderStore.HasKey("parent-id");

            // If the envelope is an attachment, we don't download the content.
            // MessageAttachments will be generated when attachment envelope arrives, or root message is downloaded
            // Root message doesn't have access key of the attachment, therefore access key for the attachment will be updated
            // when attachment envelope arrives. If not, it'll be created and later ignored by root message.

            if (isAttachmentEnvelope)
            {
                var attachmentEnvelope = new AttachmentEnvelope(envelopeBase);

                // AttachmentGroupId is used to group attachments that belong to the same message.
                // Attachments can have multiple parts. However, 1 message can have the same file name once.
                // Therefore, we group attachments by the parent message id and the file name.

                var attachmentGroupId = await GetAttachmentGroupIdAsync(attachmentEnvelope.ParentId, attachmentEnvelope.Envelope.EnvelopeHeaderStore.GetData<string>("name"));
                var attachmentModel = await GetMessageAttachmentByAttachmentIdAsync(attachmentEnvelope.Envelope.Id).ConfigureAwait(false);

                if (attachmentModel == null)
                {
                    attachmentModel = attachmentEnvelope.AsEntity();
                    attachmentModel.AttachmentGroupId = attachmentGroupId;

                    await Connection.InsertAsync(attachmentModel).ConfigureAwait(false);
                }
                else if (attachmentModel != null && string.IsNullOrEmpty(attachmentModel.AccessKey))
                {
                    // Update access key for the attachment if available.
                    // Broadcast attachments don't have it.

                    if (envelopeBase.AccessKey != null)
                    {
                        attachmentModel.AccessKey = Convert.ToBase64String(envelopeBase.AccessKey);
                    }

                    attachmentModel.AttachmentGroupId = attachmentGroupId;

                    await Connection.UpdateAsync(attachmentModel);
                }
            }
            else
            {
                var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();

                // Message envelope. Download the content.
                // If there are files, and they fall under the allowed limit for auto-download, download them as well.

                HttpResponseMessage contentResponse = null;

                if (link != null)
                {
                    contentResponse = await messagesClient.GetMessageContentAsync(accountAddress, messageId, link).ConfigureAwait(false);
                }
                else
                {
                    contentResponse = await messagesClient.GetMessageContentAsync(accountAddress, messageId).ConfigureAwait(false);
                }

                if (!contentResponse.IsSuccessStatusCode)
                {
                    var error = await headersResponse.Content.ReadAsStringAsync();
                    Debugger.Break();

                    return;
                }

                var content = await contentResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var messageEnvelope = MessageEnvelope.FromEnvelope(envelopeBase, content);

                var savedMessage = await SaveMessageEnvelopeAsync(messageEnvelope).ConfigureAwait(false);

                if (savedMessage == null) return;

                // Check if the attachments belong to the message is under the size limit.
                bool hasFiles = messageEnvelope.Files != null && messageEnvelope.Files.Any();

                if (hasFiles)
                {
                    var envelopeFilesToDownload = messageEnvelope.Files.Where(f => f.Size < _preferencesService.DownloadAttachmentLimitInMegabytes * 1024 * 1024);

                    foreach (var envelopeFile in envelopeFilesToDownload)
                    {
                        var attachment = await GetMessageAttachmentByAttachmentIdAsync(envelopeFile.Id).ConfigureAwait(false);

                        var attachmentContentEnvelope = await GetAttachmentContentEnvelopeAsync(accountAddress, link, envelopeFile).ConfigureAwait(false);
                        await _attachmentManager.SaveAttachmentEnvelopeAsync(attachment, attachmentContentEnvelope.Content).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task HandleMessageAsync(UserAddress toAddress, AccountLink link, string messageId)
        {
            // Check if message exists.
            var exists = await IsMessageExistsAsync(messageId).ConfigureAwait(false);

            if (exists) return;

            // Also check if the attachment exists but validate if all fields are available to get the content later on.
            var existingAttachment = await GetMessageAttachmentByAttachmentIdAsync(messageId).ConfigureAwait(false);

            if (existingAttachment != null && existingAttachment.IsValid())
                return;

            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            HttpResponseMessage headersResponse = null;

            if (link != null)
            {
                headersResponse = await messagesClient.GetMessageHeadersAsync(toAddress, messageId, link);
            }
            else
            {
                headersResponse = await messagesClient.GetMessageHeadersAsync(toAddress, messageId);
            }

            await HandleMessageHeadersAsync(headersResponse, messageId, toAddress, link).ConfigureAwait(false);
        }

        public async Task<bool> IsMessageExistsAsync(string envelopeId)
        {
            var message = await Connection?.Table<Message>().Where(m => m.EnvelopeId == envelopeId).FirstOrDefaultAsync();

            return message != null;
        }

        public async Task<bool> IsMessageAttachmentExistsAsync(string attachmentId)
        {
            var messageAttachment = await Connection.Table<MessageAttachment>().Where(m => m.Id == attachmentId).FirstOrDefaultAsync();

            return messageAttachment != null;
        }

        public async Task<MessageAttachment> GetMessageAttachmentByAttachmentIdAsync(string attachmentId)
            => await Connection.Table<MessageAttachment>().Where(m => m.Id == attachmentId).FirstOrDefaultAsync();

        public Task<List<MessageAttachment>> GetMessageAttachmentsAsync(string parentMessageId)
            => Connection.Table<MessageAttachment>().Where(m => m.ParentId == parentMessageId).OrderBy(a => a.Part).ToListAsync();

        public async Task<Guid> GetAttachmentGroupIdAsync(string parentMessageId, string fileName)
        {
            var existingAttachment = await Connection.Table<MessageAttachment>().Where(m => m.ParentId == parentMessageId && m.FileName == fileName).FirstOrDefaultAsync();

            if (existingAttachment == null) return Guid.NewGuid();

            return existingAttachment.AttachmentGroupId;
        }

        private async Task<Message> SaveMessageEnvelopeAsync(MessageEnvelope envelope)
        {
            bool exists = await IsMessageExistsAsync(envelope.Envelope.Id).ConfigureAwait(false);

            if (exists) return null;

            var message = envelope.AsEntity();

            // Authored messages are always read.
            if (_applicationStateService.ActiveProfile.Address == message.Author)
            {
                message.IsRead = true;
            }

            // Save the actual message.
            await Connection.InsertAsync(message).ConfigureAwait(false);

            // Save attachment parts if any.
            if (envelope.Files != null)
            {
                // Get attachment parts.
                var attachments = envelope.Files.AsEntity(message.EnvelopeId).ToList();

                // Attachment envelope may arrive before or after the root message.
                // Just to be on the safe side, we check if the attachment exists.
                // If it does, we skip it here because message envelope doesn't have access key for the attachment.
                // AccessKey will be saved when the attachment envelope arrives.

                foreach (var attachment in attachments)
                {
                    // Attachment may have been added before without the part.
                    // If exists just update the part,filename and mime type coming from the Files header.
                    // If doesn't, add it without the access key.

                    var existingAttachment = await GetMessageAttachmentByAttachmentIdAsync(attachment.Id).ConfigureAwait(false);

                    if (existingAttachment == null)
                    {
                        existingAttachment = attachment;
                        await Connection.InsertAsync(attachment).ConfigureAwait(false);
                    }
                    else
                    {
                        existingAttachment.Part = attachment.Part;
                        existingAttachment.FileName = attachment.FileName;
                        existingAttachment.MimeType = attachment.MimeType;

                        await Connection.UpdateAsync(existingAttachment).ConfigureAwait(false);
                    }

                }
            }

            WeakReferenceMessenger.Default.Send(new MessageAdded(message));

            return message;
        }

        public Task<List<Message>> GetMessagesAsync(Expression<Func<Message, bool>> filter)
        {
            return Connection
                .Table<Message>()
                .Where(filter)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public Task<Message> GetMessageAsync(Guid messageId)
            => Connection.Table<Message>().Where(m => m.Id == messageId).FirstOrDefaultAsync();

        private Task<Message> GetMessageByEnvelopeIdAsync(string envelopeId)
            => Connection.Table<Message>().Where(m => m.EnvelopeId == envelopeId).FirstOrDefaultAsync();

        public Task MarkMessageReadAsync(Guid messageId)
        {
            return Connection.ExecuteAsync("UPDATE Message SET IsRead = 1 WHERE Id = ?", messageId);
        }

        public async Task DeleteMessagePermanentAsync(Guid messageId)
        {
            var message = await Connection.Table<Message>().Where(m => m.Id == messageId).FirstOrDefaultAsync();

            if (message == null) return;

            await Connection.RunInTransactionAsync(async (connection) =>
            {
                await Connection.Table<MessageDeliveryInformation>().Where(a => a.EnvelopeId == message.EnvelopeId).DeleteAsync().ConfigureAwait(false);
                await Connection.Table<MessageAttachment>().Where(a => a.ParentId == message.EnvelopeId).DeleteAsync().ConfigureAwait(false);

                await Connection.DeleteAsync(message).ConfigureAwait(false);
            });

            WeakReferenceMessenger.Default.Send(new MessageDeleted(message));
        }

        public async Task DeleteMessageToTrashAsync(Guid messageId)
        {
            var message = await Connection.Table<Message>().Where(m => m.Id == messageId).FirstOrDefaultAsync();

            if (message == null) return;

            // Doing a soft delete so Trash folder can list the items.
            message.IsDeleted = true;
            message.DeletedAt = DateTimeOffset.Now;

            await Connection.UpdateAsync(message).ConfigureAwait(false);

            WeakReferenceMessenger.Default.Send(new MessageDeleted(message));
        }

        public async Task RecallMessageByEnvelopeId(string envelopeId)
        {
            var messagesClient = _clientFactory.CreateProfileClient<IMessagesClient>();
            var response = await messagesClient
                .RecallAuthoredMessageAsync(_applicationStateService.ActiveProfile.Account.Address, envelopeId)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
        }

        public async Task RecallMessageAsync(Guid messageId)
        {
            var message = await Connection.Table<Message>().Where(m => m.Id == messageId).FirstOrDefaultAsync();

            if (message == null) return;

            List<string> recallEnvelopeIds = new List<string> { message.EnvelopeId };

            // Check if there are any attachments.
            var attachments = await GetMessageAttachmentsAsync(message.EnvelopeId).ConfigureAwait(false);

            if (attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    recallEnvelopeIds.Add(attachment.Id);
                }
            }

            // Safely try to recall the envelopes.

            try
            {
                foreach (var envelopeId in recallEnvelopeIds)
                {
                    await RecallMessageByEnvelopeId(message.EnvelopeId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to recall the message. {ex.Message}");
            }

            // Hard delete the message.
            await DeleteMessagePermanentAsync(messageId).ConfigureAwait(false);

            WeakReferenceMessenger.Default.Send(new MessageDeleted(message));
        }


        public async Task UpdateMessageAsync(Message message)
        {
            var isExists = await IsMessageExistsAsync(message.EnvelopeId).ConfigureAwait(false);

            if (isExists)
            {
                await Connection.UpdateAsync(message).ConfigureAwait(false);
                WeakReferenceMessenger.Default.Send(new MessageUpdated(message));
            }
            else
            {
                await Connection.InsertAsync(message).ConfigureAwait(false);
                WeakReferenceMessenger.Default.Send(new MessageAdded(message));
            }
        }

        public List<Tuple<MessageAttachment, byte[]>> CreateMessageAttachments(Message rootMessage, string filePath)
        {
            var attachments = new List<Tuple<MessageAttachment, byte[]>>();

            if (!File.Exists(filePath)) return default;

            var file = File.ReadAllBytes(filePath);

            // Split files into parts, each of them can have maximum 64 mb.
            var splittedFileData = ByteHelper.SplitByteArray(file);

            // Some properties are shared among attachment parts.

            var fileName = Path.GetFileName(filePath);
            var mimeType = "pdf"; // TODO: Get mime type from file extension.
            var attachmentGroupId = Guid.NewGuid();

            for (int i = 0; i < splittedFileData.Count; i++)
            {
                // Create attachment for each part.

                var attachment = new MessageAttachment
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentId = rootMessage.EnvelopeId,
                    FileName = fileName,
                    MimeType = mimeType,
                    Size = splittedFileData[i].Length,
                    AccessKey = rootMessage.AccessKey,
                    AttachmentGroupId = attachmentGroupId,
                    ModifiedAt = DateTimeOffset.Now,
                    Part = i + 1
                };

                attachments.Add(new Tuple<MessageAttachment, byte[]>(attachment, splittedFileData[i]));
            }

            return attachments;
        }

        public Task SaveMessageAttachmentAsync(MessageAttachment messageAttachment) => Connection.InsertAsync(messageAttachment);

        public async Task RemoveMessageAttachmentAsync(Guid attachmentGroupId)
        {
            var attachments = await Connection.Table<MessageAttachment>().Where(m => m.AttachmentGroupId == attachmentGroupId).ToListAsync().ConfigureAwait(false);

            if (attachments.Count == 0) return;

            foreach (var attachment in attachments)
            {
                await Connection.DeleteAsync(attachment).ConfigureAwait(false);

                // Delete parts from the disk.
                _attachmentManager.DeleteAttachment(attachment);
            }
        }

        public Task<List<MessageDeliveryInformation>> GetMessageDeliveryInformationAsync(string envelopeId)
            => Connection.Table<MessageDeliveryInformation>().Where(a => a.EnvelopeId == envelopeId).ToListAsync();
    }
}
