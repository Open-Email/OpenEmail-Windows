﻿using System.ComponentModel;

namespace OpenEmail.Contracts.Application
{
    public interface IPreferencesService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the maximum size of an attachment that can be downloaded automatically in megabytes.
        /// </summary>
        int DownloadAttachmentLimitInMegabytes { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes at which the application should sync with the server.
        /// </summary>
        int SyncIntervalInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in days at which the application should empty the trash.
        /// </summary>
        int EmptyTrashIntervalInDays { get; set; }

        /// <summary>
        /// Gets or sets the list of trusted domains.
        /// </summary>
        List<string> TrustedDomains { get; set; }

        /// <summary>
        /// Gets or sets the width of the message listing pane.
        /// </summary>
        double MessageListingPaneWidth { get; set; }

        /// <summary>
        /// Gets or sets the width of the open navigation pane.
        /// </summary>
        double ShellMenuPageOpenWidth { get; set; }
    }
}
