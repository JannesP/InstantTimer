using InstantTimer.NativeImports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InstantTimer.Utility
{
    static class NativeHookWrapper
    {
        public static event EventHandler<KeyboardHookEventArgs> KeyboardHookCalled;
        private static IntPtr _currentHook = IntPtr.Zero;
        private static NativeHook.LowLevelKeyboardProc _hookFunction = new NativeHook.LowLevelKeyboardProc(HookCallback);

        public static void ActivateKeyboardHook()
        {
            if (_currentHook != IntPtr.Zero) throw new Exception("LL Keyboard Hook already set.");
#pragma warning disable CS0618 // Type or member is obsolete
            _currentHook = NativeHook.SetWindowsHookExA(NativeHook.HookType.WH_KEYBOARD_LL, _hookFunction, IntPtr.Zero, 0);
#pragma warning restore CS0618 // Type or member is obsolete
            if (_currentHook == IntPtr.Zero)
            {
                int res = Marshal.GetLastWin32Error();
                throw new Exception($"Error setting up LL Hook: {res}");
            }
        }

        public static void DisableKeyboardHook()
        {
            if (_currentHook != IntPtr.Zero)
            {
                NativeHook.UnhookWindowsHookEx(_currentHook);
                _currentHook = IntPtr.Zero;
            }
        }

        private static int HookCallback(int code, IntPtr wParam, ref NativeHook.KBDLLHOOKSTRUCT lParam)
        {
            if (code < 0) return NativeHook.CallNextHookEx(IntPtr.Zero, code, wParam, ref lParam);
            KeyboardHookEventArgs.EventType evtType;
            switch ((NativeHook.WmKeyUpDown)wParam.ToInt32())
            {
                case NativeHook.WmKeyUpDown.WM_KEYDOWN:
                case NativeHook.WmKeyUpDown.WM_SYSKEYDOWN:
                    evtType = KeyboardHookEventArgs.EventType.KeyDown;
                    break;
                case NativeHook.WmKeyUpDown.WM_KEYUP:
                case NativeHook.WmKeyUpDown.WM_SYSKEYUP:
                    evtType = KeyboardHookEventArgs.EventType.KeyUp;
                    break;
                default:
                    Trace.TraceWarning($"Received unexpected WM_KEYXXXX ({wParam.ToInt32()}) in HookCallback");
                    return NativeHook.CallNextHookEx(IntPtr.Zero, code, wParam, ref lParam);
            }

            var evt = new KeyboardHookEventArgs(KeyInterop.KeyFromVirtualKey(lParam.vkCode), evtType);
            KeyboardHookCalled?.Invoke(null, evt);
            return evt.Handled ? 1 : NativeHook.CallNextHookEx(IntPtr.Zero, code, wParam, ref lParam);
        }
    }

    public class KeyboardHookEventArgs : EventArgs
    {
        public KeyboardHookEventArgs(Key key, EventType type)
        {
            Type = type;
            Key = key;
        }
        public EventType Type { get; private set; }
        public Key Key { get; private set; }
        public bool Handled { get; set; } = false;
        public enum EventType
        {
            KeyUp,
            KeyDown
        }
    }
}
