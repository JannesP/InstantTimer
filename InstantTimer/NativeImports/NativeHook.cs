using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InstantTimer.NativeImports
{
    static class NativeHook
    {
        [DllImport("User32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookExA(HookType type, LowLevelKeyboardProc hook, IntPtr hmod, int dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        internal enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        internal struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        internal enum WmKeyUpDown : int
        {
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105
        }

        internal delegate int LowLevelKeyboardProc(int code, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    }
}
