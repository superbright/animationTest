using HVR.Interface;
using UnityEngine;
using UnityEngine.Rendering;

namespace HVR.Core
{
    public class RenderInterface
	{
		const int MAXIMUM_VIEWPORTS = 2;
		GraphicsDeviceType m_initializedGraphicsDeviceType = GraphicsDeviceType.Null;
		HVRViewportInterface[] m_viewports = new HVRViewportInterface [MAXIMUM_VIEWPORTS];
		HVRFrameBufferInterface m_frameBuffer;
		int m_viewportIndex = 0;

		public HVRFrameBufferInterface frameBuffer
		{
            get 
            {
                return m_frameBuffer;
            }
		}

		public HVRViewportInterface CurrentViewport() 
		{
			HVRViewportInterface viewport = m_viewports[m_viewportIndex];
			if (viewport == null || !viewport.IsValid() || m_initializedGraphicsDeviceType != SystemInfo.graphicsDeviceType)
			{
				InitializeViewports();
				viewport = m_viewports[m_viewportIndex];
			}
            return viewport;
		}

		public HVRViewportInterface FlipViewport()
		{
			HVRViewportInterface viewport = CurrentViewport();
			m_viewportIndex = (m_viewportIndex + 1) % MAXIMUM_VIEWPORTS;
			return viewport;
		}

		private void InitializeViewports()
		{			
			m_initializedGraphicsDeviceType = SystemInfo.graphicsDeviceType;
			m_frameBuffer = new HVRFrameBufferInterface();
			m_viewports = new HVRViewportInterface [MAXIMUM_VIEWPORTS];
			m_viewportIndex = 0;

			for ( int i = 0; i < MAXIMUM_VIEWPORTS; ++i )
			{
                HVRViewportInterface viewport = new HVRViewportInterface();
				viewport.SetFrameBuffer(m_frameBuffer);
				m_viewports[i] = viewport;
			}
		}
    }
}
