using System.Text;
using CommunityToolkit.Diagnostics;
using OpenEmail.Domain.Models.Mail;

namespace OpenEmail.Domain.Models.Profile
{
    public class ProfileData : KeyValueDataStore
    {
        public bool IsReadOnly { get; set; } = true;

        // Don't show these attributes to the user.
        private string[] invisibleDisplayAttributes = new[]
        {
            "Encryption-Key",
            "Name",
            "Signing-Key"
        };

        public ProfileData(string input) : base(input)
        {
            Guard.IsNotNull(Name, nameof(Name));
            Guard.IsNotNull(Updated, nameof(Updated));
            Guard.IsNotNull(SigningKey, nameof(SigningKey));

            DisplayAttributes = new();

            foreach (var item in Data)
            {
                if (!string.IsNullOrEmpty(item.Value) && !invisibleDisplayAttributes.Contains(item.Key))
                {
                    DisplayAttributes.Add(new ProfileAttribute(item.Key, item.Value));
                }
            }
        }

        public string Name => GetData<string>(nameof(Name));
        public DateTimeOffset Updated => GetData<DateTimeOffset>(nameof(Updated));
        public string SigningKey => ParseAttributes(GetData<string>("Signing-Key"))["value"];

        public bool? Away => GetData<bool?>(nameof(Away));
        public bool? LastSeenPublic => GetData<bool?>("Last-Seen-Public");
        public DateTime? LastSeen => GetData<DateTime?>("Last-Seen");
        public string EncryptionKey => ParseAttributes(GetData<string>("Encryption-Key"))["value"];
        public string EncryptionKeyId => ParseAttributes(GetData<string>("Encryption-Key"))["id"];

        /// <summary>
        /// List of key-value display pairs for non-null attributes.
        /// </summary>
        public List<ProfileAttribute> DisplayAttributes { get; }

        public override string ToString() => DataStoreInput;

        public void UpdateAttribute(string key, string value)
        {
            // Don't update if editing explicitly enabled for the profile.
            if (IsReadOnly) return;

            // Do the conversions.

            if (bool.TryParse(value, out bool boolVal))
            {
                value = boolVal ? "Yes" : "No";
            }

            if (Data.ContainsKey(key))
            {
                Data[key] = value;
            }
            else
            {
                Data.Add(key, value);
            }
        }

        /// <summary>
        /// Serializes the profile data to a string.
        /// </summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (var kvp in Data)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }

        // Address is not stored in profile data.
        public ReaderUploadData CreateUploadPayload(string address) =>
            new ReaderUploadData(address, EncryptionKey, SigningKey, EncryptionKeyId);
    }
}
