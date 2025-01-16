using Microsoft.UI.Dispatching;
using OpenEmail.Contracts.Application;
using System;

namespace OpenEmail.WinRT
{
    public class WinUIDispatcher : IPlatformDispatcher
    {
        protected DispatcherQueue DispatcherQueue { get; }

        public WinUIDispatcher(DispatcherQueue dispatcherQueue)
        {
            DispatcherQueue = dispatcherQueue;
        }

        public void ExecuteOnDispatcher(Action action)
            => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => action());
    }
}
