using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Configuration;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Core.Helpers;
using OpenEmail.Domain;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.Core.Services
{
    public class AttachmentManager : IAttachmentManager
    {
        private const string AttachmentsFolderName = "Attachments";
        private const string AttachmentFilePartFileFormat = ".chunk";

        private readonly IApplicationConfiguration _applicationConfiguration;
        private readonly IClientFactory _clientFactory;

        private Dictionary<Guid, AttachmentProgress> _activeDownloads = new();

        public AttachmentManager(IApplicationConfiguration applicationConfiguration, IClientFactory clientFactory)
        {
            _applicationConfiguration = applicationConfiguration;
            _clientFactory = clientFactory;
        }

        public async Task SaveAttachmentEnvelopeAsync(MessageAttachment attachment, byte[] content)
        {
            var attachmentRootFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, attachment.ParentId);

            // If whole file exists, don't save it.
            if (File.Exists($"{attachmentRootFolderPath}{attachment.FileName}")) return;

            // Save the part.
            // Later on when needed, parts will be merged and saved as a single file.
            // Then we'll delete the parts.

            if (!Directory.Exists(attachmentRootFolderPath)) Directory.CreateDirectory(attachmentRootFolderPath);

            var attachmentFilePath = $"{Path.Combine(attachmentRootFolderPath, attachment.Id)}{AttachmentFilePartFileFormat}";

            await File.WriteAllBytesAsync(attachmentFilePath, content);
        }

        public string CreateAttachmentFilePath(string parentId, string fileName)
        {
            var attachmentFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, parentId);
            return $"{attachmentFolderPath}\\{fileName}";
        }

        /// <summary>
        /// Merges attachment parts and saves the whole file.
        /// Removes parts after merging.
        /// </summary>
        /// <param name="allPartsOfAttachment">Parts of the attachment.</param>
        /// <returns>Full file path of the merged file.</returns>
        /// <exception cref="Exception">Missing part for the attachment.</exception>
        private async Task<string> MergeAttachmentChunksAsync(List<MessageAttachment> allPartsOfAttachment)
        {
            var parentId = allPartsOfAttachment[0].ParentId;
            var fileName = allPartsOfAttachment[0].FileName;
            var attachmentFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, parentId);
            var fullFileName = CreateAttachmentFilePath(parentId, fileName);

            var totalParts = allPartsOfAttachment.Max(x => x.Part);

            if (totalParts == 1)
            {
                // Single part attachment.
                // Save the first part as the whole file.

                var firstPart = allPartsOfAttachment[0];

                // Rename the saved part to the original file name.
                File.Move($"{Path.Combine(attachmentFolderPath, firstPart.Id)}{AttachmentFilePartFileFormat}", fullFileName);

                return $"{Path.Combine(attachmentFolderPath, fullFileName)}";
            }
            else
            {
                // Multi part attachment.

                // Read all parts, merge them, save as single file and delete parts.

                var allParts = new List<byte[]>();

                for (var i = 1; i <= totalParts; i++)
                {
                    var part = allPartsOfAttachment.FirstOrDefault(x => x.Part == i) ?? throw new Exception("Part not found.");
                    var partFilePath = $"{attachmentFolderPath}\\{part.Id}{AttachmentFilePartFileFormat}";

                    var partContent = await File.ReadAllBytesAsync(partFilePath);
                    File.Delete(partFilePath);

                    allParts.Add(partContent);
                }

                var mergedFile = allParts.SelectMany(x => x).ToArray();
                await File.WriteAllBytesAsync(fullFileName, mergedFile);

                return fullFileName;
            }
        }

        public async Task<byte[]> GetAttachmentAsync(List<MessageAttachment> allPartsOfAttachment)
        {
            // Filename is the same for all parts.
            var fileName = allPartsOfAttachment[0].FileName;
            var parentId = allPartsOfAttachment[0].ParentId;

            var attachmentFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, parentId);
            var fullFileName = CreateAttachmentFilePath(parentId, fileName);

            // If the file exists, return it.
            if (File.Exists(fullFileName)) return await File.ReadAllBytesAsync(fullFileName);

            var mergedFilePath = await MergeAttachmentChunksAsync(allPartsOfAttachment).ConfigureAwait(false);

            return await File.ReadAllBytesAsync(mergedFilePath);
        }

        private bool IsProgressExists(Guid attachmentGroupId, out AttachmentProgress progress)
        {
            if (_activeDownloads.ContainsKey(attachmentGroupId))
            {
                progress = _activeDownloads[attachmentGroupId];
                return true;
            }

            progress = null;
            return false;
        }

        public async Task StartDownloadAttachmentAsync(AttachmentDownloadInfo downloadInfo)
        {
            var attachments = downloadInfo.Parts;
            var groupId = attachments[0].AttachmentGroupId;
            var parentMessageId = attachments[0].ParentId;
            var fileName = attachments[0].FileName;

            // Don't start multi downloads.
            if (IsProgressExists(groupId, out AttachmentProgress progress) &&
                (progress.Status == AttachmentDownloadStatus.Downloaded || progress.Status == AttachmentDownloadStatus.Downloading))
                return;

            // File already exists.
            if (File.Exists(CreateAttachmentFilePath(parentMessageId, fileName))) return;

            // Download the attachment by parts.
            var totalSize = attachments.Sum(x => x.Size);
            progress = new AttachmentProgress(totalSize);

            _activeDownloads.Add(groupId, progress);

            WeakReferenceMessenger.Default.Send(new AttachmentDownloadSessionCreated(groupId, progress));

            // First check downloaded chunks and add to progress.

            List<MessageAttachment> chunksToDownload = new();

            attachments.ForEach(attachmentPart =>
            {
                // Some parts might be downloaded. Skip them.
                var partFilePath = CreateAttachmentFilePath(parentMessageId, $"{attachmentPart.Id}{AttachmentFilePartFileFormat}");

                if (File.Exists(partFilePath))
                {
                    // Check size. Maybe it was interrupted.
                    if (new FileInfo(partFilePath).Length == attachmentPart.Size)
                    {
                        // Part is downloaded. Continue with the next part.
                        progress.BytesDownloaded += attachmentPart.Size;
                    }
                    else
                    {
                        // Chunk is corrupted. Delete it. Add for re-download.
                        File.Delete(partFilePath);

                        chunksToDownload.Add(attachmentPart);
                    }
                }
                else
                {
                    chunksToDownload.Add(attachmentPart);
                }
            });

            foreach (var attachmentPart in chunksToDownload)
            {
                progress.Status = AttachmentDownloadStatus.Downloading;

                var messagesClient = _clientFactory.CreateProfileClientWithProgress<IMessagesClient>(progress);
                var attachmentPartContent = await messagesClient.GetMessageContentAsync(downloadInfo.TargetAddress, attachmentPart.Id, downloadInfo.Link).ConfigureAwait(false);

                byte[] rawContent = await attachmentPartContent.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                // Content is encrypted with symmetric key.
                if (attachmentPart.AccessKey != null)
                {
                    var accessKey = Convert.FromBase64String(attachmentPart.AccessKey);
                    rawContent = CryptoUtils.DecryptSymmetric(rawContent, accessKey);
                }

                await SaveAttachmentEnvelopeAsync(attachmentPart, rawContent).ConfigureAwait(false);
            }

            // Merge parts.
            await MergeAttachmentChunksAsync(attachments).ConfigureAwait(false);

            progress.Status = AttachmentDownloadStatus.Downloaded;
            _activeDownloads.Remove(groupId);
        }

        public AttachmentProgress GetAttachmentProgress(AttachmentDownloadInfo attachmentDownloadInfo)
        {
            if (IsProgressExists(attachmentDownloadInfo.AttachmentGroupId, out AttachmentProgress progress)) return progress;

            return null;
        }

        public void DeleteAttachment(MessageAttachment messageAttachment)
        {
            var parentId = messageAttachment.ParentId;

            var attachmentFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, parentId);

            var attachmentPartFileName = $"{attachmentFolderPath}\\{messageAttachment.Id}{AttachmentFilePartFileFormat}";

            if (File.Exists(attachmentPartFileName)) File.Delete(attachmentPartFileName);

            // User might've merge the parts and created a single file for preview.
            // Delete that if exists as well.

            var fullFileName = CreateAttachmentFilePath(parentId, messageAttachment.FileName);

            if (File.Exists(fullFileName)) File.Delete(fullFileName);
        }

        public byte[] GetAttachmentChunkBytes(MessageAttachment messageAttachment)
        {
            var parentId = messageAttachment.ParentId;
            var attachmentFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, AttachmentsFolderName, parentId);

            var fullFileName = messageAttachment.FilePath;
            var partFilePath = $"{attachmentFolderPath}\\{messageAttachment.Id}{AttachmentFilePartFileFormat}";

            if (File.Exists(partFilePath)) return File.ReadAllBytes(partFilePath);

            if (!File.Exists(fullFileName)) return new byte[0];

            // Append or create the attachment file on the app storage.
            try
            {
                using var fileStream = File.OpenRead(fullFileName);

                if (messageAttachment.Part > 1)
                {
                    fileStream.Seek((messageAttachment.Part - 1) * ByteHelper.MaxPartSizeMB * 1024 * 1024, SeekOrigin.Begin);
                }

                var remainingBytes = Math.Min(ByteHelper.MaxPartSizeMB * 1024 * 1024, fileStream.Length - fileStream.Position);
                var buffer = new byte[remainingBytes];
                fileStream.Read(buffer, 0, (int)remainingBytes);

                Directory.CreateDirectory(attachmentFolderPath);
                File.WriteAllBytes(partFilePath, buffer);

                return buffer;
            }
            catch
            {
                return new byte[0];
            }
        }
    }
}
