using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace VrDesktopMirrorWorkaround
{
    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static uint CANCELMODE = 0x001F;

        public static IntPtr NullIntPtr = new IntPtr(0);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            */

            int unityWindow = Convert.ToInt32(Environment.GetCommandLineArgs()[1]);

            IntPtr unityHandlePtr = new IntPtr(unityWindow);

            //callBackPtr = new CallBackPtr(Report);

            for (;;)
            {
                IntPtr result = SendMessage(unityHandlePtr, CANCELMODE, NullIntPtr, NullIntPtr);
                Console.WriteLine(result.ToInt32());

                Thread.Sleep(3000);
            }
        }
    }
}
