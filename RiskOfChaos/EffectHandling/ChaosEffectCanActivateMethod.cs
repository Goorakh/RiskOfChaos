using HarmonyLib;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct ChaosEffectCanActivateMethod
    {
        readonly MethodInfo _method;
        readonly bool _hasContextArg;

        public ChaosEffectCanActivateMethod(MethodInfo method)
        {
            _method = method;

            ParameterInfo[] methodParameters = _method.GetParameters();
            if (methodParameters.Length > 0)
            {
                if (methodParameters.Length == 1)
                {
                    if (methodParameters[0].ParameterType == typeof(EffectCanActivateContext))
                    {
                        _hasContextArg = true;
                    }
                    else
                    {
                        Log.Error($"Invalid parameter type {methodParameters[0].ParameterType.FullName} in {method.FullDescription()}");
                    }
                }
                else
                {
                    Log.Error($"Invalid parameter count for method {method.FullDescription()}");
                }
            }
        }

        public readonly bool Invoke(in EffectCanActivateContext context)
        {
            if (_hasContextArg)
            {
                return (bool)_method.Invoke(null, new object[] { context });
            }
            else
            {
                return (bool)_method.Invoke(null, null);
            }
        }
    }
}
