using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public class ChaosEffectManager : MonoBehaviour
    {
        static GameObject _effectManagerObject;

        static ChaosEffectManager _instance;
        public static ChaosEffectManager Instance => _instance;

        readonly struct ManagerComponent
        {
            readonly Behaviour _behaviour;
            readonly ChaosControllerAttribute _controllerAttribute;

            public readonly bool IsEnabled
            {
                get
                {
                    return _behaviour.enabled;
                }
                set
                {
                    if (!_controllerAttribute.CanBeActive())
                    {
#if DEBUG
                        if (value)
                        {
                            Log.Debug($"Not enabling manager {_behaviour.GetType().Name}");
                        }
#endif

                        _behaviour.enabled = false;
                        return;
                    }

                    _behaviour.enabled = value;
                }
            }

            public ManagerComponent(Behaviour behaviour, ChaosControllerAttribute controllerAttribute)
            {
                _behaviour = behaviour;
                _controllerAttribute = controllerAttribute;
            }
        }

        ManagerComponent[] _managerComponents;

        internal static void InitializeObject()
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
