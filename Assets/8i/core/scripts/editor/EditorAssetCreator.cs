using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

using HVR.Core;

namespace HVR.Editor
{
	public class Hvr_AssetCreator
	{
		[MenuItem("Assets/Create/8i/Create HVR Asset")]
		public static void CreateHvrAsset()
		{
			ScriptableObjectUtility.CreateAsset<HvrAsset>();
		}
	}
}
