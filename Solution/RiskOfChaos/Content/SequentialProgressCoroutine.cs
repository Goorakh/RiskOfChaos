using HG;
using RiskOfChaos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Content
{
    public sealed class SequentialProgressCoroutine : IAsyncOperationCoroutine
    {
        readonly IProgress<float> _progressReceiver;

        readonly List<CoroutineEntry> _coroutineEntries = [];
        int _currentCoroutineIndex;

        readonly IEnumerator _internalCoroutine;

        public float Progress
        {
            get => field;
            private set
            {
                if (field != value)
                {
                    field = value;
                    _progressReceiver.Report(value);
                }
            }
        }

        public SequentialProgressCoroutine(IProgress<float> progressReceiver)
        {
            _progressReceiver = progressReceiver;
            _internalCoroutine = internalCoroutine();
        }

        public void Add(IEnumerator coroutine, ReadableProgress<float> progressReceiver)
        {
            _coroutineEntries.Add(new CoroutineEntry(coroutine, progressReceiver));
        }

        public void Add(IAsyncOperationCoroutine coroutine)
        {
            _coroutineEntries.Add(new CoroutineEntry(coroutine, null));
        }

        object IEnumerator.Current => _internalCoroutine.Current;

        bool IEnumerator.MoveNext()
        {
            return _internalCoroutine.MoveNext();
        }

        void IEnumerator.Reset()
        {
            _internalCoroutine.Reset();
        }

        IEnumerator internalCoroutine()
        {
            while (_currentCoroutineIndex < _coroutineEntries.Count)
            {
                IEnumerator currentCoroutine = _coroutineEntries[_currentCoroutineIndex].Coroutine;
                if (currentCoroutine.MoveNext())
                {
                    yield return currentCoroutine.Current;
                }
                else
                {
                    _currentCoroutineIndex++;
                }

                recalculateProgress();
            }
        }

        void recalculateProgress()
        {
            if (_coroutineEntries.Count == 0)
            {
                Progress = 1f;
                return;
            }

            float progress = _currentCoroutineIndex;
            if (_currentCoroutineIndex < _coroutineEntries.Count)
            {
                if (_coroutineEntries[_currentCoroutineIndex].Coroutine is IAsyncOperationCoroutine asyncOperationCoroutine)
                {
                    progress += Mathf.Clamp01(asyncOperationCoroutine.Progress);
                }
                else
                {
                    progress += Mathf.Clamp01(_coroutineEntries[_currentCoroutineIndex].ProgressReceiver.value);
                }
            }

            Progress = Mathf.Clamp01(progress / _coroutineEntries.Count);
        }

        readonly struct CoroutineEntry
        {
            public readonly IEnumerator Coroutine;

            public readonly ReadableProgress<float> ProgressReceiver;

            public CoroutineEntry(IEnumerator coroutine, ReadableProgress<float> progressReceiver)
            {
                Coroutine = coroutine;
                ProgressReceiver = progressReceiver;
            }
        }
    }
}
