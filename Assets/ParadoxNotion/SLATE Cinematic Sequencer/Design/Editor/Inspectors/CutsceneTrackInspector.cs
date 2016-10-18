#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Linq;

namespace Slate{

	[CustomEditor(typeof(CutsceneTrack), true)]
	public class CutsceneTrackInspector : Editor {

		private CutsceneTrack track{
			get {return (CutsceneTrack)target;}
		}

		public override void OnInspectorGUI(){
			base.OnInspectorGUI();
		}
	}
}

#endif