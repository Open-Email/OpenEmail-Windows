using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Entities;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels.Interfaces
{
    /// <summary>
    /// Service responsible for constructing view models for messages.
    /// </summary>
    public interface IMessagePreperationService
    {
        Task<ContactViewModel> PrepareContactViewModel(string address, CancellationToken cancellationToken = default);
        Task<MessageViewModel> PrepareViewModelAsync(Message message, IPlatformDispatcher platformDispatcher, CancellationToken cancellationToken = default);

    }
}
