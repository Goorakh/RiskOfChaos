using RoR2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfChaos.Utilities
{
    class UnityUpdateTaskScheduler : TaskScheduler, IDisposable
    {
        public static UnityUpdateTaskScheduler Instance { get; } = new UnityUpdateTaskScheduler();

        static Thread _unityMainThread;

        [SystemInitializer]
        static void Init()
        {
            _unityMainThread = Thread.CurrentThread;

            Log.Debug($"Recorded Unity main thread: '{_unityMainThread.Name}' ({_unityMainThread.ManagedThreadId})");
        }

        public override int MaximumConcurrencyLevel => 1;

        readonly ConcurrentBag<Task> _queuedTasks = [];

        bool _disposed;

        public UnityUpdateTaskScheduler()
        {
            RoR2Application.onUpdate += staticUpdate;
        }

        ~UnityUpdateTaskScheduler()
        {
            dispose();
        }

        void staticUpdate()
        {
            while (_queuedTasks.TryTake(out Task task))
            {
                TryExecuteTask(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _queuedTasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            if (Thread.CurrentThread.ManagedThreadId == _unityMainThread.ManagedThreadId)
            {
                TryExecuteTask(task);
            }
            else
            {
                _queuedTasks.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread.ManagedThreadId != _unityMainThread.ManagedThreadId)
                return false;

            if (taskWasPreviouslyQueued)
            {
                if (_queuedTasks.TryTake(out task))
                {
                    return TryExecuteTask(task);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return TryExecuteTask(task);
            }
        }

        protected virtual void dispose()
        {
            if (!_disposed)
            {
                RoR2Application.onUpdate -= staticUpdate;

                _disposed = true;
            }
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }
    }
}
