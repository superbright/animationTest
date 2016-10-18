using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
	public class EditorHelper
	{
		/// <summary>
		/// Used to get assets of a certain type and file extension from entire project
		/// </summary>
		/// <param name="type">The type to retrieve. eg typeof(GameObject).</param>
		/// <param name="fileExtension">The file extention the type uses eg ".prefab".</param>
		/// <returns>An Object array of assets.</returns>
		public static T[] GetProjectAssetsOfType<T>(string fileExtension) where T : UnityEngine.Object
		{
			List<T> tempObjects = new List<T>();
			DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
			FileInfo[] goFileInfo = directory.GetFiles("*" + fileExtension, SearchOption.AllDirectories);

			int i = 0;
			int goFileInfoLength = goFileInfo.Length;
			FileInfo tempGoFileInfo; string tempFilePath;
			T tempGO;
			for (; i < goFileInfoLength; i++)
			{
				tempGoFileInfo = goFileInfo[i];
				if (tempGoFileInfo == null)
					continue;

				tempFilePath = tempGoFileInfo.FullName;
				tempFilePath = tempFilePath.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
				tempGO = AssetDatabase.LoadAssetAtPath(tempFilePath, typeof(T)) as T;
				if (tempGO == null)
				{
					continue;
				}
				else if (!(tempGO is T))
				{
					continue;
				}

				tempObjects.Add(tempGO);
			}

			return tempObjects.ToArray();
		}

		public static string GetFullPathToAsset(UnityEngine.Object asset)
		{
			string datapath = Application.dataPath;
			datapath = datapath.Substring(0, datapath.Length - 6);

			string assetPath = AssetDatabase.GetAssetPath(asset.GetInstanceID());

			return datapath + assetPath;
		}

		public static string[] GetEnabledScenesInBuild()
		{
			return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
		}

		public static string[] GetAllScenesInBuild()
		{
			return (from scene in EditorBuildSettings.scenes select scene.path).ToArray();
		}

		public static string[] GetAllScenes()
		{
			return (from scene in AssetDatabase.GetAllAssetPaths() where scene.EndsWith(".unity") select scene).ToArray();
		}
	}
}
