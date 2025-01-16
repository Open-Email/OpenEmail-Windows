using System.Diagnostics;
using System.Text;
using EmailValidation;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.Models.Notifications
{
    public record NotificationResponse(string Id, string Link, string NotifierSigningKeyFingerprint, string EncryptedNotifierAddress)
    {
        public string DecryptedNotifierAddress { get; private set; }

        public bool ValidateFor(AccountProfile profile)
        {
            try
            {
                // If the encryption key was updated in the mean time, then the decryption will fail.
                // The right process of encryption key rotation will avoid that. All notifications should
                // be fetched before updating keys.

                var notifierAddress = CryptoUtils.DecryptAnonymous(EncryptedNotifierAddress, profile.PrivateEncryptionKey, profile.PublicEncryptionKey);

                DecryptedNotifierAddress = Encoding.ASCII.GetString(notifierAddress);

                // Validate address.
                if (!EmailValidator.Validate(DecryptedNotifierAddress))
                {
                    Debug.WriteLine($"Invalid email address for notification: {DecryptedNotifierAddress}");
                }

                // Validate link.
                var notifierAddressModel = UserAddress.CreateFromAddress(DecryptedNotifierAddress);
                var createdLink = AccountLink.Create(notifierAddressModel, profile.Account.Address);

                if (createdLink.Link != Link)
                {
                    Debug.WriteLine($"Link mismatch for notification. Expected: {createdLink.Link}, Actual: {Link}");
                    return false;
                }

                // Validate fingerprint.
                // TODO: Verification fails. Revisit this with a new account.

                //var profilePublicSigningKey = profile.PublicSigningKey;

                //if (NotifierSigningKeyFingerprint != CryptoUtils.Sha256Sum(profilePublicSigningKey).Item1)
                //    throw new ArgumentException("Invalid signing key fingerprint for notification.");

                // TODO: Try older signing keys. Right now we don't support changing of keys.

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Validation failed for notification: {ex.Message}");

                return false;
            }
        }

        public Notification AsNotification() => new Notification(this);
    }
}
