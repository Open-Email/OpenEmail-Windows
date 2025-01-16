using OpenEmail.Domain.Models.Accounts;
using SQLite;

namespace OpenEmail.Domain.Entities
{
    public class Account
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        public string LocalPart { get; set; }
        public string HostPart { get; set; }
        public string DisplayName { get; set; }
        public string PublicEncryptionKey { get; set; }
        public string PublicSigningKey { get; set; }
        public string PublicEncryptionKeyId { get; set; }

        private UserAddress _address;

        [Ignore]
        public UserAddress Address
        {
            get
            {
                if (_address == null)
                {
                    _address = new UserAddress(LocalPart, HostPart);
                }

                return _address;
            }
        }
    }
}
