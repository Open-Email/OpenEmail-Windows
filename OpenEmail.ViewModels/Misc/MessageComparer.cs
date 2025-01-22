using OpenEmail.ViewModels.Data;

namespace OpenEmail.Domain.Models.Mail
{
    public class MessageComparer : IComparer<MessageViewModel>
    {
        public int Compare(MessageViewModel x, MessageViewModel y)
        {
            return x.CreatedAt.CompareTo(y.CreatedAt);
        }
    }
}
