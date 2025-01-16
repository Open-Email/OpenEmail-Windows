namespace OpenEmail.Contracts.Navigation
{
    /// <summary>
    /// Common implementation of the dialog view models.
    /// </summary>
    public interface IDialogAware
    {
        void OnDialogOpened();
        void OnDialogClosed();

        event EventHandler HideRequested;
        void Hide();
    }

    /// <summary>
    /// Generic version of IDialogAware that can return a result via dialog service.
    /// </summary>
    /// <typeparam name="TResult">Result of the dialog.</typeparam>
    public interface IDialogAware<TResult> : IDialogAware
    {
        TResult Result { get; }
    }
}
