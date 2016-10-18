using UnityEngine;
using HVR.Core;
using HVR.Interface;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HVR
{
    public class HvrStaticInterface
    {
        static private HvrStaticInterface m_instance;
        static public HvrStaticInterface Self()
        {
            if (m_instance == null)
                m_instance = new HvrStaticInterface();

            return m_instance;
        }

        public HvrStaticInterface()
        {
#if UNITY_EDITOR
            EditorApplication.update += UnityEditorUpdate;
#endif
        }


        public void UnityEditorUpdate()
        {
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
            {
                float editorTime = (float)EditorApplication.timeSinceStartup;

                StaticInterface.Self().UnityEditorUpdate(editorTime);
                SceneView.RepaintAll();
            }
#endif
        }


		public void ResetFrameBuffersAndMeshes()
		{
			StaticInterface.Self().ResetFrameBuffersAndMeshes();
		}

        void Update()
        {
            if (Application.isPlaying)
            {
                StaticInterface.Self().Update(Time.unscaledTime);
            }
        }

        void LateUpdate()
        {
            StaticInterface.Self().LateUpdate();
        }

		public void RenderCamera(HvrRender hvrRender, HVRViewportInterface viewport, bool resizedViewport = false)
		{
			// If the Unity viewport has been resized and an OpenGL renderer 
			// is in use then Unity will have recreated the main OpenGL 
			// context so all framebuffer and vertex attribute objects that
			// are not shareable need to be destroyed and recreated.
			//
			// The extra call to `PlayerInterface.PreRender()` in this case is
			// to make sure that the vertex buffers are populated for the 
			// render call below to avoid flickering.
			if (resizedViewport)
            {
				StaticInterface.Self().ResetFrameBuffersAndMeshes();
                PlayerInterface.PreRender(SceneInterface.Self().hvrSceneInterface);
                PlayerInterface.PreRender(SceneInterface.Self().hvrSceneInterface);
            }

			Update();
			LateUpdate();

            if (!SceneInterface.Self().sceneHasPreRendered)
            {
                PlayerInterface.PreRender(SceneInterface.Self().hvrSceneInterface);
                SceneInterface.Self().sceneHasPreRendered = true;
            }

            PlayerInterface.Render(SceneInterface.Self().hvrSceneInterface, viewport);
        }
    }
}
