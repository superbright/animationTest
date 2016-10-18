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
    [AddComponentMenu("8i/Render/HVR Render")]

    public class HvrRender : PostEffectsBase
    {
        public RenderInterface renderInterface;

        HVRViewportInterface viewport;
        struct ViewportSettings
        {
            public int height;
            public int width;
        }
        ViewportSettings viewportSettings;

        float viewportSizeMultiplier = 1.0f;
        float viewportSizeMin = 0.2f;
        float viewportSizeMax = 2.0f;

        public enum CompositeMethods
        {
            commandBuffer,
            simple
        }
        public CompositeMethods compositeMethod = CompositeMethods.commandBuffer;


        Shader simpleRender_shader;
        Material simpleRender_material;


        Shader commandBuffer_shader;
        Material commandBuffer_material;
        Mesh commandBuffer_mesh;

        CameraEvent activeCameraEvent = CameraEvent.AfterForwardOpaque;
        CommandBuffer activeCommandBuffer;

        const string commandBuffer_name = "HVRRender";
        const CameraEvent cameraEvent_forwardRendering = CameraEvent.AfterForwardOpaque;
        const CameraEvent cameraEvent_deferredRendering = CameraEvent.AfterFinalPass; // CameraEvent.AfterGBuffer to allow lighting when normals exist

        RenderTexture colorGradedTexture;


        public HvrRender()
        {
            renderInterface = new RenderInterface();

            viewportSettings.height = 600;
            viewportSettings.width = 400;
        }

        void OnEnable()
        {
            GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;

            if (simpleRender_shader == null)
            {
                simpleRender_shader = Resources.Load("shaders/HVRRender_Simple") as Shader;

                if (simpleRender_material == null)
                {
                    simpleRender_material = CheckShaderAndCreateMaterial(simpleRender_shader, simpleRender_material);
                    simpleRender_material.hideFlags = HideFlags.DontSave;
                }
            }

            if (commandBuffer_shader == null)
            {
                commandBuffer_shader = Resources.Load("shaders/HVRRender_CommandBuffer") as Shader;

                if (commandBuffer_material == null)
                {
                    commandBuffer_material = CheckShaderAndCreateMaterial(commandBuffer_shader, commandBuffer_material);
                    commandBuffer_material.hideFlags = HideFlags.DontSave;
                    ResetFadeSettings();
                }
            }

            if (commandBuffer_mesh == null)
            {
                commandBuffer_mesh = CompositeBufferUtils.GenerateQuad();
                commandBuffer_mesh.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void OnLevelWasLoaded(int level)
        {
            SetFadeSettings(0, Color.white, 0, 0, 0);
        }

        void OnDisable()
        {
            CommandBuffer_Remove(activeCameraEvent);
        }

        public override bool CheckResources()
        {
            isSupported = CheckSupport(true, true);
            return isSupported;
        }

        void OnPreRender()
        {
            if (CheckResources() == false)
                return;

            CommandBuffer_Validate();

            Camera cam = GetComponent<Camera>();

            //Viewport Settings
            viewportSizeMultiplier = Mathf.Clamp(viewportSizeMultiplier, viewportSizeMin, viewportSizeMax);

            viewportSettings.width = Mathf.RoundToInt((float)cam.pixelWidth * viewportSizeMultiplier);
            viewportSettings.height = Mathf.RoundToInt((float)cam.pixelHeight * viewportSizeMultiplier);

            viewport = renderInterface.FlipViewport();
            viewport.SetViewMatrix(cam.worldToCameraMatrix);
            viewport.SetProjMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
            viewport.SetNearFarPlane(cam.nearClipPlane, cam.farClipPlane);
            viewport.SetDimensions(0, 0, viewportSettings.width, viewportSettings.height);
            bool resizedViewport = viewport.frameBuffer.Resize(viewportSettings.width, viewportSettings.height);

            if (resizedViewport)
                CommandBuffer_Remove(activeCameraEvent);

            // Render viewport
            HvrStaticInterface.Self().RenderCamera(this, viewport, resizedViewport);

            HvrColorGrading colorGrading = GameObject.FindObjectOfType<HvrColorGrading>();
            if (colorGrading != null)
            {
                if (colorGradedTexture == null || colorGradedTexture.width != viewportSettings.width || colorGradedTexture.height != viewportSettings.height)
                {
                    if (colorGradedTexture)
                        colorGradedTexture.Release();

                    colorGradedTexture = new RenderTexture(viewportSettings.width, viewportSettings.height, 0);
                }

                colorGrading.DoGrade(viewport.frameBuffer.renderColourBuffer, colorGradedTexture);
            }
            else
            {
                if (colorGradedTexture != null)
                {
                    colorGradedTexture.Release();
                    colorGradedTexture = null;
                }
            }

            switch (compositeMethod)
            {
                case CompositeMethods.commandBuffer:
                    // Command Buffer
                    commandBuffer_material.SetTexture("_oCOL", colorGradedTexture ? colorGradedTexture : viewport.frameBuffer.renderColourBuffer);
                    commandBuffer_material.SetTexture("_oDEP", viewport.frameBuffer.renderDepthBuffer);

                    Matrix4x4 inverseProjection = (cam.projectionMatrix.inverse);
                    commandBuffer_material.SetMatrix("_ProjectInverse", inverseProjection);

                    Matrix4x4 inverseViewProjection = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
                    commandBuffer_material.SetMatrix("_ViewProjectInverse", inverseViewProjection);

                    commandBuffer_material.SetInt("g_hdr", cam.hdr ? 1 : 0);
                    break;
                default:
                    break;
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            Camera cam = GetComponent<Camera>();

            switch (compositeMethod)
            {
                case CompositeMethods.simple:
                    // Simple Material
                    simpleRender_material.SetTexture("_Overlay", colorGradedTexture ? colorGradedTexture : viewport.frameBuffer.renderColourBuffer);
                    simpleRender_material.SetTexture("_FrameBufferDepthTexture", viewport.frameBuffer.renderDepthBuffer);
                    Graphics.Blit(src, dst, simpleRender_material);
                    return;
                default:
                    break;
            }

            Graphics.Blit(src, dst);
        }

        void CommandBuffer_Add(CameraEvent cameraEventType)
        {
            activeCommandBuffer = new CommandBuffer();
            activeCommandBuffer.name = commandBuffer_name;
            activeCommandBuffer.DrawMesh(commandBuffer_mesh, Matrix4x4.identity, commandBuffer_material, 0, 0);

            GetComponent<Camera>().AddCommandBuffer(cameraEventType, activeCommandBuffer);
        }
        void CommandBuffer_Remove(CameraEvent cameraEventType)
        {
            CommandBuffer[] buffers = GetComponent<Camera>().GetCommandBuffers(cameraEventType);
            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i].name == commandBuffer_name)
                {
                    GetComponent<Camera>().RemoveCommandBuffer(cameraEventType, buffers[i]);
                }
            }
        }
        void CommandBuffer_Validate()
        {
            Camera cam = GetComponent<Camera>();
            switch (compositeMethod)
            {
                case CompositeMethods.simple:
                    {
                        if (CommandBuffer_IsAttachedToCamera(activeCameraEvent))
                            CommandBuffer_Remove(activeCameraEvent);
                    }
                    break;

                case CompositeMethods.commandBuffer:
                    {
                        switch (cam.actualRenderingPath)
                        {
                            case RenderingPath.Forward:
                                if (activeCameraEvent != cameraEvent_forwardRendering)
                                {
                                    if (CommandBuffer_IsAttachedToCamera(activeCameraEvent))
                                        CommandBuffer_Remove(activeCameraEvent);

                                    activeCameraEvent = cameraEvent_forwardRendering;
                                }

                                break;
                            case RenderingPath.DeferredShading:
                                if (activeCameraEvent != cameraEvent_deferredRendering)
                                {
                                    if (CommandBuffer_IsAttachedToCamera(activeCameraEvent))
                                        CommandBuffer_Remove(activeCameraEvent);

                                    activeCameraEvent = cameraEvent_deferredRendering;
                                }
                                break;
                        }

                        if (!CommandBuffer_IsAttachedToCamera(activeCameraEvent))
                            CommandBuffer_Add(activeCameraEvent);
                    }
                    break;
            }
        }
        bool CommandBuffer_IsAttachedToCamera(CameraEvent cameraEventType)
        {
            CommandBuffer[] buffers = GetComponent<Camera>().GetCommandBuffers(cameraEventType);
            for (int i = 0; i < buffers.Length; i++)
            {
                if (buffers[i].name == commandBuffer_name)
                {
                    return true;
                }
            }

            return false;
        }

        public void ResetFadeSettings()
        {
            SetFadeSettings(0, Color.white, 0, 0, 0);
        }

        public void SetFadeSettings(int isEnabled, Color color, float fadeValue, float minHeight, float maxHeight)
        {
            if (commandBuffer_material == null)
                return;

            commandBuffer_material.SetInt("_FadeEnabled", isEnabled);
            commandBuffer_material.SetColor("_FadeColor", color);
            commandBuffer_material.SetFloat("_FadeValue", fadeValue);
            commandBuffer_material.SetFloat("_FadeHeightMin", minHeight);
            commandBuffer_material.SetFloat("_FadeHeightMax", maxHeight);
        }
    }
}
