using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using OpenEmail.Domain.Models.Mail;
using Windows.UI.Text;

namespace OpenEmail.Helpers
{
    public static class XamlHelpers
    {
        public static Visibility ReverseBooleanToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
        public static Visibility ReverseVisibility(Visibility value) => value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        public static bool ReverseBoolean(bool value) => !value;

        public static string GetTimeAgo(string dateStringValue)
            => GetTimeAgo(DateTimeOffset.Parse(dateStringValue));

        public static string GetTimeAgo(DateTimeOffset? dateTimeOffsetValue)
            => GetTimeAgo(dateTimeOffsetValue?.DateTime);

        public static string GetFormattedLocalTimeDate(DateTimeOffset dateTimeOffset, string format)
            => dateTimeOffset.ToLocalTime().ToString(format);

        public static string GetFolderHeaderTitle(MailFolder folderType) => $"{folderType} Messages";
        public static string EmptyTextConverter(string value, string emptyValue) => string.IsNullOrWhiteSpace(value) ? emptyValue : value;

        public static Visibility VisibilityAndConverter(bool value1, bool value2) => value1 && value2 ? Visibility.Visible : Visibility.Collapsed;
        public static Visibility VisibilityAndReverseConverter(bool value1, bool value2) => value1 && value2 ? Visibility.Collapsed : Visibility.Visible;
        public static Visibility VisibilityOrConverter(bool value1, bool value2) => value1 || value2 ? Visibility.Visible : Visibility.Collapsed;
        public static Visibility VisibilityOrReverseConverter(bool value1, bool value2) => value1 || value2 ? Visibility.Collapsed : Visibility.Visible;

        public static string GetFolderIconByFolderType(MailFolder folderType)
        {
            return folderType switch
            {
                MailFolder.Inbox => OpenEmailIcons.Inbox,
                MailFolder.Outbox => OpenEmailIcons.Outbox,
                MailFolder.Drafts => OpenEmailIcons.Drafts,
                MailFolder.Trash => OpenEmailIcons.Trash,
                _ => OpenEmailIcons.Inbox,
            };
        }
        public static string GetDisplayDateOnListingPage(DateTimeOffset dateTimeOffset)
        {
            var diff = DateTimeOffset.Now - dateTimeOffset;

            if (diff.TotalDays < 1)
                return dateTimeOffset.ToString("h:mm tt");
            else if (diff.TotalDays < 2)
                return "Yesterday";
            else if (diff.TotalDays < 7)
                return dateTimeOffset.ToString("dddd");
            else
                return dateTimeOffset.ToString("dd MMMM");
        }

        public static FontWeight IsReadConverter(bool isRead) => isRead ? FontWeights.SemiBold : FontWeights.Bold;

        public static string GetTimeAgo(DateTime? dateTimeValue)
        {
            if (dateTimeValue == null) return "Unknown";

            var dateTime = dateTimeValue.Value;

            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return $"{(int)timeSpan.TotalSeconds} seconds ago";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            else if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            else if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            else
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }
    }
}
