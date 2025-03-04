namespace OpenEmail.Domain.Models.Messages
{
    /// <summary>
    /// A model that contains the message reader delivery information.
    /// </summary>
    /// <param name="SeenAt">Seen date.</param>
    /// <param name="Link">Account link.</param>
    public record DeliveryInfo(string Link, DateTimeOffset? SeenAt)
    {
        public static DeliveryInfo FromString(string item)
        {
            var splitted = item.Split(',');

            var accountLink = splitted[0].Trim();
            var seenAtString = splitted[1].Trim();

            DateTimeOffset? seenAt = null;

            // If the seen date is present, parse it.
            if (!string.IsNullOrEmpty(seenAtString) && long.TryParse(seenAtString, out long seenAtUnixTimestamp))
            {
                seenAt = DateTimeOffset.FromUnixTimeSeconds(seenAtUnixTimestamp);
            }

            return new DeliveryInfo(accountLink, seenAt);
        }
    }
}
