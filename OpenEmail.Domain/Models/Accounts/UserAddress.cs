using Refit;

namespace OpenEmail.Domain.Models.Accounts
{
    public record UserAddress([AliasAs("local")] string LocalPart, [AliasAs("host")] string HostPart)
    {
        public string FullAddress => $"{LocalPart}@{HostPart}";

        public static UserAddress CreateFromAddress(string address)
        {
            var parts = address.Split('@');
            return new UserAddress(parts[0], parts[1]);
        }

        public override string ToString()
        {
            return $"{HostPart}/{LocalPart}";
        }
    }
}
