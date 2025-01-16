namespace OpenEmail.Contracts.Application
{
    public interface IPreferencesService
    {
        int DownloadAttachmentLimitInMegabytes { get; set; }
        int SyncIntervalInMinutes { get; set; }
        int EmptyTrashIntervalInDays { get; set; }
        List<string> TrustedDomains { get; set; }
    }
}
