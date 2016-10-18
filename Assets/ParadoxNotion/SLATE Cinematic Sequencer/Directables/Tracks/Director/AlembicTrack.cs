#if !NO_UTJ

using UnityEngine;
using System.Linq;

namespace Slate{

	[Attachable(typeof(DirectorGroup))]
	[Description("The Alembic Track can sample imported Alembic (.abc) files. This track does not accept any clips. Instead a virtual clip will represent the active exported frame range of the alembic file, plus any extra offset set bellow.\nAlembic files should be placed under 'Assets/StreamingAssets' folder.")]
	public class AlembicTrack : CutsceneTrack {

		public AlembicStreamRoot alembicStream;

		private float abcLinkStartTime{
			get {return alembicStream? alembicStream.m_startTime + alembicStream.m_timeOffset : float.NegativeInfinity;}
		}

		private float abcLinkEndTime{
			get {return alembicStream? alembicStream.m_endTime + alembicStream.m_timeOffset : float.NegativeInfinity;}
		}

		public override string info{
			get {return alembicStream != null? alembicStream.m_pathToAbc.Split('/').LastOrDefault() : "NONE";}
		}

		protected override void OnAfterValidate(){
			if (alembicStream != null){
				alembicStream.Validate();
			}
		}

		protected override bool OnInitialize(){
			if (alembicStream != null){
				alembicStream.Initialize();
				return true;
			}
			
			return false;
		}

		protected override void OnUpdate(float deltaTime, float previousTime){
			if (alembicStream != null){
				alembicStream.Sample(deltaTime);
			}
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public override Texture icon{
			get {return Slate.Styles.alembicIcon;}
		}

		public override void OnTrackTimelineGUI(Rect posRect, Rect timeRect, float cursorTime, System.Func<float, float> TimeToPos){

			var clipRect = posRect;
			clipRect.xMin = TimeToPos(abcLinkStartTime);
			clipRect.xMax = TimeToPos(abcLinkEndTime);

			GUI.color = new Color(1,1,1, 0.3f);
			GUI.Box(clipRect, "", Slate.Styles.clipBoxStyle);

			GUI.color = Color.black;
			var inLabel = (abcLinkStartTime * Prefs.frameRate).ToString("0");
			var outLabel = (abcLinkEndTime * Prefs.frameRate).ToString("0");
			var inSize = new GUIStyle("label").CalcSize(new GUIContent(inLabel));
			var outSize = new GUIStyle("label").CalcSize(new GUIContent(outLabel));
			inSize.x = Mathf.Min(inSize.x, clipRect.width/2);
			outSize.x = Mathf.Min(outSize.x, clipRect.width/2);
			var inRect = new Rect(clipRect.x, clipRect.y, inSize.x, clipRect.height);
			GUI.Label(inRect, inLabel);
			var outRect = new Rect(clipRect.xMax - outSize.x, clipRect.y, outSize.x, clipRect.height);
			GUI.Label(outRect, outLabel);
			GUI.color = Color.white;

			if (clipRect.Contains(Event.current.mousePosition)){
				UnityEditor.EditorGUIUtility.AddCursorRect(clipRect, UnityEditor.MouseCursor.Link);
				if (Event.current.type == EventType.MouseDown){
					CutsceneUtility.selectedObject = this;
					Event.current.Use();
				}
			}
		}

		#endif
	}
}

#endif