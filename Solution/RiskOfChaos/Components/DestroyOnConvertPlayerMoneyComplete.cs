using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class DestroyOnConvertPlayerMoneyComplete : MonoBehaviour
    {
        bool _hasStartedConverting;

        ConvertPlayerMoneyToExperience _moneyConverter;

        void FixedUpdate()
        {
            if (!_hasStartedConverting)
            {
                if (!_moneyConverter)
                {
                    _moneyConverter = GetComponent<ConvertPlayerMoneyToExperience>();
                }

                if (_moneyConverter)
                {
                    _hasStartedConverting = true;
                }
            }
            else
            {
                if (!_moneyConverter)
                {
                    Log.Debug($"Money convert complete, destroying: {name}");
                    Destroy(gameObject);
                }
            }
        }
    }
}
