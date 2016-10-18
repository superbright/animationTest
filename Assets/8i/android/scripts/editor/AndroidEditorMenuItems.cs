using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using HVR.Editor;
using UnityEditor;
using UnityEditorInternal;

using System.IO;


namespace HVR.Android.Editor{
	public class AndroidEditorMenuItems : MonoBehaviour {

		[MenuItem("8i/Android/Set Build Location", false, 10)]
		static void Android_SetBuildPath(MenuCommand menuCommand){
			SetBuildPath ();
		}

		[MenuItem("8i/Android/Build and Run (Assets Packed)", false, 10)]
		static void Android_Build(MenuCommand menuCommand){
			if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android){
				string buildPath = GetBuildPath();
				if (buildPath == "") {
					EditorUtility.DisplayDialog ("Invalid Build Path", "The selected build path is invalid. No build has been performed.", "Close");
					return; 
				}

				string[] buildScenes = GetScenesForBuild();
				string androidAssetsPath = Application.dataPath + "/Plugins/Android/assets/";
				
				HVRAssetClipCopier.instance.ExportHVRAssetClipData(androidAssetsPath);

				BuildPipeline.BuildPlayer(buildScenes, buildPath, BuildTarget.Android, BuildOptions.AutoRunPlayer);

				HVRAssetClipCopier.instance.CleanHRVAssetClipData(androidAssetsPath);
			}
			else{
				EditorUtility.DisplayDialog ("Invalid Platform", "The tool you have attempted to run is used for performing an Android build with packaged HVR assets.\n\nThis tool cannot be used unless the current build target is Android.", "Close");
			}
		}

		[MenuItem("8i/Build External Assets Folder", false, 10)]
		static void Android_BuildExternalAssetsFolder(MenuCommand menuCommand){
			string buildPath =  GetAssetBuildPath();
			if (buildPath == "") {
				EditorUtility.DisplayDialog ("Invalid Build Path", "The selected location was invalid. No assets have been exported.", "Close");
				return; 
			}

			Debug.Log ("[HVR] Starting to export HVR assets to "+buildPath+"/8i");
			HVRAssetClipCopier.instance.CleanHRVAssetClipData(buildPath+"/");
			HVRAssetClipCopier.instance.ExportHVRAssetClipData(buildPath+"/");
			Debug.Log ("[HVR] Finished exporting HVR assets to "+buildPath+"/8i");
		}

		static string GetBuildPath(){
			string buildPath = EditorPrefs.GetString ("8i_Android_BuildPath");
			if (buildPath == null | buildPath == "") {
				return SetBuildPath ();
			} else {
				return buildPath;
			}
		}

		static string SetBuildPath(){
			string buildPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", "", "", "apk");
			EditorPrefs.SetString ("8i_Android_BuildPath", buildPath);
			return buildPath;
		}

		static string GetAssetBuildPath(){
			string buildPath = EditorPrefs.GetString ("8i_Android_AssetBuildPath");
			string defaultLocation = "";
			string defaultFolderName = "";
			if (!(buildPath == null | buildPath == "")) {
				defaultLocation = buildPath;
			}

			buildPath = EditorUtility.SaveFolderPanel("Choose Location for external assets to be saved", defaultLocation, "");
			EditorPrefs.SetString ("8i_Android_AssetBuildPath", buildPath);
			return buildPath;
		}

		static string[] GetScenesForBuild(){

			string[] enabledScenes = EditorHelper.GetEnabledScenesInBuild ();
			List<string> buildScenesList = new List<string> (enabledScenes);

			string unpackScenePath = GetUnpackScenePath ();
			if (unpackScenePath != null) {
				buildScenesList.Insert (0, unpackScenePath);
			}

			return buildScenesList.ToArray();
		}

		static string GetUnpackScenePath(){

			foreach (string scenePath in EditorHelper.GetAllScenes()) {
				if (scenePath.EndsWith ("HVR_Android_UnpackScene.unity")) {
					return scenePath;
				}
			}
			return null;
		}
			

		//[MenuItem("8i/Android/Prepare Assets for Build", false, 10)]
		static void Android_PrepareForBuild(MenuCommand menuCommand){
			string androidAssetsPath = Application.dataPath + "/Plugins/Android/assets/";
			HVRAssetClipCopier.instance.ExportHVRAssetClipData(androidAssetsPath);
		}

	}
}
