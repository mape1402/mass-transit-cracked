namespace MassTransit.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;


    public class PendingReceiveLockContext :
        ReceiveLockContext
    {
        ReceiveLockContext _lockContext;
        Queue<ReceiveLockContext> _pending;

        public Task Complete()
        {
            return Execute(context => context.Complete());
        }

        public Task Faulted(Exception exception)
        {
            return Execute(context => context.Faulted(exception));
        }

        public Task ValidateLockStatus()
        {
            return Execute(context => context.ValidateLockStatus());
        }

        public void Dispose()
        {
            lock (this)
            {
                _lockContext = null;
                _pending?.Clear();
            }
        }

        public void Enqueue(ReceiveLockContext lockContext)
        {
            lock (this)
            {
                if (_lockContext == null)
                    _lockContext = lockContext;
                else
                    (_pending ??= new Queue<ReceiveLockContext>(1)).Enqueue(lockContext);
            }
        }

        async Task Execute(Func<ReceiveLockContext, Task> action)
        {
            if (_lockContext == null)
            {
                lock (this)
                {
                    if (_lockContext == null)
                        return;
                }
            }

            ExceptionDispatchInfo dispatchInfo = null;

            do
            {
                try
                {
                    await action(_lockContext).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    dispatchInfo = ExceptionDispatchInfo.Capture(ex.GetBaseException());
                }
            }
            while (TryDequeue());

            if (dispatchInfo != null)
            {
                dispatchInfo.Throw();

                throw dispatchInfo.SourceException;
            }
        }

        bool TryDequeue()
        {
            lock (this)
            {
                if (_pending == null || _pending.Count == 0)
                    return false;

                _lockContext = _pending.Dequeue();
                return true;
            }
        }
    }
}
