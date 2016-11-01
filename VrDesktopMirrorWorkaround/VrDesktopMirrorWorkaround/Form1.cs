using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace VrDesktopMirrorWorkaround
{
    public partial class Form1 : Form
    {
        enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window 
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
                          /// <summary>
                          /// Activates the window and displays it as a maximized window.
                          /// </summary>       
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value 
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level 
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is 
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position. 
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
            /// that owns the window is not responding. This flag should only be 
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        public delegate bool CallBackPtr(int hwnd, int lParam);
        private CallBackPtr callBackPtr;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(int handle, out int processId);

        [DllImport("user32.dll")]
        private static extern int EnumWindows(CallBackPtr callPtr, int lPar);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool CloseWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

        [DllImport("user32.dll")]
        static extern IntPtr GetMenu(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        public static uint CANCELMODE = 0x001F;

        public static IntPtr NullIntPtr = new IntPtr(0);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EndMenu();

        public static bool Report(int hwnd, int lParam)
        {
            IntPtr unityWindow = new IntPtr(1182624);

            IntPtr hWnd = new IntPtr(hwnd);
            int processId = 0;

            GetWindowThreadProcessId(hwnd, out processId);

            if (processId == 5736)
            {
                /*
                IntPtr currentMenu = GetMenu(hWnd);
                if (currentMenu.ToInt32() != 0)
                {
                    SetMenu(hWnd, new IntPtr(0));
                    //SetForegroundWindow();                    
                }
                */

                StringBuilder ClassName = new StringBuilder(256);
                //Get the window class name
                GetClassName(hWnd, ClassName, ClassName.Capacity);

                Console.WriteLine("Window handle is " + hwnd + ", pid: " + processId.ToString() + ", class: " + ClassName);

                if(ClassName.ToString() == "#32768")
                {
                    /*
                    CloseWindow(hWnd);
                    ShowWindow(new IntPtr(986178), ShowWindowCommands.Hide);
                    ShowWindow(new IntPtr(986178), ShowWindowCommands.Show);
                    */
                    SendMessage(unityWindow, CANCELMODE, NullIntPtr, NullIntPtr);                    
                }

                /*
                if(ClassName.ToString() == "SysShadow")
                {
                    CloseWindow(hWnd);
                    ShowWindow(new IntPtr(986178), ShowWindowCommands.Hide);
                    ShowWindow(new IntPtr(986178), ShowWindowCommands.Show);
                }
                */
            }
            return true;
        }
        
        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Visible = true;
            Refresh();

            int unityWindow = Convert.ToInt32(Environment.GetCommandLineArgs()[1]);

            IntPtr unityHandlePtr = new IntPtr(unityWindow);

            //callBackPtr = new CallBackPtr(Report);
            
            for(;;)
            {
                IntPtr result = SendMessage(unityHandlePtr, CANCELMODE, NullIntPtr, NullIntPtr);
                Console.WriteLine(result.ToInt32());

                Thread.Sleep(3000);
            }
            
        }

        static List<IntPtr> Childs = new List<IntPtr>();

        static void GetAllChildrenWindowHandles(IntPtr hParent, int maxCount)
        {
            int ct = 0;
            IntPtr prevChild = IntPtr.Zero;
            IntPtr currChild = IntPtr.Zero;
            while (true && ct < maxCount)
            {
                currChild = FindWindowEx(hParent, prevChild, null, null);
                if (currChild == IntPtr.Zero) break;
                Childs.Add(currChild);

                GetAllChildrenWindowHandles(currChild, 2000);
                
                prevChild = currChild;
                ++ct;
            }            
        }
    }
}
