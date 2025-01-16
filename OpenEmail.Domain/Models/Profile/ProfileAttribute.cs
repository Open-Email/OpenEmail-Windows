namespace OpenEmail.Domain.Models.Profile
{
    public class ProfileAttribute
    {
        public ProfileAttribute(string rawKey, string value)
        {
            RawKey = rawKey;
            DisplayKey = rawKey.Replace("-", " ");
            Value = value;
        }

        public string RawKey { get; }
        public string Value { get; }

        /// <summary>
        /// The key to display to the user.
        /// For example; Last-Seen-Public would be displayed as "Last Seen Public".
        /// </summary>
        public string DisplayKey { get; }
    }
}
