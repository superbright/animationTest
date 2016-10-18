#if UNITY_EDITOR

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


namespace Slate
{
    public class GifRecorder : MovieRecorder
    {
        public int numColors = 256;
        public int captureEveryNthFrame = 2;
        public int keyframe = 30;

        fcAPI.fcGIFContext m_ctx;

        void InitializeContext()
        {
            m_num_video_frames = 0;

            // initialize scratch buffer
            UpdateScratchBuffer();

            // initialize context and stream
            {
                fcAPI.fcGifConfig conf;
                conf.width = m_scratch_buffer.width;
                conf.height = m_scratch_buffer.height;
                conf.num_colors = Mathf.Clamp(numColors, 1, 256);
                conf.max_active_tasks = 0;
                m_ctx = fcAPI.fcGifCreateContext(ref conf);
            }

            // initialize command buffer
            {
                int tid = Shader.PropertyToID("_TmpFrameBuffer");
                m_cb = new CommandBuffer();
                m_cb.name = "GifRecorder: copy frame buffer";
                m_cb.GetTemporaryRT(tid, -1, -1, 0, FilterMode.Bilinear);
                m_cb.Blit(BuiltinRenderTextureType.CurrentActive, tid);
                m_cb.SetRenderTarget(m_scratch_buffer);
                m_cb.DrawMesh(m_quad, Matrix4x4.identity, m_mat_copy, 0, 0);
                m_cb.ReleaseTemporaryRT(tid);
            }
        }

        void ReleaseContext()
        {
            if (m_cb != null)
            {
                m_cb.Release();
                m_cb = null;
            }

            // scratch buffer is kept

            fcAPI.fcGuard(() =>
            {
                fcAPI.fcEraseDeferredCall(m_callback);
                m_callback = 0;

                if (m_ctx.ptr != IntPtr.Zero)
                {
                    fcAPI.fcGifDestroyContext(m_ctx);
                    m_ctx.ptr = IntPtr.Zero;
                }
            });
        }


        void UpdateScratchBuffer()
        {
            var cam = GetComponent<Camera>();
            int capture_width = resolutionWidth;
            int capture_height = (int)((float)resolutionWidth / ((float)cam.pixelWidth / (float)cam.pixelHeight));

            if (m_scratch_buffer != null)
            {
                if (m_scratch_buffer.IsCreated() &&
                    m_scratch_buffer.width == capture_width && m_scratch_buffer.height == capture_height)
                {
                    // update is not needed
                    return;
                }
                else
                {
                    ReleaseScratchBuffer();
                }
            }

            m_scratch_buffer = new RenderTexture(capture_width, capture_height, 0, RenderTextureFormat.ARGB32);
            m_scratch_buffer.wrapMode = TextureWrapMode.Repeat;
            m_scratch_buffer.Create();
        }

        void ReleaseScratchBuffer()
        {
            if (m_scratch_buffer != null)
            {
                m_scratch_buffer.Release();
                m_scratch_buffer = null;
            }
        }


        public override bool IsSeekable() { return true; }
        public override bool IsEditable() { return true; }

        public override bool BeginRecording()
        {
            if (m_recording) { return false; }
            m_recording = true;

            InitializeContext();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterEverything, m_cb);
            Debug.Log("GifRecorder.BeginRecording()");
            return true;
        }

        public override bool EndRecording()
        {
            if (!m_recording) { return false; }
            m_recording = false;

            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterEverything, m_cb);
            ReleaseContext();
            Debug.Log("GifRecorder.EndRecording()");
            return true;
        }

        public override bool recording
        {
            get { return m_recording; }
            set { m_recording = value; }
        }

        public override string GetOutputPath()
        {
            return outputDir.GetPath() + "/" + fileName + ".gif";
        }

        public override bool Flush()
        {
            return Flush(0, -1);
        }

        public override bool Flush(int begin_frame, int end_frame)
        {
            bool ret = false;
            if (m_ctx.ptr != IntPtr.Zero && m_num_video_frames > 0)
            {
                var path = GetOutputPath();
                fcAPI.fcGuard(() =>
                {
                    ret = fcAPI.fcGifWriteFile(m_ctx, path, begin_frame, end_frame);
                });
                Debug.Log("GifRecorder.FlushFile(" + begin_frame + ", " + end_frame + "): " + path);
            }
            return ret;
        }

        public override RenderTexture GetScratchBuffer() { return m_scratch_buffer; }

        public override void EraseFrame(int begin_frame, int end_frame)
        {
            fcAPI.fcGifEraseFrame(m_ctx, begin_frame, end_frame);
        }

        public override int GetExpectedFileSize(int begin_frame = 0, int end_frame = -1)
        {
            return fcAPI.fcGifGetExpectedDataSize(m_ctx, begin_frame, end_frame);
        }

        public override int GetFrameCount()
        {
            return fcAPI.fcGifGetFrameCount(m_ctx);
        }

        public override void GetFrameData(RenderTexture rt, int frame)
        {
            fcAPI.fcGifGetFrameData(m_ctx, rt.GetNativeTexturePtr(), frame);
        }

        public fcAPI.fcGIFContext GetGifContext() { return m_ctx; }


        void OnEnable()
        {
            shCopy = FrameCapturerUtils.GetFrameBufferCopyShader();  
             
            outputDir.CreateDirectory();
            m_quad = FrameCapturerUtils.CreateFullscreenQuad();
            m_mat_copy = new Material(shCopy);

            if (GetComponent<Camera>().targetTexture != null)
            {
                m_mat_copy.EnableKeyword("OFFSCREEN");
            }
        }

        void OnDisable()
        {
            EndRecording();
            ReleaseContext();
            ReleaseScratchBuffer();
        }

        IEnumerator OnPostRender()
        {
            if (m_recording && Time.frameCount % captureEveryNthFrame == 0)
            {
                yield return Application.isPlaying? new WaitForEndOfFrame() : null;

                bool doKey = keyframe > 0 && m_num_video_frames % keyframe == 0;
                double timestamp = Time.unscaledTime;
                if (frameRateMode == FrameRateMode.Constant)
                {
                    timestamp = 1.0 / framerate * m_num_video_frames;
                }

                m_callback = fcAPI.fcGifAddFrameTexture(m_ctx, m_scratch_buffer, doKey, timestamp, m_callback);
                GL.IssuePluginEvent(fcAPI.fcGetRenderEventFunc(), m_callback);
                m_num_video_frames++;
            }
        }
    }

}

#endif