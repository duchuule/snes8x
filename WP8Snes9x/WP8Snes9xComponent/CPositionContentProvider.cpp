#include "pch.h"
#include "CPositionContentProvider.h"

using namespace PhoneDirect3DXamlAppComponent;

CPositionDirect3DContentProvider::CPositionDirect3DContentProvider(CPositionDirect3DBackground^ controller) :
	m_controller(controller)
{
	m_controller->RequestAdditionalFrame += ref new RequestAdditionalFrameHandler([=] ()
		{
			if (m_host)
			{
				m_host->RequestAdditionalFrame();
			}
		});
}

// IDrawingSurfaceContentProviderNative interface
HRESULT CPositionDirect3DContentProvider::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host, _In_ ID3D11Device1* device)
{
	m_host = host;

	return m_controller->Connect(host, device);
}

void CPositionDirect3DContentProvider::Disconnect()
{
	m_controller->Disconnect();
	m_host = nullptr;
}

HRESULT CPositionDirect3DContentProvider::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Inout_ DrawingSurfaceSizeF* desiredRenderTargetSize)
{
	return m_controller->PrepareResources(presentTargetTime, desiredRenderTargetSize);
}

HRESULT CPositionDirect3DContentProvider::Draw(_In_ ID3D11Device1* device, _In_ ID3D11DeviceContext1* context, _In_ ID3D11RenderTargetView* renderTargetView)
{
	return m_controller->Draw(device, context, renderTargetView);
}