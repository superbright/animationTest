using HVR.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HVR.Editor
{
    class BuildPostProcessor
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string buildExecutablePath)
        {

			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
				// Don't run the post process if we're targeting Android, because the user should be using the custom build pipeline, 
				// found in the 8i/Android menu.
			}else{
				HVRAssetClipCopier.instance.ExportHVRAssetClipData (buildExecutablePath);
			}

			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows |
			   	EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64) {

				CopyWindowsSevenDll (buildExecutablePath);
			}
        }

		public static void CopyWindowsSevenDll(string buildExecutablePath)
		{
			string dllName = "d3dcompiler_47.dll";

			string[] buildPathSplit = buildExecutablePath.Split('/');
			string buildExeName = buildPathSplit[buildPathSplit.Length - 1];
			string buildDirectoryPath = buildExecutablePath.Remove(buildExecutablePath.Length - buildExeName.Length, buildExeName.Length);

			string buildDataPath = buildExecutablePath.Replace (".exe", "") + "_Data";
			string pluginsPath = buildDataPath+"/Plugins";

			string originalDllLocation = pluginsPath + "/" + dllName;
			string newDllLocation = buildDirectoryPath + "/" + dllName;

			File.Copy (originalDllLocation, newDllLocation);
		}
    }

    public class HVRAssetClipCopier
    {
        static HVRAssetClipCopier m_instance;
        public static HVRAssetClipCopier instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new HVRAssetClipCopier();

                return m_instance;
            }
        }

        FileCopier fileCopier;

        public HVRAssetClipCopier()
        {
            fileCopier = null;
            EditorApplication.update += Update;
        }

		public void CleanHRVAssetClipData(string buildExecutablePath){
			string buildDirectoryPath = GetBuildDirectoryFromBuildPath (buildExecutablePath);
			if (Directory.Exists (buildDirectoryPath + "8i/")) {
				Directory.Delete (buildDirectoryPath + "8i/", true);
			}
		}

        public void ExportHVRAssetClipData(string buildExecutablePath)
        {
			CleanHRVAssetClipData (buildExecutablePath);

			string buildDirectoryPath = GetBuildDirectoryFromBuildPath (buildExecutablePath);
			List<HvrAsset> clipsToExport = GetHvrAssetsInBuild ();

            Debug.Log("[HVR] Exporting " + clipsToExport.Count + " HVRAsset Data folder");

            List<string[]> copyMappings = new List<string[]>();

            foreach (HvrAsset clipToExport in clipsToExport) {
				DirectoryInfo assetDataDir = GetAssetDataDir(clipToExport);
				FileInfo assetDataFile = GetAssetDataFile (clipToExport);
				if (assetDataDir == null & assetDataFile == null) {
					string assetDataName;
					if (clipToExport.data == null) {
						assetDataName = "null";
					}
					else{
						assetDataName = clipToExport.data.name;
					}
					EditorUtility.DisplayDialog ("Invalid HVR Asset", "HVR asset '" + clipToExport.name + "' has an invalid data folder attribute. This attribute should be pointing to a valid project folder, but is instead pointing to: '" + assetDataName + "'.\n\nThis HVR asset has not been packaged with your build. This may result in missing HVR actors in your built application.", "Continue");
				} 
				else if(assetDataFile != null){
					string destinationFile = buildDirectoryPath + "8i/" + clipToExport.uniqueID + "/" + assetDataFile.Name;
					copyMappings.Add (new string[] { assetDataFile.FullName, destinationFile });
					
				}
				else {

					List<string> assetFiles = GetAssetFiles (assetDataDir);

					foreach (string assetFile in assetFiles) {
						string relativeFileName = assetFile.Replace (assetDataDir.FullName, "");
						string destinationFile = buildDirectoryPath + "8i/" + clipToExport.uniqueID + "/" + relativeFileName;
						copyMappings.Add (new string[] { assetFile, destinationFile });
					}
				}
            }

            fileCopier = new FileCopier();
            fileCopier.Start(copyMappings.ToArray(), true);
        }

		private string GetBuildDirectoryFromBuildPath(string buildExecutablePath)
		{
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
			{
				// Anything in the Data dir is automatically bundled with the app
				return buildExecutablePath + "/Data/";
			}

			string[] buildPathSplit = buildExecutablePath.Split('/');
			string buildExeName = buildPathSplit[buildPathSplit.Length - 1];
			string buildDirectoryPath = buildExecutablePath.Remove(buildExecutablePath.Length - buildExeName.Length, buildExeName.Length);

			return buildDirectoryPath;
		}

		private List<HvrAsset> GetHvrAssetsInBuild(){
			string[] enabledScenes = EditorHelper.GetEnabledScenesInBuild();

			// Get list of all asset dependencies for build 
			List<string> buildAssetDependencies = AssetDatabase.GetDependencies(enabledScenes).ToList();

			// Find all assets in project
			HvrAsset[] clipsInProject = EditorHelper.GetProjectAssetsOfType<HvrAsset>(".asset");
			List<HvrAsset> clipsToExport = new List<HvrAsset>();

			for (int i = 0; i < clipsInProject.Length; i++)
			{
				string path = AssetDatabase.GetAssetPath(clipsInProject[i].GetInstanceID());

				// If this asset is depended on in the build, then include it.
				if (buildAssetDependencies.Contains (path)) {
					clipsToExport.Add (clipsInProject [i]);
				}
			}

			return clipsToExport;
		}

		private DirectoryInfo GetAssetDataDir(HvrAsset asset){
			if (asset.data == null) {
				return null;
			}
			else{
				string assetDir = EditorHelper.GetFullPathToAsset(asset.data);

				if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
				{
					// Just in case of different file systems returning different results
					assetDir = assetDir.Replace ("/", "\\");
					assetDir = assetDir.Replace (@"\", "\\");
				}

				DirectoryInfo assetDirInfo = new DirectoryInfo (assetDir);
				if (assetDirInfo.Exists) {
					return assetDirInfo;
				} else {
					return null;
				}
			}
		}

		private FileInfo GetAssetDataFile(HvrAsset asset){
			if (asset.data == null) {
				return null;
			}
			else{
				string assetPath = EditorHelper.GetFullPathToAsset(asset.data);

				if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
				{
					// Just in case of different file systems returning different results
					assetPath = assetPath.Replace ("/", "\\");
					assetPath = assetPath.Replace (@"\", "\\");
				}

				Debug.Log (assetPath);
				FileInfo assetFileInfo = new FileInfo (assetPath);
				if (assetFileInfo.Exists) {
					return assetFileInfo;
				} else {
					return null;
				}
			}
		}

		private List<string> GetAssetFiles(DirectoryInfo assetDataDirInfo){
			
			List<string> assetFiles = new List<string>();

			if (assetDataDirInfo.Exists == false) {
				return assetFiles;
			}

			FileInfo[] allAssetFiles = assetDataDirInfo.GetFiles("*.*", SearchOption.AllDirectories);

			for (int j = 0; j < allAssetFiles.Length; j++){
				string file = allAssetFiles[j].FullName;

				// Skip meta files as they should not be used
				if (file.EndsWith (".meta")) {
					continue;
				}

				file = file.Replace("/", "\\");
				file = file.Replace(@"\", "\\");

				assetFiles.Add(allAssetFiles[j].FullName);
			}

			return assetFiles;
		}
			
        public void Update()
        {
            if (fileCopier != null)
            {
                if (fileCopier.copyComplete == false)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                    "Export Progress",
                    fileCopier.GetCopyOutput(),
                    fileCopier.GetProgress()))
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
                else
                {
                    fileCopier = null;
                    EditorUtility.ClearProgressBar();
                }
            }
        }
    }
}
