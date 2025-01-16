using System.Collections.ObjectModel;
using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Contracts.Services
{
    public interface IMessageUploader
    {
        ObservableCollection<MessageUploadProgress> MessageUploadQueue { get; }

        Task UploadMessageAsync(Guid rootMessageId, List<ReaderUploadData> readersMap, IPlatformDispatcher platformDispatcher);
    }
}
