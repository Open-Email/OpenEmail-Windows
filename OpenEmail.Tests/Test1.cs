using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.MessageEnvelope;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Tests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var message = new Message()
            {
                AccountId = Guid.NewGuid(),
                Author = "burakwindows@open.email",
                Body = "Hello, World!",
                Category = "personal",
                CreatedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Readers = "tester33@open.email",
                Size = 13,
                Subject = "Test Subject",
            };


            var payloadSeal = new PayloadSeal(CryptoConstants.SYMMETRIC_CIPHER);

            var acc = new Account()
            {
                PublicEncryptionKey = "1XhNs0nVnr5qhcIwAGuLmFIl5a/OgQIt7oeBszvvERg=",
                PublicEncryptionKeyId = "1XhNs0nVnr5qhcIwAGuLmFIl5a/OgQIt7oeBszvvERg=",
                PublicSigningKey = "JcXiYpWihbIiRf95MiNZ1Yt4Ma1j42FEu0L2WYPpxmc=",
                LocalPart = "burakwindows",
                HostPart = "open.email",
            };

            var privenc = "oKFDkqLP31CaiyZl+fkVx3MprymuTWZTIKU9/xkjcxs=";
            var privsign = "RWXVx+wI7Z4WRpu6yKMyzkoET4mO5SggrSrTtVnSc/hJxmSbu9WPeDg3jHEF9uMMoalTlu7jxT0YNTG2rmZlmA==";


            var privencbytes = Convert.FromBase64String(privenc);
            var privsignbytes = Convert.FromBase64String(privsign);

            //var pp = new ProfileData()
            var profile = new AccountProfile(acc, null, privencbytes, privsignbytes, null);

            var envelope = new MessageUploadPayload(message, profile, payloadSeal, new List<ReaderUploadData>()
            {
                new ReaderUploadData("tester33@open.email", "pDs3MHbs3aKr+wCu0z8p9oYdDuuzUvsQElwdvqPWhWE=", "kk2yDP8ZuaMaV4QmVDL8kR3pg+UxF9oIRazA5WKRHxs=","ULnK")
            });

            var link = AccountLink.Create(UserAddress.CreateFromAddress("burakwindows@open.email"), UserAddress.CreateFromAddress("tester33@open.email"));

            var testerprofile = GetTesterProfile();

            var t = new EnvelopeBase(envelope.ToString(), testerprofile, link, null);

            var accessKey = envelope.MessageAccessKey;
        }

        private AccountProfile GetTesterProfile()
        {
            var acc = new Account()
            {
                Id = Guid.NewGuid(),
                HostPart = "open.email",
                LocalPart = "tester33",
                DisplayName = "Dejan",
                PublicEncryptionKey = "pDs3MHbs3aKr+wCu0z8p9oYdDuuzUvsQElwdvqPWhWE=",
                PublicEncryptionKeyId = "ULnK",
                PublicSigningKey = "kk2yDP8ZuaMaV4QmVDL8kR3pg+UxF9oIRazA5WKRHxs="
            };

            var profile = new ProfileData(@"## Profile of tester33@open.email
Name: Dejan Strbac
Last-Seen: 2024-12-04T08:24:36.000Z
Updated: 2024-12-14T23:19:28.000Z
Encryption-Key: id=ULnK; algorithm=curve25519xsalsa20poly1305; value=pDs3MHbs3aKr+wCu0z8p9oYdDuuzUvsQElwdvqPWhWE=
Signing-Key: algorithm=ed25519; value=kk2yDP8ZuaMaV4QmVDL8kR3pg+UxF9oIRazA5WKRHxs=
## End of profile
");

            var enc = Convert.FromBase64String("BZFrYn4eC7cU/ExdAXlyzldwEA4NG+AOM4Jo9db7PUw=");
            var sign = Convert.FromBase64String("3q0ysl+A0RvQ7laix85gHcTFuEM/bM8VpPw4bthQWoySTbIM/xm5oxpXhCZUMvyRHemD5TEX2ghFrMDlYpEfGw==");

            return new AccountProfile(acc, profile, enc, sign, null);
        }
    }
}
