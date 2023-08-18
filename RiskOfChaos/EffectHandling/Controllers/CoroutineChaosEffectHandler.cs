using RiskOfChaos.EffectDefinitions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(false)]
    public class CoroutineChaosEffectHandler : MonoBehaviour
    {
        ChaosEffectDispatcher _effectDispatcher;

        class CoroutineWrapper
        {
            readonly IEnumerator _original;

            MonoBehaviour _coroutineHost;
            Coroutine _runningCoroutine;

            bool _isFinished;
            public bool IsFinished => _isFinished;

            public event Action OnFinished;

            public CoroutineWrapper(IEnumerator original)
            {
                _original = original;
            }

            public void PerformOn(MonoBehaviour coroutineHost)
            {
                _coroutineHost = coroutineHost;
                _runningCoroutine = _coroutineHost.StartCoroutine(perform());
            }

            IEnumerator perform()
            {
                yield return _original;
                _isFinished = true;

                OnFinished?.Invoke();
            }

            public void Stop()
            {
                if (_isFinished)
                    return;

                _coroutineHost.StopCoroutine(_runningCoroutine);
            }
        }

        class ActiveCoroutineEffect
        {
            public readonly ICoroutineEffect EffectInstance;

            CoroutineWrapper _runningCoroutine;

            public event Action OnEnd;

            public ActiveCoroutineEffect(ICoroutineEffect effectInstance)
            {
                EffectInstance = effectInstance;
            }

            public void StartCoroutineOn(MonoBehaviour coroutineHost)
            {
                _runningCoroutine = new CoroutineWrapper(EffectInstance.OnStartCoroutine());

                _runningCoroutine.OnFinished += () =>
                {
                    OnEnd?.Invoke();
                };

                _runningCoroutine.PerformOn(coroutineHost);
            }

            public void Stop()
            {
                if (_runningCoroutine.IsFinished)
                    return;

                _runningCoroutine.Stop();
                EffectInstance.OnForceStopped();

                OnEnd?.Invoke();
            }
        }

        readonly List<ActiveCoroutineEffect> _activeCoroutineEffects = new List<ActiveCoroutineEffect>();

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();
        }

        void OnEnable()
        {
            _effectDispatcher.OnEffectDispatched += onEffectDispatched;

            _activeCoroutineEffects.Clear();
        }

        void OnDisable()
        {
            _effectDispatcher.OnEffectDispatched -= onEffectDispatched;

            foreach (ActiveCoroutineEffect activeCoroutineEffect in _activeCoroutineEffects.ToArray())
            {
                activeCoroutineEffect.Stop();
            }

            _activeCoroutineEffects.Clear();
        }

        void onEffectDispatched(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            if (effectInstance is ICoroutineEffect coroutineEffect)
            {
                startCoroutineEffect(coroutineEffect);
            }
        }

        void startCoroutineEffect(ICoroutineEffect effect)
        {
            ActiveCoroutineEffect activeCoroutineEffect = new ActiveCoroutineEffect(effect);
            activeCoroutineEffect.StartCoroutineOn(this);
            _activeCoroutineEffects.Add(activeCoroutineEffect);

            activeCoroutineEffect.OnEnd += () =>
            {
                _activeCoroutineEffects.Remove(activeCoroutineEffect);
            };
        }
    }
}
