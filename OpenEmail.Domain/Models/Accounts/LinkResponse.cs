namespace OpenEmail.Domain.Models.Accounts
{
    public class LinkResponse
    {
        public LinkResponse(UserAddress address, bool? isBroadcastEnabled) : this(address)
        {
            IsBroadcastEnabled = isBroadcastEnabled;
        }

        public LinkResponse(UserAddress address)
        {
            Address = address;
        }

        public UserAddress Address { get; set; }

        /// <summary>
        /// Whether the user's broadcast is enabled on the server.
        /// Null if not specified.
        /// </summary>
        public bool? IsBroadcastEnabled { get; set; }
    }
}
