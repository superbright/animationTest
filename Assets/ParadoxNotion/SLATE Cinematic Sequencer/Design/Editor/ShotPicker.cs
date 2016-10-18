#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	///A popup window to select a camera shot with a preview
	public class ShotPicker : PopupWindowContent {

		private System.Action<ShotCamera> callback;
		private Vector2 scrollPos;
		
		///Shows the popup menu at position and with title
		public static void Show(Vector2 pos, System.Action<ShotCamera> callback){
			PopupWindow.Show( new Rect(pos.x, pos.y, 0, 0), new ShotPicker(callback) );
		}

		public ShotPicker(System.Action<ShotCamera> callback){
			this.callback = callback;
		}

		public override Vector2 GetWindowSize(){ return new Vector2(450, 600); }
		public override void OnGUI(Rect rect){
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
			foreach (var shot in Object.FindObjectsOfType<ShotCamera>()){
				if (GUILayout.Button( EditorTools.GetCameraTexture( shot.cam, 400, 200 ) )){
					callback(shot);
					editorWindow.Close();
					return;
				}
				var r = GUILayoutUtility.GetLastRect();
				r.x += 10;
				r.y += 10;
				r.width -= 50;
				r.height = 18;
				GUI.Box(r, "");
				GUI.color = new Color(0,0,0,0.5f);
				GUI.DrawTexture(r, Slate.Styles.whiteTexture);
				GUI.color = Color.white;
				GUI.Label(r, shot.name);
			}
			EditorGUILayout.EndScrollView();
		}
	}
}

#endif