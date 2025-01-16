using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Core.Services
{
    public class ApplicationStateService : IApplicationStateService
    {
        public AccountProfile ActiveProfile { get; set; }
    }
}
