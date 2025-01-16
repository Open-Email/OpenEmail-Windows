using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.ViewModels.Data;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.Services
{
    public class MessagePreperationService : IMessagePreperationService
    {
        private readonly IMessagesService _messagesService;
        private readonly IContactService _contactService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IProfileDataManager _profileDataManager;
        private readonly IAttachmentManager _attachmentManager;

        public MessagePreperationService(IMessagesService messagesService,
                                         IContactService contactService,
                                         IApplicationStateService applicationStateService,
                                         IProfileDataManager profileDataManager,
                                         IAttachmentManager attachmentManager)
        {
            _messagesService = messagesService;
            _contactService = contactService;
            _applicationStateService = applicationStateService;
            _profileDataManager = profileDataManager;
            _attachmentManager = attachmentManager;
        }

        public async Task<ContactViewModel> PrepareContactViewModel(string address, CancellationToken cancellationToken = default)
        {
            if (address == _applicationStateService.ActiveProfile.Account.Address.FullAddress)
            {
                return ContactViewModel.CreateSelf(_applicationStateService.ActiveProfile);
            }

            var contact = await _contactService.GetContactAsync(_applicationStateService.ActiveProfile.Account.Id, address);

            // Contact info may not exists. Create default empty one from address.
            if (contact == null)
            {
                return ContactViewModel.CreateEmpty(address);
            }
            else
            {
                var profileData = await _profileDataManager.GetProfileDataAsync(UserAddress.CreateFromAddress(address), cancellationToken);

                return new ContactViewModel(contact, profileData);
            }
        }

        public async Task<MessageViewModel> PrepareViewModelAsync(Message message, IPlatformDispatcher platformDispatcher, CancellationToken cancellationToken = default)
        {
            // Find the contact and profile data for the author.
            ContactViewModel authorContactViewModel = await PrepareContactViewModel(message.Author, cancellationToken);

            // Prepare attachments
            var attachments = await _messagesService.GetMessageAttachmentsAsync(message.EnvelopeId);
            var groupedAttachments = attachments.GroupBy(a => a.AttachmentGroupId);

            List<AttachmentViewModel> attachmentViewModels = [];

            foreach (var group in groupedAttachments)
            {
                var attachmentGroupId = group.Key;

                var attachmentViewModel = new AttachmentViewModel(group.ToList(), message);

                // Try to hook existing attachment progress.
                var progress = _attachmentManager.GetAttachmentProgress(attachmentViewModel.CreateDownloadInfo(_applicationStateService.ActiveProfile));

                if (progress != null)
                {
                    attachmentViewModel.HookProgress(progress, platformDispatcher);
                }

                attachmentViewModel.LocalFilePath = _attachmentManager.CreateAttachmentFilePath(message.EnvelopeId, attachmentViewModel.FileName);

                attachmentViewModels.Add(attachmentViewModel);
            }

            // Prepare readers.
            var readerViewModels = new List<ContactViewModel>();

            if (!string.IsNullOrEmpty(message.Readers))
            {
                var readers = message.Readers.Split(',');

                foreach (var reader in readers)
                {
                    if (string.IsNullOrEmpty(reader)) continue;

                    var readerContactViewModel = await PrepareContactViewModel(reader, cancellationToken);

                    if (readerContactViewModel == null) continue;

                    readerViewModels.Add(readerContactViewModel);
                }
            }

            return new MessageViewModel(message, authorContactViewModel, readerViewModels, attachmentViewModels);
        }
    }
}
