using UnityEngine;

namespace RiskOfChaos.Utilities.Pool
{
    public class ScriptableObjectPool<T> : GenericPool<T> where T : ScriptableObject
    {
        protected override T createNew(int creationID)
        {
            T result = ScriptableObject.CreateInstance<T>();
            result.name = $"{typeof(T).Name} #{creationID} (Pooled)";
            return result;
        }
    }
}
