namespace OpenEmail.Domain.Models.Contacts
{
    public enum ContactRelationship
    {
        Unknown, // Failed to retireve relationship
        Mutual, // Both users have each other in their contacts
        OnlyMe, // Only the user has the contact. The contact does not have the user in their address book.
        OnlyThem, // Only the contact has the user. The user does not have the contact in their address book.
    }
}
