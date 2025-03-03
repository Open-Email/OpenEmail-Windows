namespace OpenEmail.Domain.Models.Navigation
{
    public enum PageType
    {
        // Having mail folder types first is important because we cast to MailFolder enum type as parameter.
        // Don't change the order of this enum.
        MailInbox,
        MailOutbox,
        MailDrafts,
        MailTrash,
        MailBroadcast, // Mail list broadcast page.
        BroadcastPage, // Disabled broadcast page.
        SettingsPage,
        ContactsPage,
        ProfileEditPage
    }
}
