#if !NO_UTJ

using UnityEditor;
using UnityEngine;

namespace Slate{

	[CustomEditor(typeof(AlembicTrack))]
	public class AlembicTrackInspector : Editor {

		private AlembicTrack track{
			get {return (AlembicTrack)target;}
		}

		public override void OnInspectorGUI(){
			
			base.OnInspectorGUI();

			if (track.alembicStream != null){
				float offset = track.alembicStream.m_timeOffset * Prefs.frameRate;
				offset = (int)EditorGUILayout.IntField("Offset", (int)offset);
				track.alembicStream.m_timeOffset = offset * (1f/Prefs.frameRate);
			}

			if (track.alembicStream == null && GUILayout.Button("Import Alembic File")){
				var abc = Commands.ImportAlembicDialog();
				if (abc != null){
					Undo.RecordObject(track, "Alembic Change");
					track.alembicStream = abc;
					abc.transform.parent = track.root.context.transform;
					abc.transform.localPosition = Vector3.zero;
					abc.transform.localRotation = Quaternion.identity;
					abc.transform.localScale = Vector3.one;
				}
			}

			if (track.alembicStream != null && GUILayout.Button("Replace Alembic File")){
				var abc = Commands.ImportAlembicDialog();
				if (abc != null){
					Undo.DestroyObjectImmediate(track.alembicStream.gameObject);
					Undo.RecordObject(track, "Alembic Change");
					track.alembicStream = abc;
					abc.transform.parent = track.root.context.transform;
					abc.transform.localPosition = Vector3.zero;
					abc.transform.localRotation = Quaternion.identity;
					abc.transform.localScale = Vector3.one;					
				}
			}

			if (GUI.changed && track.alembicStream != null){
				EditorUtility.SetDirty(track.alembicStream);
			}
		}
	}
}

#endif