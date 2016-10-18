#if UNITY_EDITOR

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slate
{
    public class MP4Recorder : MovieRecorder
    {
        public bool captureVideo = true;
        public bool captureAudio = false;
        public int captureEveryNthFrame = 1;
        public int videoBitrate = 8192000;
        public int audioBitrate = 64000;

        fcAPI.fcMP4Context m_ctx;
        fcAPI.fcMP4Config m_mp4conf = fcAPI.fcMP4Config.default_value;
        fcAPI.fcStream m_ostream;

        void InitializeContext()
        {
            m_num_video_frames = 0;

            // initialize scratch buffer
            UpdateScratchBuffer();

            // initialize context and stream
            {
                m_mp4conf = fcAPI.fcMP4Config.default_value;
                m_mp4conf.video = captureVideo;
                m_mp4conf.audio = captureAudio;
                m_mp4conf.video_width = m_scratch_buffer.width;
                m_mp4conf.video_height = m_scratch_buffer.height;
                m_mp4conf.video_max_framerate = 60;
                m_mp4conf.video_bitrate = videoBitrate;
                m_mp4conf.audio_bitrate = audioBitrate;
                m_mp4conf.audio_sampling_rate = AudioSettings.outputSampleRate;
                m_mp4conf.audio_num_channels = fcAPI.fcGetNumAudioChannels();
                m_ctx = fcAPI.fcMP4CreateContext(ref m_mp4conf);

                m_ostream = fcAPI.fcCreateFileStream(GetOutputPath());
                fcAPI.fcMP4AddOutputStream(m_ctx, m_ostream);
            }

            // initialize command buffer
            {
                int tid = Shader.PropertyToID("_TmpFrameBuffer");
                m_cb = new CommandBuffer();
                m_cb.name = "MP4Recorder: copy frame buffer";
                m_cb.GetTemporaryRT(tid, -1, -1, 0, FilterMode.Bilinear);
                m_cb.Blit(BuiltinRenderTextureType.CurrentActive, tid);
                m_cb.SetRenderTarget(m_scratch_buffer);
                m_cb.DrawMesh(m_quad, Matrix4x4.identity, m_mat_copy, 0, 0);
                m_cb.ReleaseTemporaryRT(tid);
            }
        }

        void ReleaseContext()
        {
            if(m_cb != null)
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
                    fcAPI.fcMP4DestroyContext(m_ctx);
                    m_ctx.ptr = IntPtr.Zero;
                }
                if (m_ostream.ptr != IntPtr.Zero)
                {
                    fcAPI.fcDestroyStream(m_ostream);
                    m_ostream.ptr = IntPtr.Zero;
                }
            });
        }

        void UpdateScratchBuffer()
        {
            var cam = GetComponent<Camera>();
            int capture_width = resolutionWidth;
            int capture_height = (int)((float)resolutionWidth / ((float)cam.pixelWidth / (float)cam.pixelHeight));

            if( m_scratch_buffer != null)
            {
                if( m_scratch_buffer.IsCreated() &&
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


        public override bool IsSeekable() { return false; }
        public override bool IsEditable() { return false; }

        public override bool BeginRecording()
        {
            if (m_recording) { return false; }
            m_recording = true;

            InitializeContext();
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterEverything, m_cb);
            Debug.Log("MP4Recorder.BeginRecording(): " + GetOutputPath());
            return true;
        }

        public override bool EndRecording()
        {
            if (!m_recording) { return false; }
            m_recording = false;

            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterEverything, m_cb);
            ReleaseContext();
            Debug.Log("MP4Recorder.EndRecording(): " + GetOutputPath());
            return true;
        }

        public override bool recording
        {
            get { return m_recording; }
            set { m_recording = value; }
        }


        public override string GetOutputPath()
        {
            string ret = outputDir.GetPath();
            if(ret.Length > 0) { ret += "/"; }
            ret += fileName + ".mp4";
            return ret;
        }
        public override RenderTexture GetScratchBuffer() { return m_scratch_buffer; }
        public override int GetFrameCount() { return m_num_video_frames; }

        public override bool Flush()
        {
            return EndRecording();
        }

        public override bool Flush(int begin_frame, int end_frame)
        {
            return EndRecording();
        }

        // N/A
        public override int GetExpectedFileSize(int begin_frame, int end_frame)
        {
            return 0;
        }

        // N/A
        public override void GetFrameData(RenderTexture rt, int frame)
        {
        }

        // N/A
        public override void EraseFrame(int begin_frame, int end_frame)
        {
        }


        public fcAPI.fcMP4Context GetMP4Context() { return m_ctx; }

        void Start()
        {
            
            shCopy = FrameCapturerUtils.GetFrameBufferCopyShader();

            if(captureAudio)
            {
                fcAPI.fcMP4SetFAACPackagePath(Application.streamingAssetsPath + "/UTJ/FrameCapturer/FAAC_SelfBuild.zip");
            }
            fcAPI.fcSetModulePath(outputDir.GetPath());
            fcAPI.fcMP4DownloadCodecBegin();
        }

        void OnEnable()
        {

            shCopy = FrameCapturerUtils.GetFrameBufferCopyShader();

#if UNITY_EDITOR
            if(captureAudio && frameRateMode == FrameRateMode.Constant)
            {
                Debug.LogWarning("MP4Recorder: capture audio with Constant frame rate mode will cause desync");
            }
#endif
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

        void OnAudioFilterRead(float[] samples, int channels)
        {
            if (m_recording && captureAudio)
            {
                if(channels != m_mp4conf.audio_num_channels) {
                    Debug.LogError("MP4Recorder: audio channels mismatch!");
                    return;
                }
                fcAPI.fcMP4AddAudioFrame(m_ctx, samples, samples.Length);
            }
        }

        IEnumerator OnPostRender()
        {
            if (m_recording && captureVideo && Time.frameCount % captureEveryNthFrame == 0)
            {
                yield return Application.isPlaying? new WaitForEndOfFrame() : null;

                double timestamp = Time.unscaledTime;
                if (frameRateMode == FrameRateMode.Constant)
                {
                    timestamp = 1.0 / framerate * m_num_video_frames;
                }

                m_callback = fcAPI.fcMP4AddVideoFrameTexture(m_ctx, m_scratch_buffer, timestamp, m_callback);
                GL.IssuePluginEvent(fcAPI.fcGetRenderEventFunc(), m_callback);
                m_num_video_frames++;
            }
        }
    }

}

#endif