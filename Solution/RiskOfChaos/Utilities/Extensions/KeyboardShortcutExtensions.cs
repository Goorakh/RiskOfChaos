using BepInEx;
using BepInEx.Configuration;
using System.Linq;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class KeyboardShortcutExtensions
    {
        public static bool IsDownIgnoringBlockerKeys(this in KeyboardShortcut shortcut)
        {
            return shortcut.IsDown() || (UnityInput.Current.GetKeyDown(shortcut.MainKey) && checkModifierKeys(shortcut));
        }

        public static bool IsPressedIgnoringBlockerKeys(this in KeyboardShortcut shortcut)
        {
            return shortcut.IsPressed() || (UnityInput.Current.GetKey(shortcut.MainKey) && checkModifierKeys(shortcut));
        }

        public static bool IsUpIgnoringBlockerKeys(this in KeyboardShortcut shortcut)
        {
            return shortcut.IsUp() || (UnityInput.Current.GetKeyUp(shortcut.MainKey) && checkModifierKeys(shortcut));
        }

        static bool checkModifierKeys(in KeyboardShortcut shortcut)
        {
            return shortcut.Modifiers.All(UnityInput.Current.GetKey);
        }
    }
}
