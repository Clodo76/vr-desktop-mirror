#include <d3d11.h>
#include <dxgi1_2.h>

#include "IUnityInterface.h"
#include "IUnityGraphics.h"
#include "IUnityGraphicsD3D11.h"

#pragma comment(lib, "dxgi.lib")


class DeskInfo
{
public:

	IDXGIOutputDuplication* g_deskDupl = nullptr;
	ID3D11Texture2D*        g_texture = nullptr;
	bool                    g_isPointerVisible = false;
	int                     g_pointerX = -1;
	int                     g_pointerY = -1;
	int                     g_width = -1;
	int                     g_height = -1;
	int						g_needReinit = 0;
	DXGI_OUTDUPL_FRAME_INFO g_frameInfo;
};

namespace
{
	IUnityInterfaces*       g_unity = nullptr;
	int						g_needReinit = 0;
	
	DeskInfo g_desks[100];
	int g_nDesks = 0;
}



extern "C"
{
	void DesksClean()
	{
		for (int i = 0; i < g_nDesks; i++)
		{
			if (g_desks[i].g_deskDupl != nullptr)
				g_desks[i].g_deskDupl->Release();
		}
		g_nDesks = 0;
	}

	int DeskAdd()
	{
		int i = g_nDesks;
		g_desks[i].g_deskDupl = nullptr;
		g_desks[i].g_texture = nullptr;
		g_desks[i].g_isPointerVisible = false;
		g_desks[i].g_pointerX = -1;
		g_desks[i].g_pointerY = -1;
		g_desks[i].g_width = -1;
		g_desks[i].g_height = -1;
		g_desks[i].g_needReinit = 0;
		g_nDesks++;
		return i;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API DesktopCapturePlugin_Initialize()
	{	
		DesksClean();

		g_needReinit = 0;


		IDXGIFactory1* factory;
		CreateDXGIFactory1(__uuidof(IDXGIFactory1), reinterpret_cast<void**>(&factory));

		IDXGIAdapter1* adapter;
		for (int i = 0; (factory->EnumAdapters1(i, &adapter) != DXGI_ERROR_NOT_FOUND); ++i)
		{
			IDXGIOutput* output;
			for (int j = 0; (adapter->EnumOutputs(j, &output) != DXGI_ERROR_NOT_FOUND); j++)
			{
				DXGI_OUTPUT_DESC outputDesc;
				output->GetDesc(&outputDesc);

				MONITORINFOEX monitorInfo;
				monitorInfo.cbSize = sizeof(MONITORINFOEX);
				GetMonitorInfo(outputDesc.Monitor, &monitorInfo);

				// Maybe in future add a function to identify the primary monitor.
				//if (monitorInfo.dwFlags == MONITORINFOF_PRIMARY)
				{
					int iDesk = DeskAdd();

					g_desks[iDesk].g_width = monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left;					
					g_desks[iDesk].g_height = monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top;

					auto device = g_unity->Get<IUnityGraphicsD3D11>()->GetDevice();
					IDXGIOutput1* output1;
					output1 = reinterpret_cast<IDXGIOutput1*>(output);
					output1->DuplicateOutput(device, &g_desks[iDesk].g_deskDupl);
				}

				output->Release();
			}
			adapter->Release();
		}

		factory->Release();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
	{
		g_unity = unityInterfaces;

		g_needReinit = 1;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload()
	{
		DesksClean();		
	}

	void UNITY_INTERFACE_API OnRenderEvent(int eventId)
	{
		for (int iDesk = 0; iDesk < g_nDesks; iDesk++)
		{
			if (g_desks[iDesk].g_deskDupl == nullptr || g_desks[iDesk].g_texture == nullptr)
			{
				g_needReinit++;
				return;
			}

			IDXGIResource* resource = nullptr;

			const UINT timeout = 0; // ms
			HRESULT resultAcquire = g_desks[iDesk].g_deskDupl->AcquireNextFrame(timeout, &g_desks[iDesk].g_frameInfo, &resource);
			if (resultAcquire != S_OK)
			{
				g_needReinit++;
				return;
			}

			g_desks[iDesk].g_isPointerVisible = (g_desks[iDesk].g_frameInfo.PointerPosition.Visible == TRUE);
			g_desks[iDesk].g_pointerX = g_desks[iDesk].g_frameInfo.PointerPosition.Position.x;
			g_desks[iDesk].g_pointerY = g_desks[iDesk].g_frameInfo.PointerPosition.Position.y;

			ID3D11Texture2D* texture;
			HRESULT resultQuery = resource->QueryInterface(__uuidof(ID3D11Texture2D), reinterpret_cast<void**>(&texture));
			resource->Release();

			if (resultQuery != S_OK)
			{
				g_needReinit++;
				return;
			}

			ID3D11DeviceContext* context;
			auto device = g_unity->Get<IUnityGraphicsD3D11>()->GetDevice();
			device->GetImmediateContext(&context);
			context->CopyResource(g_desks[iDesk].g_texture, texture);

			g_desks[iDesk].g_deskDupl->ReleaseFrame();
		}

		g_needReinit = 0;
	}

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API DesktopCapturePlugin_GetRenderEventFunc()
	{
		return OnRenderEvent;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetNDesks()
	{
		return g_nDesks;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetWidth(int iDesk)
	{
		return g_desks[iDesk].g_width;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetHeight(int iDesk)
	{
		return g_desks[iDesk].g_height;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetNeedReInit()
	{
		return g_needReinit;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_IsPointerVisible(int iDesk)
	{
		return g_desks[iDesk].g_isPointerVisible;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetPointerX(int iDesk)
	{
		return g_desks[iDesk].g_pointerX;
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API DesktopCapturePlugin_GetPointerY(int iDesk)
	{
		return g_desks[iDesk].g_pointerY;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API DesktopCapturePlugin_SetTexturePtr(int iDesk, void* texture)
	{
		g_desks[iDesk].g_texture = reinterpret_cast<ID3D11Texture2D*>(texture);
	}
	
}