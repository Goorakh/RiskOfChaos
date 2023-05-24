using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public class ChaosEffectManager : MonoBehaviour
    {
        static GameObject _effectManagerObject;

        static ChaosEffectManager _instance;
        public static ChaosEffectManager Instance => _instance;

        class ManagerComponent : IDisposable
        {
            readonly Behaviour _behaviour;
            readonly ChaosControllerAttribute _controllerAttribute;

            bool _isEnabled;
            public bool IsEnabled
            {
                get
                {
                    return _isEnabled;
                }
                set
                {
                    _isEnabled = value;
                    refreshComponentEnabledState();
                }
            }

            public ManagerComponent(Behaviour behaviour, ChaosControllerAttribute controllerAttribute)
            {
                _behaviour = behaviour;
                _controllerAttribute = controllerAttribute;

                _controllerAttribute.OnShouldRefreshEnabledState += refreshComponentEnabledState;
            }

            public void Dispose()
            {
                _controllerAttribute.OnShouldRefreshEnabledState -= refreshComponentEnabledState;
            }

            void refreshComponentEnabledState()
            {
                _behaviour.enabled = _isEnabled && _controllerAttribute.CanBeActive();

#if DEBUG
                if (_isEnabled && !_controllerAttribute.CanBeActive())
                {
                    Log.Debug($"Not enabling manager {_behaviour.GetType().Name}");
                }
#endif
            }
        }

        ManagerComponent[] _managerComponents;

        [SystemInitializer]
        static void InitializeObject()
        {
            _effectManagerObject = new GameObject("ChaosEffectManager");
            DontDestroyOnLoad(_effectManagerObject);
            _effectManagerObject.AddComponent<ChaosEffectManager>();
        }

        void Awake()
        {
            SingletonHelper.Assign(ref _instance, this);

            gameObject.SetActive(false);

            _managerComponents = ChaosControllerAttribute.GetInstances<ChaosControllerAttribute>()
                                                         .Cast<ChaosControllerAttribute>()
                                                         .Select(s => new ManagerComponent((Behaviour)gameObject.AddComponent((Type)s.target), s))
                                                         .ToArray();

            setManagersActive(Run.instance);

            gameObject.SetActive(true);

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        void OnDestroy()
        {
            SingletonHelper.Unassign(ref _instance, this);

            foreach (ManagerComponent manager in _managerComponents)
            {
                manager.Dispose();
            }

            Run.onRunStartGlobal -= Run_onRunStartGlobal;
            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
        }

        void setManagersActive(bool active)
        {
            foreach (ManagerComponent manager in _managerComponents)
            {
                manager.IsEnabled = active;
            }
        }

        void Run_onRunStartGlobal(Run _)
        {
            setManagersActive(true);
        }

        void Run_onRunDestroyGlobal(Run _)
        {
            setManagersActive(false);
        }
    }
}
