#if UNITY_EDITOR

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR


namespace Slate {

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public abstract class ImageSequenceRecorder : MonoBehaviour{

        public bool captureFramebuffer = true;
        public bool captureGBuffer = true;
        public DataPath outputDir = new DataPath(DataPath.Root.ProjectDirectory, "SlateRenders");
        public string fileName;
        public Shader shCopy;        

        protected Material m_mat_copy;
        protected Mesh m_quad;
        protected CommandBuffer m_cb_copy_fb;
        protected CommandBuffer m_cb_copy_gb;
        protected RenderTexture m_frame_buffer;
        protected RenderTexture[] m_gbuffer;
        protected int[] m_callbacks_fb;
        protected int[] m_callbacks_gb;
        protected int currentFrame;

        abstract public void Initialize();
        abstract public void Release();
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public abstract class MovieRecorder : MonoBehaviour {

        public enum FrameRateMode {
            Variable,
            Constant,
        }

        public DataPath outputDir = new DataPath(DataPath.Root.ProjectDirectory, "SlateRenders");
        public string fileName;
        public int resolutionWidth = 640;
        public FrameRateMode frameRateMode = FrameRateMode.Constant;
        public int framerate = 30;
        public Shader shCopy;

        protected Material m_mat_copy;
        protected Mesh m_quad;
        protected CommandBuffer m_cb;
        protected RenderTexture m_scratch_buffer;
        protected int m_callback;
        protected int m_num_video_frames;
        protected bool m_recording = false;

        public abstract bool IsSeekable();
        public abstract bool IsEditable();

        public abstract bool BeginRecording();
        public abstract bool EndRecording();
        public abstract bool recording { get; set; }
        public abstract string GetOutputPath();

        public abstract bool Flush();
        public abstract RenderTexture GetScratchBuffer();
        public abstract int GetFrameCount();

        // available only if IsSeekable() is true
        public abstract bool Flush(int begin_frame, int end_frame);
        // available only if IsSeekable() is true
        public abstract int GetExpectedFileSize(int begin_frame, int end_frame);
        // available only if IsSeekable() is true
        public abstract void GetFrameData(RenderTexture rt, int frame);

        // available only if IsEditable() is true
        public abstract void EraseFrame(int begin_frame, int end_frame);
    }
}

#endif