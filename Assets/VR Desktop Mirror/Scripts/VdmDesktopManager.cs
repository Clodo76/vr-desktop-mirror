#define VDM_SteamVR

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class VdmDesktopManager : MonoBehaviour {

    public static VdmDesktopManager Instance;

    public static bool ActionInThisFrame = false;
    
    [Tooltip("Keyboard key to show/drag/hide")]
    public KeyCode KeyboardShow = KeyCode.LeftControl;
    [Tooltip("Keyboard key to zoom")]
    public KeyCode KeyboardZoom = KeyCode.LeftAlt;

#if VDM_SteamVR

    public enum ViveButton
    {
        None = 0,
        Trigger = 33,
        Grip = 2,
        Menu = 1,
        Touchpad = 32,
    }

    [Tooltip("Vive button to show/drag/hide")]
    public ViveButton ViveShow = ViveButton.Grip;
    [Tooltip("Vive button to zoom")]
    public ViveButton ViveZoom = ViveButton.Menu;
    [Tooltip("Vive button as mouse click (left)")]
    public ViveButton ViveLeftClick = ViveButton.Trigger;
    [Tooltip("Vive touchpad as mouse left and right click")]
    public bool ViveTouchPadForClick = true;
#endif
    [Tooltip("Distance of the screen if showed with keyboard/mouse. Change it at runtime with 'Show' + Mouse Wheel")]
    public float KeyboardDistance = 1;
    
    [Tooltip("If EnableZoomWithMenu is true, it's the distance between camera and monitor in Zoom Mode")]
    public float KeyboardZoomDistance = 0.5f;
    [Tooltip("If EnableZoomWithMenu is true, it's the distance between controller and monitor in Zoom Mode")]
    public float ControllerZoomDistance = 0.1f;
    [Tooltip("Monitor Scale Factor")]
    public float ScreenScaleFactor = 0.00025f;
    [Tooltip("Show a line between the controller and the cursor pointer on monitor.")]
    public bool ShowLine = true;
    [Tooltip("Monitor texture filtering")]
    public FilterMode TextureFilterMode = FilterMode.Point;
    [Tooltip("Monitor Color Space")]
    public bool LinearColorSpace = false;
    [Tooltip("Multimonitor - 0 for all, otherwise screen number 1..x")]
    public int MultiMonitorScreen = 0;
    [Tooltip("Distance offset between monitors if MultiMonitorScreen==0")]
    public Vector3 MultiMonitorPositionOffset = new Vector3(1,0,0);
    
    [Tooltip("Render Scale - Supersampling. GPU intensive if >1")]
    [Range(1f, 2f)]
    public float RenderScale = 1.0f;

    [Tooltip("Unity Bug hack. If there are active UI elements that stop the playmode/VR, autoclose it.")]
    public bool EnableHackUnityBug = true;

    private System.Diagnostics.Process m_process = null;

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
    [DllImport("DesktopCapture")]
    private static extern void DesktopCapturePlugin_Initialize();
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetNDesks();
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetWidth(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetHeight(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetNeedReInit();
    [DllImport("DesktopCapture")]
    private static extern bool DesktopCapturePlugin_IsPointerVisible(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetPointerX(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetPointerY(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_SetTexturePtr(int iDesk, IntPtr ptr);
    [DllImport("DesktopCapture")]
    private static extern IntPtr DesktopCapturePlugin_GetRenderEventFunc();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public SendInputEventType type;
        public MouseKeybdhardwareInputUnion mkhi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
    
    [StructLayout(LayoutKind.Explicit)]
    struct MouseKeybdhardwareInputUnion
    {
        [FieldOffset(0)]
        public MouseInputData mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public int uMsg;
        public short wParamL;
        public short wParamH;
    }
    struct MouseInputData
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public MouseEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [Flags]
    enum MouseEventFlags : uint
    {
        MOUSEEVENTF_MOVE = 0x0001,
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        MOUSEEVENTF_LEFTUP = 0x0004,
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        MOUSEEVENTF_RIGHTUP = 0x0010,
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080,
        MOUSEEVENTF_XUP = 0x0100,
        MOUSEEVENTF_WHEEL = 0x0800,
        MOUSEEVENTF_VIRTUALDESK = 0x4000,
        MOUSEEVENTF_ABSOLUTE = 0x8000
    }
    enum SendInputEventType : int
    {
        InputMouse,
        InputKeyboard,
        InputHardware
    }

    public enum SPIF
    {
        None = 0x00,
        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        SPIF_UPDATEINIFILE = 0x01,
        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SPIF_SENDCHANGE = 0x02,
        /// <summary>Same as SPIF_SENDCHANGE.</summary>
        SPIF_SENDWININICHANGE = 0x02
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, SPIF fWinIni); // T = any type

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, SPIF fWinIni);

    public const uint SPI_SETMOUSETRAILS = 0x005D;
    public const uint SPI_GETMOUSETRAILS = 0x005E;


    private static int needReinit = 0;

#if VDM_SteamVR
    // Don't worry!
    // If you have a compilation error about missing SteamVR_TrackedObject,
    // comment the first line of this file, the "#define VDM_SteamVR" line.
    private List<SteamVR_TrackedObject> Controllers = new List<SteamVR_TrackedObject>();
#endif

    private List<VdmDesktop> Monitors = new List<VdmDesktop>();

    private bool m_forceMouseTrail = false; // Otherwise cursor is not visible

    // Use this for initialization
    IEnumerator Start () {

        Instance = this;

        ReInit();

        GameObject monitorBase = transform.GetChild(0).gameObject;
        monitorBase.SetActive(false);

        int nScreen = DesktopCapturePlugin_GetNDesks();
        int iScreenIndex = 0;
        for (int s = 0; s < nScreen; s++)
        {
            GameObject monitor = GameObject.Instantiate(monitorBase);

            if ((MultiMonitorScreen != 0) && (MultiMonitorScreen != (s + 1)))
                continue;

            monitor.name = "Monitor " + (s+1).ToString();
            VdmDesktop desk = monitor.AddComponent<VdmDesktop>();
            desk.Screen = s;
            desk.ScreenIndex = iScreenIndex;
            iScreenIndex++;
            monitor.transform.SetParent(transform);

            monitor.SetActive(true);
        }

        yield return new WaitForSeconds(1);

#if VDM_SteamVR
        RefreshControllers();
#endif

        StartCoroutine(OnRender());
    }

    void OnEnable()
    {
#if VDM_SteamVR
        SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
#endif

        HackStart();

        if (GetMouseTrailEnabled() == false)
        {
            m_forceMouseTrail = true;
            SetMouseTrailEnabled(true);
        }
    }

    void OnDisable()
    {
#if VDM_SteamVR
        SteamVR_Utils.Event.Remove("device_connected", OnDeviceConnected);
#endif

        HackStop();

        if (m_forceMouseTrail)
            SetMouseTrailEnabled(false);
    }

    // Update is called once per frame
    void Update () {

        ActionInThisFrame = false;

        if (UnityEngine.VR.VRSettings.renderScale != RenderScale)
            UnityEngine.VR.VRSettings.renderScale = RenderScale;

        needReinit = DesktopCapturePlugin_GetNeedReInit();
        
        if (needReinit > 1000)
            ReInit();

        if(Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.VR.InputTracking.Recenter();    
        }

        foreach (VdmDesktop monitor in Monitors)
        {
            monitor.HideLine();

            monitor.CheckKeyboardAndMouse();                
            
#if VDM_SteamVR
            foreach (SteamVR_TrackedObject controller in Controllers)
            {
                monitor.CheckController(controller);
            }
#endif            
        }
    }
        
    public float GetScreenWidth(int screen)
    {
        return DesktopCapturePlugin_GetWidth(screen);
    }

    public float GetScreenHeight(int screen)
    {
        return DesktopCapturePlugin_GetHeight(screen);
    }

    public bool IsScreenPointerVisible(int screen)
    {
        return DesktopCapturePlugin_IsPointerVisible(screen);
    }

    public int GetScreenPointerX(int screen)
    {
        return DesktopCapturePlugin_GetPointerX(screen);
    }

    public int GetScreenPointerY(int screen)
    {
        return DesktopCapturePlugin_GetPointerY(screen);
    }
    
    public void Connect(VdmDesktop winDesk)
    {
        Monitors.Add(winDesk);

        ReInit();
    }

    public void Disconnect(VdmDesktop winDesk)
    {
        Monitors.Remove(winDesk);
    }

    public void SetCursorPos(float x, float y)
    {
        int iX = (int) x;
        int iY = (int) y;
        SetCursorPos(iX, iY);
    }

    public Vector2 GetCursorPos()
    {
        POINT p;
        GetCursorPos(out p);
        return new Vector2(p.X, p.Y);
    }

    public void SimulateMouseLeftDown()
    {
        INPUT mouseDownInput = new INPUT();
        mouseDownInput.type = SendInputEventType.InputMouse;
        mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
        SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));
    }

    public void SimulateMouseLeftUp()
    {
        INPUT mouseUpInput = new INPUT();
        mouseUpInput.type = SendInputEventType.InputMouse;
        mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
        SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
    }

    public void SimulateMouseRightDown()
    {
        INPUT mouseDownInput = new INPUT();
        mouseDownInput.type = SendInputEventType.InputMouse;
        mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
        SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

    }

    public void SimulateMouseRightUp()
    {
        INPUT mouseUpInput = new INPUT();
        mouseUpInput.type = SendInputEventType.InputMouse;
        mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
        SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
    }

    IEnumerator OnRender()
    {
        for (;;)
        {
            yield return new WaitForEndOfFrame();

            GL.IssuePluginEvent(DesktopCapturePlugin_GetRenderEventFunc(), 0);
        }
    }

    private void ReInit()
    {
        DesktopCapturePlugin_Initialize();
        
        foreach (VdmDesktop winDesk in Monitors)
        {
            int screen = winDesk.Screen;
            int width = DesktopCapturePlugin_GetWidth(screen);
            int height = DesktopCapturePlugin_GetHeight(screen);
            var tex = new Texture2D(width, height, TextureFormat.BGRA32, false, LinearColorSpace);

            DesktopCapturePlugin_SetTexturePtr(screen, tex.GetNativeTexturePtr());
            
            winDesk.ReInit(tex, width, height);                        
        }
    }

#if VDM_SteamVR
    private void RefreshControllers()
    {
        foreach (SteamVR_TrackedObject trackedObj in GameObject.FindObjectsOfType<SteamVR_TrackedObject>())
        {
            if (trackedObj.name.Contains("Controller"))
            {
                if (Controllers.Contains(trackedObj) == false)
                    Controllers.Add(trackedObj);
            }
        }
        Debug.Log("Controllers found: " + Controllers.Count);
    }
#endif

#if VDM_SteamVR
    private void OnDeviceConnected(params object[] args)
    {
        RefreshControllers();        
    }
#endif

    public bool GetMouseTrailEnabled()
    {
        IntPtr Current = new IntPtr(0);
        SystemParametersInfo(SPI_GETMOUSETRAILS, 0, ref Current, SPIF.None);

        return (Current.ToInt32() >= 2);
    }

    public void SetMouseTrailEnabled(bool v)
    {
        IntPtr NullIntPtr = new IntPtr(0);
        if (v)
            SystemParametersInfo(SPI_SETMOUSETRAILS, 2, NullIntPtr, SPIF.None);
        else
            SystemParametersInfo(SPI_SETMOUSETRAILS, 0, NullIntPtr, SPIF.None);
    }

    public void HackStart()
    {
        HackStop();

        string exePath = "Assets\\VR Desktop Mirror\\Hack\\VrDesktopMirrorWorkaround.exe";
        if (System.IO.File.Exists(exePath))
        {
            m_process = new System.Diagnostics.Process();
            m_process.StartInfo.FileName = exePath;
            m_process.StartInfo.CreateNoWindow = true;
            m_process.StartInfo.UseShellExecute = true;
            m_process.StartInfo.Arguments = GetActiveWindow().ToString();
            m_process.Start();
        }
        else
        {
            Debug.Log("VR Desktop Mirror Hack exe not found: " + exePath);
        }
    }

    public void HackStop()
    {
        if (m_process != null)
        {
            if (m_process.HasExited == false)
            {
                m_process.Kill();
            }
        }
        m_process = null;
    }


}
