using HarmonyLib;
using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct ChaosEffectCanActivateMethod
    {
        readonly string _methodDescription;
        readonly EffectCanActivateDelegate _canActivate;

        public ChaosEffectCanActivateMethod(MethodInfo method)
        {
            _methodDescription = method.FullDescription();

            Delegate canActivateDelegate = Delegate.CreateDelegate(typeof(EffectCanActivateDelegate), method, false);
            if (canActivateDelegate != null)
            {
                _canActivate = canActivateDelegate as EffectCanActivateDelegate;
            }
            else
            {
                ParameterInfo[] methodParameters = method.GetParameters();
                switch (methodParameters.Length)
                {
                    case 0:
                        _canActivate = (in EffectCanActivateContext context) =>
                        {
                            return (bool)method.Invoke(null, null);
                        };
                        break;
                    case 1:
                        if (methodParameters[0].ParameterType == typeof(EffectCanActivateContext))
                        {
                            _canActivate = (in EffectCanActivateContext context) =>
                            {
                                return (bool)method.Invoke(null, [ context ]);
                            };
                        }
                        else
                        {
                            Log.Error($"Invalid parameter type {methodParameters[0].ParameterType.FullName} in {_methodDescription}");
                        }

                        break;
                    default:
                        Log.Error($"Invalid parameter count for method {_methodDescription}: {methodParameters.Length}");
                        break;
                }
            }
        }

        public readonly bool Invoke(in EffectCanActivateContext context)
        {
            if (_canActivate == null)
            {
                Log.Warning($"Null canActivate delegate for method {_methodDescription}, defaulting to false");
                return false;
            }

            try
            {
                return _canActivate(context);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Caught exception in effect CanActivate method {_methodDescription}, defaulting to not activatable: {e}");
                return false;
            }
        }
    }
}
