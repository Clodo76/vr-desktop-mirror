#define VDM_SteamVR

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Xml;

public class VdmDesktop : MonoBehaviour
{
    [HideInInspector]
    public int Screen = 0;
    [HideInInspector]
    public int ScreenIndex = 0;

    [DllImport("user32.dll")]
    static extern void mouse_event(int dwFlags, int dx, int dy,
                      int dwData, int dwExtraInfo);

    [Flags]
    public enum MouseEventFlags
    {
        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010
    }
    
    private VdmDesktopManager m_manager;
    private LineRenderer m_line;
    private Renderer m_renderer;
    private MeshCollider m_collider;

#if VDM_SteamVR
    private float m_lastLeftTriggerClick = 0;
    private float m_lastLeftTouchClick = 0;
    private float m_lastRightTouchClick = 0;
    private float m_distanceBeforeZoom = 0;
    private float m_lastShowClick = 0;
    private bool m_controllerAttach;
#endif

    private bool m_zoom = false;
    private bool m_zoomWithFollowCursor = false;

    private Vector3 m_positionNormal;
    private Quaternion m_rotationNormal;
    private Vector3 m_positionZoomed;
    private Quaternion m_rotationZoomed;
    
    private float m_positionAnimationStart = 0;
    
    // Keyboard and Mouse
    private float m_lastShowClickStart = 0;

    void Start()
    {
        m_manager = transform.parent.GetComponent<VdmDesktopManager>();
        m_line = GetComponent<LineRenderer>();
        m_renderer = GetComponent<Renderer>();
        m_collider = GetComponent<MeshCollider>();

        m_manager.Connect(this);

        Hide();
    }

    public void Update()
    {
        bool skip = false;
        if (Visible() == false)
            skip = true;
        if ((m_controllerAttach) && (m_zoom == false))
            skip = true;
        if(skip == false)    
        {   
            float step = 0;
            if (Time.time - m_positionAnimationStart > 1)
                step = 1;
            else
                step = Time.time - m_positionAnimationStart;

            Vector3 positionDestination;
            Quaternion rotationDestination;

            if (m_zoom)
            {
                positionDestination = m_positionZoomed;
                rotationDestination = m_rotationZoomed;
            }
            else
            {
                positionDestination = m_positionNormal;
                rotationDestination = m_rotationNormal;
            }

            if (transform.position != positionDestination)
                transform.position = Vector3.Lerp(transform.position, positionDestination, step);

            if (transform.rotation != rotationDestination)
                transform.rotation = Quaternion.Lerp(transform.rotation, rotationDestination, step);
            
        }
        
    }
    
    void OnEnable()
    {
    }

    void OnDisable()
    {
        m_manager.Disconnect(this);
    }

    public void HideLine()
    {
        m_line.enabled = false;
    }

    public void Hide()
    {
        m_renderer.enabled = false;
        m_collider.enabled = false;
    }

    public void Show()
    {
        m_renderer.enabled = true;
        m_collider.enabled = true;

        if(m_manager.EnableHackUnityBug)
        {
            m_manager.HackStart();
        }
    }

    public bool Visible()
    {
        return (m_renderer.enabled);
    }

