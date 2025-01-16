using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface ISynchronizationService
    {
        Task SynchronizeAsync(AccountProfile accountProfile);
    }
}
