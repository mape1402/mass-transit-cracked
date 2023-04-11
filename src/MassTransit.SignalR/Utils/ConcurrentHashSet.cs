﻿namespace MassTransit.SignalR.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;


    // From here: https://stackoverflow.com/a/11034999/6558597
    public class ConcurrentHashSet<T> : IDisposable
    {
        readonly HashSet<T> _hashSet;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ConcurrentHashSet()
        {
            _hashSet = new HashSet<T>();
        }

        public ConcurrentHashSet(IEqualityComparer<T> equalityComparer)
        {
            _hashSet = new HashSet<T>(equalityComparer);
        }

        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _hashSet.Count;
                }
                finally
                {
                    if (_lock.IsReadLockHeld)
                        _lock.ExitReadLock();
                }
            }
        }

    #region Dispose

        public void Dispose()
        {
            if (_lock != null)
                _lock.Dispose();
        }

    #endregion

        public bool Add(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _hashSet.Add(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _hashSet.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _hashSet.Contains(item);
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _hashSet.Remove(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public T[] ToArray()
        {
            try
            {
                _lock.EnterReadLock();
                return _hashSet.ToArray(); // Internally Linq .ToArray uses CopyTo
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }
    }
}
