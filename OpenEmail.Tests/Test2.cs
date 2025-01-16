using System.Text;
using OpenEmail.Domain;
using Sodium;

namespace OpenEmail.Tests
{
    [TestClass]
    public sealed class Test2
    {
        [TestMethod]
        public void TestMethod2()
        {
            var accesskey = SodiumCore.GetRandomBytes(32);
            string originalMessage2 = "Hello, world!";
            var data = Encoding.UTF8.GetBytes(originalMessage2);

            var encrypted = CryptoUtils.EncryptSymmetric(data, accesskey);
            var decrypted2 = CryptoUtils.DecryptSymmetric(encrypted, accesskey);
        }
    }
}
