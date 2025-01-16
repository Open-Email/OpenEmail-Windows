using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Application
{
    public interface IApplicationStateService
    {
        AccountProfile ActiveProfile { get; set; }
    }
}
