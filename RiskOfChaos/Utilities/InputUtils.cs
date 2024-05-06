using Rewired;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiskOfChaos.Utilities
{
    public static class InputUtils
    {
        static MemoizedGetComponent<TMP_InputField> _currentSelectedObjectTMPInputField = new MemoizedGetComponent<TMP_InputField>();
        static MemoizedGetComponent<InputField> _currentSelectedObjectInputField = new MemoizedGetComponent<InputField>();

        public static bool IsUsingInputField()
        {
            EventSystem eventSystem = MPEventSystemManager.FindEventSystem(ReInput.players.GetPlayer(0));
            if (eventSystem)
            {
                GameObject selectedObject = eventSystem.currentSelectedGameObject;
                if (_currentSelectedObjectTMPInputField.Get(selectedObject) || _currentSelectedObjectInputField.Get(selectedObject))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
