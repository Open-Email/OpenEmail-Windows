namespace OpenEmail.Contracts.Services
{
    public interface IWindowService
    {
        /// <summary>
        /// Closes login window and opens a new window with shell.
        /// </summary>
        Task StartShellApplicationAsync();

        /// <summary>
        /// Closes shell window and opens login window.
        /// </summary>
        Task GoBackLoginAsync();
    }
}
