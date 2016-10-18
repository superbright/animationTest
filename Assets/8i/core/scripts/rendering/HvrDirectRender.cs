using UnityEngine;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

#if UNITY_EDITOR
using UnityEditor;
#endif

using HVR.Core;
using HVR.Utils;
using HVR.Interface;

namespace HVR
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("8i/Render/HVR Direct Render")]

    public class HvrDirectRender : MonoBehaviour
    {
		const int MAXIMUM_VIEWPORTS = 2;
		GraphicsDeviceType m_initializedGraphicsDeviceType = GraphicsDeviceType.Null;
		HVRViewportInterface[] m_viewports = new HVRViewportInterface [MAXIMUM_VIEWPORTS];
		int m_viewportIndex = 0;        
        int m_currentWidth = 0;
        int m_currentHeight = 0;

        void OnPostRender()
        {
            Camera camera = GetComponent<Camera>();

            int width = camera.pixelWidth;
            int height = camera.pixelHeight;
	        bool resizedViewport = m_currentWidth != 0 && m_currentWidth != width || m_currentHeight != 0 && m_currentHeight != height;
	        m_currentWidth = width;
	        m_currentHeight = height;

            HVRViewportInterface viewport = FlipViewport();
            viewport.SetViewMatrix(camera.worldToCameraMatrix);
            viewport.SetProjMatrix(GL.GetGPUProjectionMatrix(camera.projectionMatrix, false));
            viewport.SetNearFarPlane(camera.nearClipPlane, camera.farClipPlane);
            viewport.SetDimensions(0, 0, width, height);
            viewport.SetFrameBuffer(null);
            HvrStaticInterface.Self().RenderCamera(null, viewport, resizedViewport);
        }

		public HVRViewportInterface FlipViewport()
		{
			HVRViewportInterface viewport = m_viewports[m_viewportIndex];
			if (viewport == null || !viewport.IsValid() || m_initializedGraphicsDeviceType != SystemInfo.graphicsDeviceType)
			{
				m_initializedGraphicsDeviceType = SystemInfo.graphicsDeviceType;
				m_viewports = new HVRViewportInterface [MAXIMUM_VIEWPORTS];
				m_viewportIndex = 0;

				for ( int i = 0; i < MAXIMUM_VIEWPORTS; ++i )
				{
                    m_viewports[i] = new HVRViewportInterface();
				}
				viewport = m_viewports[m_viewportIndex];
			}
			m_viewportIndex = (m_viewportIndex + 1) % MAXIMUM_VIEWPORTS;
			return viewport;
		}
    }
}
