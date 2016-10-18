#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Slate.ActionClips{

	[Category("Composition")]
	[Description("Additive load a Scene for a duration of time, or permanentely if length is zero.\n\nIf 'Update Root Cutscenes' is true, all root cutscenes objects of that scene will also be updated for the duration of the clip with an optional time offset provided.")]
	public class AdditiveScene : DirectorActionClip, ISubClipContainable {

		#if UNITY_EDITOR
		public UnityEditor.SceneAsset sceneAsset;
		#endif

		[SerializeField] [HideInInspector]
		private float _length = 5;
		[SerializeField] [HideInInspector]
		private string _scenePath;

		private Scene subScene;
		public bool updateRootCutscenes = true;
		public float timeOffset;

		private List<Cutscene> rootCutscenes;
		private bool temporary;
		private bool waitLoad;


		float ISubClipContainable.subClipOffset{
			get {return timeOffset;}
			set {timeOffset = value;}
		}

		public override bool isValid{
			get {return !string.IsNullOrEmpty(_scenePath);}
		}

		public override float length{
			get {return _length;}
			set {_length = value;}
		}


		#if UNITY_EDITOR

		public override string info{
			get {return string.Format("        Sub Scene\n        '{0}'", sceneAsset? sceneAsset.name : "NONE");}
		}

		protected override void OnAfterValidate(){
			_scenePath = AssetDatabase.GetAssetPath(sceneAsset);
		}

		#endif		


		protected override void OnEnter(){ temporary = length > 0; Activate(); }
		protected override void OnReverseEnter(){ if (temporary){ Activate(); } }

		protected override void OnUpdate(float time){

			if (Application.isPlaying){ //SceneManger.sceneLoaded doesn't really work
				if (waitLoad && subScene.isLoaded){
					waitLoad = false;
					InitializeSubScene();
				}
			}

			if (temporary && updateRootCutscenes && rootCutscenes != null){
				for (var i = 0; i < rootCutscenes.Count; i++){
					rootCutscenes[i].Sample(time - timeOffset);
				}
			}
		}

		protected override void OnExit(){ if (temporary){ DenitializeSubScene(true); Deactivate(); } }
		protected override void OnReverse(){ DenitializeSubScene(false); Deactivate(); }


		void Activate(){

			if (string.IsNullOrEmpty(_scenePath)){
				return;
			}

			#if UNITY_EDITOR
			if (!Application.isPlaying){
				subScene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
				InitializeSubScene();
				return;
			}
			#endif

			waitLoad = true;
			SceneManager.LoadScene( CleanPath(_scenePath), LoadSceneMode.Additive );
			subScene = SceneManager.GetSceneByPath( _scenePath );
			InitializeSubScene();
		}

		void Deactivate(){

			if (string.IsNullOrEmpty(_scenePath)){
				return;
			}

			#if UNITY_EDITOR
			if (!Application.isPlaying){
				EditorSceneManager.CloseScene(subScene, true);
				return;
			}
			#endif

			SceneManager.UnloadScene( CleanPath(_scenePath) );
			Resources.UnloadUnusedAssets();
		}

		string CleanPath(string path){
			return path.Replace("Assets/", "").Replace(".unity", "");
		}

		void InitializeSubScene(){
			
			rootCutscenes = new List<Cutscene>();
			if (subScene.isLoaded && subScene.IsValid()){
				foreach(var go in subScene.GetRootGameObjects()){

					//clean up cameras
					var cam = go.GetComponent(typeof(IDirectableCamera)) as IDirectableCamera;
					if (cam != null){
						cam.gameObject.SetActive(false);
						continue;
					}

					//cache root cutscenes
					var cutscene = go.GetComponent<Cutscene>();
					if (cutscene != null){
						rootCutscenes.Add(cutscene);
					}
				}
			}
		}

		void DenitializeSubScene(bool forward){
			if (rootCutscenes != null){
				foreach(var cutscene in rootCutscenes){
					if (cutscene != null){
						if (forward){ cutscene.SkipAll(); }
						else { cutscene.Rewind(); }
					}
				}
			}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
			
		protected override void OnClipGUI(Rect rect){
			GUI.DrawTexture(new Rect(0, 0, rect.height, rect.height), Slate.Styles.sceneIcon);
		}		

		#endif

	}
}