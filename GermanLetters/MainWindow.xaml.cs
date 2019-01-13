using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.Windows.Interop;

namespace GermanLetters
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow instance;

        private Dictionary<string, string> chars = new Dictionary<string, string>();

        private List<Keys> hookedKeys = new List<Keys>();
        private Keys triggerKey = Keys.Oemtilde;

        public MainWindow()
        {
            if(Environment.GetCommandLineArgs().Length > 1)
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Environment.GetCommandLineArgs()[1]);
                }
                catch (Exception)
                {
                }
            }

            instance = this;
            _hookID = SetHook(_proc);

            InitializeComponent();

            chars.Add("A", "\u00C4");
            chars.Add("O", "\u00D6");
            chars.Add("U", "\u00DC");
            chars.Add("a", "\u00E4");
            chars.Add("o", "\u00F6");
            chars.Add("u", "\u00FC");
            chars.Add("S", "\u1E9E");
            chars.Add("s", "\u00DF");

            hookedKeys.AddRange(new Keys[] { Keys.A, Keys.O, Keys.U, Keys.S });

            SetWindowVisible(false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
        }

        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private static LowLevelKeyboardProc _proc = HookCallback;

        private static IntPtr _hookID = IntPtr.Zero;


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())

            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }


        private delegate IntPtr LowLevelKeyboardProc(

            int nCode, IntPtr wParam, IntPtr lParam);

        private static bool shiftPressed = false;

        private static bool hooked = false;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys pressedKey = ((Keys)vkCode);

                if (pressedKey == instance.triggerKey)
                {
                    hooked = !hooked;

                    instance.SetWindowVisible(hooked);

                    return (IntPtr)1;
                }

                if (pressedKey == Keys.LShiftKey || pressedKey == Keys.RShiftKey)
                    shiftPressed = false;

                if (hooked && instance.hookedKeys.Contains(pressedKey))
                {
                    if (pressedKey == Keys.A)
                        instance.TypeCharacter(shiftPressed ? instance.chars["A"] : instance.chars["a"]);
                    if (pressedKey == Keys.O)
                        instance.TypeCharacter(shiftPressed ? instance.chars["O"] : instance.chars["o"]);
                    if (pressedKey == Keys.U)
                        instance.TypeCharacter(shiftPressed ? instance.chars["U"] : instance.chars["u"]);
                    if (pressedKey == Keys.S)
                        instance.TypeCharacter(shiftPressed ? instance.chars["S"] : instance.chars["s"]);

                    instance.SetWindowVisible(false);

                    return (IntPtr)1;
                }
            }

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys pressedKey = ((Keys)vkCode);

                if (pressedKey == instance.triggerKey)
                    return (IntPtr)1;

                if (pressedKey == Keys.LShiftKey || pressedKey == Keys.RShiftKey)
                    shiftPressed = true;

                if (hooked && instance.hookedKeys.Contains(pressedKey))
                    return (IntPtr)1;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void SetWindowVisible(bool v)
        {
            if (v)
                Show();
            else
                Hide();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr SetWindowsHookEx(int idHook,

            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool UnhookWindowsHookEx(IntPtr hhk);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,

            IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr GetModuleHandle(string lpModuleName);


        public void TypeCharacter(string str)
        {
            string txt = System.Windows.Clipboard.GetText();
            System.Windows.Clipboard.SetText(str);

            hooked = false;

            new Thread(() =>
            {
                while (shiftPressed)
                    Thread.Sleep(1);

                instance.Dispatcher.Invoke((MethodInvoker)delegate
                {
                    SendKeys.SendWait("^v");
                    new Thread(() =>
                    {
                        Thread.Sleep(300);

                        instance.Dispatcher.Invoke((MethodInvoker)delegate { System.Windows.Clipboard.SetText(txt); });
                    }).Start();
                });
            }).Start();
        }

    }
}
