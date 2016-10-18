#if UNITY_EDITOR && !NO_UTJ

using UnityEditor;
using UnityEngine;
using System.Collections;
using OutputType = Slate.Prefs.RenderSettings.OutputType;

namespace Slate{

	public class RenderWindow : EditorWindow {

		private Prefs.RenderSettings settings;
		private ImageSequenceRecorder recorder;
		private MovieRecorder recorder2;
		private FixDeltaTime deltaTimeFixer;
		private bool isRendering;

		private Cutscene cutscene{
			get {return CutsceneEditor.current != null? CutsceneEditor.current.cutscene : null;}
		}

		public static void Open(){
			var window = CreateInstance<RenderWindow>();
			window.ShowUtility();
		}

		void OnEnable(){
			titleContent = new GUIContent("Slate Render Utility");
			settings = Prefs.renderSettings;
			minSize = new Vector2(400, 200);
		}

		void OnDisable(){
			Prefs.renderSettings = settings;
			Done();
		}

		void Update(){

			if (!isRendering || cutscene == null){
				return;
			}

			cutscene.currentTime += 1f/settings.framerate;

			if (cutscene.currentTime >= cutscene.cameraTrack.endTime){
				Done();
			}
		}

		void OnGUI(){

			if (isRendering){
				Repaint();
			}

			settings.outputType = (OutputType)EditorGUILayout.EnumPopup("Output Type", settings.outputType);
			settings.rootFolder = (DataPath.Root)EditorGUILayout.EnumPopup("Root Folder", settings.rootFolder);
			settings.subFolder  = EditorGUILayout.TextField("Sub Folder", settings.subFolder);
			settings.fileName   = EditorGUILayout.TextField("Filename", settings.fileName);

			GUILayout.BeginVertical("box");

			if (settings.outputType == OutputType.PNGImageSequence || settings.outputType == OutputType.EXRImageSequence){
				settings.splitRenderBuffer = EditorGUILayout.Toggle("Render Passes", settings.splitRenderBuffer);
				settings.framerate         = Mathf.Clamp( EditorGUILayout.IntField("Frame Rate", settings.framerate), 2, 60);
			
			} else {

				settings.resolutionWidth = Mathf.Clamp( EditorGUILayout.IntField("Resolution Width", settings.resolutionWidth), 64, 1920 );
				settings.framerate       = Mathf.Clamp( EditorGUILayout.IntField("Frame Rate", settings.framerate), 2, 60);
/*
				if (settings.outputType == OutputType.MP4){
					settings.videoBitrate = EditorGUILayout.IntField("Bitrate", settings.videoBitrate);
				}
*/
			}

			GUILayout.EndVertical();

			if (cutscene == null){
				EditorGUILayout.HelpBox("Cutscene is null or the Cutscene Editor is not open", MessageType.Error);
			}

			GUI.enabled = cutscene != null && !isRendering;
			if (GUILayout.Button(isRendering? "RENDERING..." : "RENDER", GUILayout.Height(50))){
				Begin();
			}

			GUI.enabled = true;
			if (isRendering && GUILayout.Button("CANCEL")){
				Done();
			}
		}


		void Begin(){

			if (isRendering){
				return;
			}

			cutscene.Rewind();

			EditorApplication.ExecuteMenuItem ("Window/Game");
			isRendering = true;
			cutscene.currentTime = cutscene.cameraTrack.startTime;
			cutscene.Sample();

			CutsceneEditor.OnStopInEditor += Done;

			if (Application.isPlaying){
				deltaTimeFixer = AddGetComponent<FixDeltaTime>();
				deltaTimeFixer.targetFramerate = settings.framerate;
			}

			if (settings.outputType == OutputType.PNGImageSequence || settings.outputType == OutputType.EXRImageSequence){

				if (settings.outputType == OutputType.PNGImageSequence){
					recorder = AddGetComponent<PngRecorder>();
				}

				if (settings.outputType == OutputType.EXRImageSequence){
					recorder = AddGetComponent<ExrRecorder>();
				}

				recorder.outputDir = new DataPath(settings.rootFolder, settings.subFolder);
				recorder.fileName = settings.fileName;
				recorder.captureGBuffer = settings.splitRenderBuffer;
				recorder.captureFramebuffer = true;
				recorder.Initialize();
			}



			if (settings.outputType == OutputType.MP4Video || settings.outputType == OutputType.GIFAnimation){

				if (settings.outputType == OutputType.MP4Video){
					recorder2 = AddGetComponent<MP4Recorder>();
					//(recorder2 as MP4Recorder).videoBitrate = settings.videoBitrate;
				}

				if (settings.outputType == OutputType.GIFAnimation){
					recorder2 = AddGetComponent<GifRecorder>();
					if (recorder2 == null){ recorder2 = DirectorCamera.current.cam.gameObject.AddComponent<GifRecorder>(); }
				}

				recorder2.outputDir = new DataPath(settings.rootFolder, settings.subFolder);
				recorder2.fileName = settings.fileName;
				recorder2.resolutionWidth = settings.resolutionWidth;
				recorder2.framerate = settings.framerate;
				recorder2.BeginRecording();
			}
		}

		T AddGetComponent<T>() where T : Component{
			T rec = DirectorCamera.current.cam.GetComponent<T>();
			if (rec == null){
				rec = DirectorCamera.current.cam.gameObject.AddComponent<T>();
			}
			return rec;
		}

		void Done(){

			if (!isRendering){
				return;
			}

			CutsceneEditor.OnStopInEditor -= Done;
			isRendering = false;

			if (recorder != null){
				DestroyImmediate(recorder, true);
			}

			if (recorder2 != null){
				recorder2.Flush();
				recorder2.EndRecording();
				DestroyImmediate(recorder2, true);
			}

			if (deltaTimeFixer != null){
				DestroyImmediate(deltaTimeFixer, true);
			}

			cutscene.Rewind();
		}
	}
}

#endif