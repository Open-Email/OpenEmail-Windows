using System.Security.Cryptography;
using System.Text;

namespace OpenEmail.Domain.Models.Accounts
{
    public record AccountLink(string Link)
    {
        // Format is {source.FullAddress}{target.FullAddress} string encrypted with SHA-2

        public static AccountLink Create(UserAddress Source, UserAddress Target)
        {
            var linkJoin = new[] { Source.FullAddress, Target.FullAddress }
            .Select(x => x.ToLower().Trim())
            .OrderBy(x => x)
            .Aggregate((x, y) => x + y);

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(linkJoin));

            var generatedLink = string.Concat(hash.Select(b => b.ToString("x2")));

            return new AccountLink(generatedLink);
        }
    }
}