    public void CheckKeyboardAndMouse()
    {
        if (Input.GetKeyDown(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            m_lastShowClickStart = Time.time;

            if (Visible() == false)
            {
                Show();
                m_lastShowClickStart -= 10; // Avoid quick show/close
            }
        }
         
        if (Input.GetKey(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            m_manager.KeyboardDistance += Input.GetAxisRaw("Mouse ScrollWheel");
            m_manager.KeyboardDistance = Mathf.Clamp(m_manager.KeyboardDistance, 0.2f, 100);

            m_positionNormal = Camera.main.transform.position + Camera.main.transform.rotation * new Vector3(0, 0, m_manager.KeyboardDistance);
            m_positionNormal += m_manager.MultiMonitorPositionOffset * ScreenIndex;
            m_rotationNormal = Camera.main.transform.rotation;            
        }

        if (Input.GetKeyUp(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            if (Time.time - m_lastShowClickStart < 0.5f)
            {
                m_lastShowClickStart = 0;

                Hide();
            }
            
        }

        if (m_manager.KeyboardZoom != KeyCode.None)
        {
            if (Input.GetKeyDown(m_manager.KeyboardZoom))
            {
                if (m_zoom == false)
                {
                    m_zoomWithFollowCursor = true;
                    ZoomIn();
                }
                else
                    ZoomOut();
            }

            if( (m_zoom) && (m_zoomWithFollowCursor) )
            {
                VdmDesktopManager.ActionInThisFrame = true;

                m_manager.KeyboardZoomDistance += Input.GetAxisRaw("Mouse ScrollWheel");
                m_manager.KeyboardZoomDistance = Mathf.Clamp(m_manager.KeyboardZoomDistance, 0.2f, 100);

                // Cursor position in world space
                Vector3 cursorPos = m_manager.GetCursorPos();
                cursorPos.x = cursorPos.x / m_manager.GetScreenWidth(Screen);
                cursorPos.y = cursorPos.y / m_manager.GetScreenHeight(Screen);
                cursorPos.y = 1 - cursorPos.y;
                cursorPos.x = cursorPos.x - 0.5f;
                cursorPos.y = cursorPos.y - 0.5f;
                cursorPos = transform.TransformPoint(cursorPos);
                
                Vector3 deltaCursor = transform.position - cursorPos;
                
                m_positionZoomed = Camera.main.transform.position + Camera.main.transform.rotation * new Vector3(0, 0, m_manager.KeyboardZoomDistance);
                m_positionZoomed += m_manager.MultiMonitorPositionOffset * ScreenIndex;
                m_rotationZoomed = Camera.main.transform.rotation;

                m_positionZoomed += deltaCursor;
            }
            
        }
    }

#if VDM_SteamVR 
    public void CheckController(SteamVR_TrackedObject controller)
    {
        SteamVR_Controller.Device input = null;
        if(controller != null)
            input = SteamVR_Controller.Input((int)controller.index);

        Vector3 origin;
        Vector3 direction;
        Quaternion rotation;
        
        origin = controller.transform.position;
        direction = controller.transform.rotation * new Vector3(0, 0, 100);
        rotation = controller.transform.rotation;            
            
        bool hitScreen = false;
        
        // Mouse simulation
        if (Visible())
        {
            RaycastHit[] rcasts = Physics.RaycastAll(origin, direction);

            foreach (RaycastHit rcast in rcasts)
            {
                if (rcast.collider.gameObject != this.gameObject)
                    continue;

                hitScreen = true;

                if (m_manager.ShowLine)
                {
                    m_line.enabled = true;
                    m_line.SetPosition(0, origin);
                    m_line.SetPosition(1, rcast.point);
                }
                else
                {
                    m_line.enabled = false;
                }

                float dx = m_manager.GetScreenWidth(Screen);
                float dy = m_manager.GetScreenHeight(Screen);

                float vx = rcast.textureCoord.x;
                float vy = rcast.textureCoord.y;

                vy = 1 - vy;

                float x = (vx * dx);
                float y = (vy * dy);

                int iX = (int)x;
                int iY = (int)y;

                m_manager.SetCursorPos(iX, iY);

                if (m_lastShowClick == 0)
                {
                    //if (m_manager.EnableZoom)
                    if(m_manager.ViveZoom != VdmDesktopManager.ViveButton.None)
                    {
                        if(input.GetPressDown(MyButtonToViveButton(m_manager.ViveZoom)))
                        {
                            VdmDesktopManager.ActionInThisFrame = true;

                            m_distanceBeforeZoom = (controller.transform.position - rcast.point).magnitude;

                            float distanceDelta = m_distanceBeforeZoom - m_manager.ControllerZoomDistance;

                            Vector3 vectorMove = rotation * new Vector3(0, 0, -distanceDelta);
                            
                            m_positionZoomed = m_positionNormal + vectorMove;
                            m_rotationZoomed = m_rotationNormal;

                            //m_positionZoomed = controller.transform.position + controller.transform.rotation * new Vector3(0, 0, m_manager.ControllerZoomDistance);

                            ZoomIn();
                        }
                        if (input.GetPressUp(MyButtonToViveButton(m_manager.ViveZoom)))
                        {
                            VdmDesktopManager.ActionInThisFrame = true;

                            ZoomOut();
                        }
                    }
                }

                if (m_manager.ViveLeftClick != VdmDesktopManager.ViveButton.None)
                {
                    if (input.GetPressDown(MyButtonToViveButton(m_manager.ViveLeftClick)))
                    {
                        m_lastLeftTriggerClick = Time.time;
                        m_manager.SimulateMouseLeftDown();
                        VdmDesktopManager.ActionInThisFrame = true;
                    }

                    if (input.GetPressUp(MyButtonToViveButton(m_manager.ViveLeftClick)))
                    {
                        if (m_lastLeftTriggerClick != 0)
                        {
                            m_manager.SimulateMouseLeftUp();
                            m_lastLeftTriggerClick = 0;
                            VdmDesktopManager.ActionInThisFrame = true;
                        }
                    }
                }
    
                if (m_manager.ViveTouchPadForClick)
                {
                    if (input.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad) && (input.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).x < -0.2f))
                    {
                        m_lastLeftTouchClick = Time.time;
                        m_manager.SimulateMouseLeftDown();
                        VdmDesktopManager.ActionInThisFrame = true;
                    }

                    if (input.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad) && (input.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).x > -0.2f))
                    {
                        m_lastRightTouchClick = Time.time;
                        m_manager.SimulateMouseRightDown();
                        VdmDesktopManager.ActionInThisFrame = true;
                    }
                }
                if (m_lastLeftTouchClick != 0)
                {
                    if (input.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
                    {
                        m_manager.SimulateMouseLeftUp();
                        m_lastLeftTouchClick = 0;
                        VdmDesktopManager.ActionInThisFrame = true;
                    }
                }

                if (m_lastRightTouchClick != 0)
                {
                    if (input.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
                    {
                        m_manager.SimulateMouseRightUp();
                        m_lastRightTouchClick = 0;
                        VdmDesktopManager.ActionInThisFrame = true;
                    }
                }
                
            }
        }
        
        
        if( (Visible() == false) || (hitScreen) )
        {
            if (input.GetPressDown(MyButtonToViveButton(m_manager.ViveShow)))
            {
                if (m_lastShowClick == 0)
                {
                    VdmDesktopManager.ActionInThisFrame = true;
                    
                    if(hitScreen == false)
                    {
                        Show();
                        
                        // Don't set m_positionNormal.
                        Vector3 startDistance = controller.transform.rotation * new Vector3(0, 0, m_manager.ControllerZoomDistance);
                        transform.position = controller.transform.position + startDistance;
                        transform.rotation = controller.transform.rotation;

                        m_lastShowClick = Time.time - 10;
                    }
                    else
                        m_lastShowClick = Time.time;                    
                    transform.SetParent(controller.transform);
                    m_controllerAttach = true;
                }
            }
        }

        if (input.GetPressUp(MyButtonToViveButton(m_manager.ViveShow)))
        {
            if (m_lastShowClick != 0)
            {
                if (Time.time - m_lastShowClick < 0.5f)
                {
                    Hide();
                }

                VdmDesktopManager.ActionInThisFrame = true;
                m_lastShowClick = 0;
                transform.SetParent(m_manager.transform);
                m_controllerAttach = false;
                m_positionNormal = transform.position;
                m_rotationNormal = transform.rotation;
            }
        }
    }

    Valve.VR.EVRButtonId MyButtonToViveButton(VdmDesktopManager.ViveButton button)
    {
        if (button == VdmDesktopManager.ViveButton.Grip)
            return Valve.VR.EVRButtonId.k_EButton_Grip;
        else if (button == VdmDesktopManager.ViveButton.Trigger)
            return Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
        else if (button == VdmDesktopManager.ViveButton.Menu)
            return Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;
        else
            return Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    }
#endif

    public void ZoomIn()
    {
        m_positionAnimationStart = Time.time;
        m_zoom = true;
    }

    public void ZoomOut()
    {
        m_positionAnimationStart = Time.time;
        
        m_zoom = false;
    }
    
    public void ReInit(Texture2D tex, int width, int height)
    {
        GetComponent<Renderer>().material.mainTexture = tex;
        GetComponent<Renderer>().material.mainTexture.filterMode = m_manager.TextureFilterMode;
        GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1, -1));

        float sx = width;
        float sy = height;
        sx = sx * m_manager.ScreenScaleFactor;
        sy = sy * m_manager.ScreenScaleFactor;
        transform.localScale = new Vector3(sx, sy, 1);
        
    }
    
}