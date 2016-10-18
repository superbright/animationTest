#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Slate{

	public class CutsceneEditor : EditorWindow{

		enum EditorPlayback{
			Stoped,
			PlayingForwards,
			PlayingBackwards
		}

		public static CutsceneEditor current;

		private Cutscene _cutscene;
		private int _cutsceneID;
		
		public float length{
			get {return cutscene.length;}
			set {cutscene.length = value;}
		}

		public float viewTimeMin{
			get {return cutscene.viewTimeMin;}
			set {cutscene.viewTimeMin = value;}
		}

		public float viewTimeMax{
			get {return cutscene.viewTimeMax;}
			set {cutscene.viewTimeMax = value;}
		}

		public float maxTime{
			get {return Mathf.Max(viewTimeMax, length); }
		}

		public float viewTime{
			get {return viewTimeMax - viewTimeMin;}
		}

		public static event System.Action OnStopInEditor;

		private readonly float toolbarHeight    = 18;
		private readonly float leftMargin       = 210;
		private readonly float rightMargin      = 16;
		private readonly float topMargin        = 40;
		private readonly float groupHeight      = 20;
		private readonly float trackMargins     = 4;
		private readonly float groupRightMargin = 4;
		private readonly float trackRightMargin = 4;


		private Rect topLeftRect;
		private Rect topMiddleRect;
		//private Rect topRightRect;
		private Rect leftRect;
		private Rect centerRect;
		//private Rect rightRect;


		private Dictionary<int, ActionClipWrapper> clipWrappers;
		private EditorPlayback editorPlayback            = EditorPlayback.Stoped;
		private Cutscene.WrapMode editorPlaybackWrapMode = Cutscene.WrapMode.Loop;	
		private bool anyClipDragging                     = false;
		private Vector2 scrollPos                        = Vector2.zero;
		private float totalHeight                        = 0;
		private bool movingScrubCarret                   = false;
		private bool movingEndCarret                     = false;
		private float editorPreviousTime                 = 0;
		private CutsceneTrack pickedTrack                = null;
		private CutsceneGroup pickedGroup                = null;
		private bool mouseButton2Down;
		private float lastPlayTime;
		private Vector2? multiSelectStartPos;
		private ActionClipWrapper[] multiSelection;
		private Vector2 mousePosition;
		private bool showDragDropInfo                    = true;
		private Section draggedSection                   = null;
		private float draggedSectionShiftingMin;
		private bool willRepaint;
		private bool willDirty;
		private bool willResample;
		private int repaintCooldown;
		private System.Action OnDoPopup;
		private bool helpButtonPressed;


		public Cutscene cutscene{
			get
			{
				if (_cutscene == null)
					_cutscene = EditorUtility.InstanceIDToObject(_cutsceneID) as Cutscene;
				return _cutscene;
			}
			private set
			{
				_cutscene = value;
				if (value != null)
					_cutsceneID = value.GetInstanceID();
			}
		}

		private bool isProSkin{
			get {return EditorGUIUtility.isProSkin;}
		}

		private bool isPrefab{
			get {return cutscene != null && PrefabUtility.GetPrefabType(cutscene) == PrefabType.Prefab;}
		}

		private Texture2D whiteTexture{
			get {return Slate.Styles.whiteTexture;}
		}


		//HELPERS//
		float TimeToPos(float time){
			return (time - viewTimeMin) / viewTime * centerRect.width;
		}

		float PosToTime(float pos){
			return (pos - leftMargin) / centerRect.width * viewTime + viewTimeMin;
		}

		float SnapTime(float time){
			return (Mathf.Round(time / Prefs.snapInterval) * Prefs.snapInterval);
		}

		void SafeDoAction(System.Action call){
			var time = cutscene.currentTime;
			Stop(true);
			call();
			cutscene.currentTime = time;
		}

		void DrawGuideLine(float xPos, Color color){
			if (xPos > 0 && xPos < centerRect.xMax - leftRect.width){
				var guideRect = new Rect(xPos + centerRect.x, centerRect.y, 1, centerRect.height);
				GUI.color = color;
				GUI.DrawTexture(guideRect, whiteTexture);
				GUI.color = Color.white;
			}
		}

		void AddCursorRect(Rect rect, MouseCursor type ){
			EditorGUIUtility.AddCursorRect(rect, type);
			willRepaint = true;
		}

		void DoPopup(System.Action Call){
			OnDoPopup = Call;
		}
		///////////


		///Opens Editor for a specific Cutscene
		public static void ShowWindow(Cutscene newCutscene){
			var window = EditorWindow.GetWindow(typeof(CutsceneEditor)) as CutsceneEditor;
			window.InitializeAll(newCutscene);
			window.Show();
		}

		void OnEnable(){
			EditorApplication.playmodeStateChanged += InitializeAll;
			EditorApplication.playmodeStateChanged += ()=>{ repaintCooldown = 4; };
			EditorApplication.update += OnEditorUpdate;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Tools.hidden = false;
			current = this;

			titleContent = new GUIContent("SLATE", Styles.cutsceneIconOpen);
			wantsMouseMove = true;
			autoRepaintOnSceneChange = true;
			minSize = new Vector2(500, 250);

			willRepaint = true;

			draggedSection = null; //Find out what is going on with draggedSection for not becomming null.
			InitializeAll();
		}



		void OnDisable(){
			EditorApplication.playmodeStateChanged -= InitializeAll;
			EditorApplication.update -= OnEditorUpdate;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Tools.hidden = false;
			if (cutscene != null && !Application.isPlaying){
				Stop(true);
			}
			current = null;
		}


		//Set a new view when a script is selected in Unity's tab
		void OnSelectionChange(){
			if (Selection.activeGameObject != null){
				var cut = Selection.activeGameObject.GetComponent<Cutscene>();
				if (cut != null && cutscene != cut){
					InitializeAll(cut);
				}
			}
		}

		///Initialize everything
		void InitializeAll(){InitializeAll(cutscene);}
		void InitializeAll(Cutscene newCutscene){

			//first stop current cut if any
			if (cutscene != null){
				if (!Application.isPlaying){
					Stop(true);
				}
			}

			//set the new
			if (newCutscene != null){
				cutscene = newCutscene;
				CutsceneUtility.selectedObject = null;
				multiSelection = null;
				InitBoxes();
				if (!Application.isPlaying){
					Stop(true);
				}
			}

			Repaint();
		}



		//Play button pressed or otherwise started
		public void Play(Cutscene.WrapMode wrapMode = Cutscene.WrapMode.Loop, System.Action callback = null){

			titleContent = new GUIContent("SLATE", Styles.cutsceneIconClose);
			
			if (Application.isPlaying){
				var temp = cutscene.currentTime == length? 0 : cutscene.currentTime;
				cutscene.Play(0, length, cutscene.defaultWrapMode, callback, Cutscene.PlayingDirection.Forwards);
				cutscene.currentTime = temp;
				return;
			}

			editorPlaybackWrapMode = wrapMode;
			editorPlayback = EditorPlayback.PlayingForwards;
			editorPreviousTime = Time.realtimeSinceStartup; 
			lastPlayTime = cutscene.currentTime;
			OnStopInEditor = callback != null? callback : OnStopInEditor;
		}

		//Play reverse button pressed
		public void PlayReverse(){

			titleContent = new GUIContent("SLATE", Styles.cutsceneIconClose);

			if (Application.isPlaying){
				var temp = cutscene.currentTime == 0? length : cutscene.currentTime;
				cutscene.Play(0, length, cutscene.defaultWrapMode, null, Cutscene.PlayingDirection.Backwards);
				cutscene.currentTime = temp;
				return;
			}

			editorPlayback = EditorPlayback.PlayingBackwards;
			editorPreviousTime = Time.realtimeSinceStartup; 
			if (cutscene.currentTime == 0){
				cutscene.currentTime = length;
				lastPlayTime = 0;
			} else {
				lastPlayTime = cutscene.currentTime;			
			}
		}

		//Pause button pressed
		public void Pause(){

			titleContent = new GUIContent("SLATE", Styles.cutsceneIconOpen);

			if (Application.isPlaying){
				if (cutscene.isActive){
					cutscene.Pause();
					return;
				}
			}

			editorPlayback = EditorPlayback.Stoped;
			if (OnStopInEditor != null){
				OnStopInEditor();
				OnStopInEditor = null;
			}
		}

		//Stop button pressed or otherwise reset the scrubbing/previewing
		public void Stop(bool forceRewind){

			titleContent = new GUIContent("SLATE", Styles.cutsceneIconOpen);

			if (Application.isPlaying){
				if (cutscene.isActive){
					cutscene.Stop();
					return;
				}
			}

			if (OnStopInEditor != null){
				OnStopInEditor();
				OnStopInEditor = null;
			}

			//Super important to Sample instead of setting time here, so that we rewind correct if need be. 0 rewinds.
			cutscene.Sample( editorPlayback != EditorPlayback.Stoped && !forceRewind? lastPlayTime : 0 );
			editorPlayback = EditorPlayback.Stoped;
			willRepaint = true;
		}

		///Steps time backwards to the previous key time
		void StepForward(){
			var action = CutsceneUtility.selectedObject as ActionClip;
			if (action != null){
				var time = action.animationData.GetKeyNext( cutscene.currentTime - action.startTime );
				cutscene.currentTime = time + action.startTime;
				return;
			}
			if (cutscene.currentTime == cutscene.length){
				cutscene.currentTime = 0;
				return;
			}
			cutscene.currentTime = cutscene.GetKeyTimes().FirstOrDefault(t => t > cutscene.currentTime + 0.01f);
		}

		///Steps time forward to the next key time
		void StepBackward(){
			var action = CutsceneUtility.selectedObject as ActionClip;
			if (action != null){
				var time = action.animationData.GetKeyPrevious( cutscene.currentTime - action.startTime );
				cutscene.currentTime = time + action.startTime;
				return;
			}
			if (cutscene.currentTime == 0){
				cutscene.currentTime = cutscene.length;
				return;
			}
			cutscene.currentTime = cutscene.GetKeyTimes().LastOrDefault(t => t < cutscene.currentTime - 0.01f);
		}

		void OnEditorUpdate(){

			if (cutscene == null){
				return;
			}
			
			if (EditorApplication.isCompiling){
				Stop(true);
				return;
			}

			//if cutscene active, it will sample and update itself.
			if (cutscene.isActive){
				return;
			}

			//Sample at it's current time.
			cutscene.Sample();

			//Nothing.
			if (editorPlayback == EditorPlayback.Stoped){
				return;
			}

			//Playback.
			if (cutscene.currentTime >= length && editorPlayback == EditorPlayback.PlayingForwards){
				if (editorPlaybackWrapMode == Cutscene.WrapMode.Once){
					Stop(true);
					return;
				}
				if (editorPlaybackWrapMode == Cutscene.WrapMode.Loop){
					cutscene.currentTime = 0;
					return;
				}
			}

			if (cutscene.currentTime <= 0 && editorPlayback == EditorPlayback.PlayingBackwards){
				Stop(true);
				return;
			}

			var delta = (Time.realtimeSinceStartup - editorPreviousTime) * Time.timeScale;
			editorPreviousTime = Time.realtimeSinceStartup;
			cutscene.currentTime += editorPlayback == EditorPlayback.PlayingForwards? delta : -delta;
		}


		//initialize the action boxes
		void InitBoxes(){

			if (cutscene == null)
				return;

			multiSelection = null;
			var lastTime = cutscene.currentTime;

			if (!Application.isPlaying){
				Stop(true);
			}

			cutscene.Validate();
			clipWrappers = new Dictionary<int, ActionClipWrapper>();
			for (int g= 0; g < cutscene.groups.Count; g++){
				for (int t= 0; t < cutscene.groups[g].tracks.Count; t++){
					for (int a= 0; a < cutscene.groups[g].tracks[t].actions.Count; a++){
						var id = UID(g, t, a);
						if (clipWrappers.ContainsKey(id)){
							Debug.LogError("Collided UIDs. This should really not happen!");
							continue;
						}
						clipWrappers[id] = new ActionClipWrapper( cutscene.groups[g].tracks[t].actions[a]	);
					}
				}
			}

			if (lastTime > 0){
				cutscene.currentTime = lastTime;
			}
		}

		//A UID out of list indeces. Supports max of 100 groups, 100 tracks/group and 1.000 clips/track before UIDs collide. I suppose these are enough.
		int UID(int g, int t, int a){
			var A = g.ToString("D3");
			var B = t.ToString("D3");
			var C = a.ToString("D4");
			return int.Parse(A+B+C);
		}


		//...
		void OnSceneGUI(SceneView sceneView){

			if (cutscene == null){
				return;
			}

			//Shortcuts for scene gui only
			if (Event.current.type == EventType.KeyDown){

				if (Event.current.keyCode == KeyCode.Space && !Event.current.shift){
					GUIUtility.keyboardControl = 0;
					if (editorPlayback != EditorPlayback.Stoped){ Stop(false); }
					else {Play();}
					Event.current.Use();
				}

				if (Event.current.keyCode == KeyCode.Comma){
					GUIUtility.keyboardControl = 0;
					StepBackward();
					Event.current.Use();
				}

				if (Event.current.keyCode == KeyCode.Period){
					GUIUtility.keyboardControl = 0;
					StepForward();
					Event.current.Use();
				}
			}


			///Forward OnSceneGUI
			for (var i = 0; i < cutscene.directables.Count; i++){
				var directable = cutscene.directables[i];
				directable.SceneGUI( CutsceneUtility.selectedObject == directable );
			}
			///

			///No need to show tools of cutscene object, plus handles are shown per clip when required
			Tools.hidden = (Selection.activeObject == cutscene || Selection.activeGameObject == cutscene.gameObject) && CutsceneUtility.selectedObject != null;
			
			///Cutscene Root info and gizmos
			Handles.color = Prefs.gizmosColor;
			Handles.Label(cutscene.transform.position + new Vector3(0,0.4f,0), "Cutscene Root");
			Handles.DrawLine(cutscene.transform.position + cutscene.transform.forward, cutscene.transform.position + cutscene.transform.forward * -1);
			Handles.DrawLine(cutscene.transform.position + cutscene.transform.right, cutscene.transform.position + cutscene.transform.right * -1);
			Handles.color = Color.white;

			Handles.BeginGUI();

			if (cutscene.currentTime > 0 && (cutscene.currentTime < cutscene.length || !Application.isPlaying) ){
				///view frame. Red = scrubbing, yellow = active in playmode
				var cam       = sceneView.camera;
				var lineWidth = 3f;
				var top       = new Rect(0, 0, cam.pixelWidth, lineWidth);
				var bottom    = new Rect(0, cam.pixelHeight - lineWidth - 10, cam.pixelWidth, lineWidth + 10 );
				var left      = new Rect(0, 0, lineWidth, cam.pixelHeight);
				var right     = new Rect(cam.pixelWidth-lineWidth, 0, lineWidth, cam.pixelHeight);
				var texture   = whiteTexture;
				GUI.color = cutscene.isActive? Color.green : Color.red;
				GUI.DrawTexture(top, texture);
				GUI.DrawTexture(bottom, texture);
				GUI.DrawTexture(left, texture);
				GUI.DrawTexture(right, texture);
				//

				//Info
				GUI.color = new Color(0,0,0,0.7f);
				// GUI.Label(bottom, string.Format(" {0} '{1}'", cutscene.isActive? "Active" : "Previewing", cutscene.name ), GUIStyle.none);
				if (cutscene.isActive){
					GUI.Label(bottom, string.Format(" Active '{0}'", cutscene.name), GUIStyle.none);
				} else {
					GUI.Label(bottom, string.Format(" Previewing '{0}'. Non animatable changes made to actor components will be reverted.", cutscene.name), GUIStyle.none);
				}
			}

			GUI.color = Color.white;
			Handles.EndGUI();
		}


		//...
		void OnGUI(){

			GUI.skin.label.richText         = true;
			GUI.skin.label.alignment        = TextAnchor.UpperLeft;
			EditorStyles.label.richText     = true;
			EditorStyles.textField.wordWrap = true;
			EditorStyles.foldout.richText   = true;
			var e         = Event.current;
			mousePosition = e.mousePosition;
			current = this;

			//this is basicaly a hack to avoid unwanted behaviour when exiting playmode
			if (repaintCooldown > 0){
				repaintCooldown --;
				Repaint();
				ShowNotification(new GUIContent("...PlayMode Changed..."));
				return;
			}
			//

			if (cutscene == null || helpButtonPressed){
				ShowWelcome();
				return;
			}

			GUI.enabled = !isPrefab;
			if (isPrefab){
				ShowNotification(new GUIContent("Editing Prefab Assets is not allowed for safety\nPlease add an instance in the scene"));
				if (e.rawType == EventType.MouseDown || e.rawType == EventType.MouseUp || e.rawType == EventType.KeyDown || e.rawType == EventType.KeyUp){
					e.Use();
				}
			}

			if (EditorApplication.isCompiling){
				Stop(true);
				ShowNotification(new GUIContent("Compiling\n...Please wait..."));
				return;
			}

			if (e.type == EventType.MouseDown){
				RemoveNotification();
			}

			//Resample flag?
			if (willResample){
				willResample = false;
				cutscene.ReSample();
			}

			//handle undo/redo keyboard commands
			if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed"){
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                multiSelection = null;
                cutscene.Validate();
                e.Use();
				return;
			}

			//Undo?
			if (e.rawType == EventType.MouseDown && e.button == 0){
				Undo.RegisterFullObjectHierarchyUndo(cutscene.groupsRoot.gameObject, "Cutscene Change");
				Undo.RecordObject(cutscene, "Cutscene Change");
				willDirty = true;
			}


			//make the rects
			topLeftRect       = new Rect(0, toolbarHeight, leftMargin, topMargin);
			topMiddleRect     = new Rect(leftMargin, toolbarHeight, Screen.width - leftMargin - rightMargin, topMargin);
			//topRightRect    = new Rect(Screen.width - rightMargin, toolbarHeight, rightMargin, topMargin);
			//rightRect       = new Rect(Screen.width - rightMargin, topMargin, rightMargin, totalHeight);
			leftRect          = new Rect(0, topMargin + toolbarHeight, leftMargin, Screen.height + scrollPos.y);
			centerRect        = new Rect(leftMargin, topMargin + toolbarHeight, Screen.width - leftMargin - rightMargin, Screen.height + scrollPos.y);


			//reorder action lists for better UI. This is strictly a UI thing.
			if (!anyClipDragging && e.type == EventType.Layout){
				foreach(var group in cutscene.groups){
					foreach(var track in group.tracks){
						track.actions = track.actions.OrderBy(a => a.startTime).ToList();
					}
				}
			}				


			//button 2 seems buggy
			if (e.button == 2 && e.type == EventType.MouseDown){ mouseButton2Down = true; }
			if (e.button == 2 && e.rawType == EventType.MouseUp){ mouseButton2Down = false; }


			//just a director icon watermark at bottom right
			var r = new Rect(0,0,128,128);
			r.center = new Vector2(Screen.width-80, Screen.height-80);
			GUI.color = new Color(1,1,1,0.1f);
			GUI.DrawTexture(r, Styles.slateIcon);
			GUI.color = Color.white;
			///

			//...
			DoKeyboardShortcuts();

			//call respective function for each rect
			ShowPlaybackControls(topLeftRect);
			ShowTimeInfo(topMiddleRect);

			//Other functions
			ShowToolbar();
			DoScrubControls();
			DoZoomAndPan();

			//Dirty and Resample flags?
			if (e.rawType == EventType.MouseUp && e.button == 0){
				willResample = true;
				willDirty = true;
			}

			//Timelines
			var scrollRect1 = new Rect(0, topMargin + toolbarHeight, Screen.width, Screen.height - topMargin - toolbarHeight - 5);
			var scrollRect2 = new Rect(0, topMargin + toolbarHeight, Screen.width, totalHeight + 150);
			scrollPos = GUI.BeginScrollView(scrollRect1, scrollPos, scrollRect2);
			ShowGroupsAndTracksList(leftRect);
			ShowTimeLines(centerRect);
			GUI.EndScrollView();
			////////////////////////////////////////

			///etc
			DrawGuides();
			AcceptDrops();

			///Final stuff...///
			//enforce isScalingEnd false since rawType does not work from within GUI.Window
			if (e.rawType == EventType.MouseUp){
				foreach(var cw in clipWrappers.Values){
					cw.OnMouseUp();
				}
			}

			//clean selection and hotcontrols
			if (e.button == 0 && e.type == EventType.MouseDown){
				if (centerRect.Contains(mousePosition)){
					CutsceneUtility.selectedObject = null;
					multiSelection = null;
				}
				GUIUtility.keyboardControl = 0;
				showDragDropInfo = false;
			}
		
			//just some info for the user to drag/drop gameobject in editor
			if (showDragDropInfo && cutscene.groups.Find(g => g.GetType() == typeof(ActorGroup)) == null){
				var label = "Drag & Drop GameObjects or Prefabs in this window to create Actor Groups";
				var size = new GUIStyle("label").CalcSize(new GUIContent(label));
				var notificationRect = new Rect(0, 0, size.x, size.y);
				notificationRect.center = new Vector2((Screen.width/2) + (leftMargin/2), (Screen.height/2) + topMargin);
				// GUI.Box(notificationRect, label, Slate.Styles.clipBoxStyle);
				GUI.Label(notificationRect, label);
			}

			//should we repaint?
			if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp || GUI.changed){
				willRepaint = true;
			}

			//if the flag is true by anywhere we repaint once
			if (willRepaint){
				willRepaint = false;
				Repaint();
			}

			if (willDirty){
				willDirty = false;
				EditorUtility.SetDirty(cutscene);
				foreach(var o in cutscene.GetComponentsInChildren(typeof(IDirectable)).Cast<Object>() ){
					EditorUtility.SetDirty(o);
				}
			}

			//uber hack to show modal popup windows
			if (OnDoPopup != null){
				var temp = OnDoPopup;
				OnDoPopup = null;
				QuickPopup.Show(temp);
			}

			if (isPrefab){ ///darken
				GUI.color = new Color(0,0,0,0.3f);
				GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), whiteTexture);
				GUI.color = Color.white;
			}

			Handles.color = Color.black; //cheap separator for scroll
			Handles.DrawLine(new Vector2(0, centerRect.y+1), new Vector2(centerRect.xMax, centerRect.y+1));
			Handles.color = Color.white;

			//cleanup
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			GUI.skin = null;
		}

		

		void DoKeyboardShortcuts(){
			
			var e = Event.current;
			if (e.type == EventType.KeyDown && GUIUtility.keyboardControl == 0){
				
				if (e.keyCode == KeyCode.S){
					var keyable = CutsceneUtility.selectedObject as IKeyable;
					if (keyable != null){
						var time = cutscene.currentTime - CutsceneUtility.selectedObject.startTime;
						time = Mathf.Clamp(time, 0, keyable.endTime - keyable.startTime);
						if (keyable.animationData != null && keyable.animationData.isValid){
							keyable.animationData.TryKeyIdentity( keyable.animatedParametersTarget, time );
							e.Use();
						}
					}
				}

				if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace){
					var clip = CutsceneUtility.selectedObject as ActionClip;
					if (clip != null){
						SafeDoAction( ()=>{ (clip.parent as CutsceneTrack).DeleteAction(clip); InitBoxes(); } );
						e.Use();
					}
				}

				if (e.keyCode == KeyCode.Space && !e.shift){
					if (editorPlayback != EditorPlayback.Stoped){ Stop(false); }
					else {Play();}
					e.Use();
				}

				if (e.keyCode == KeyCode.Comma){
					StepBackward();
					e.Use();
				}

				if (e.keyCode == KeyCode.Period){
					StepForward();
					e.Use();
				}

				if (CutsceneUtility.selectedObject is ActionClip){
					var action = (ActionClip)CutsceneUtility.selectedObject;
					var time = PosToTime(mousePosition.x);
					if (e.keyCode == KeyCode.LeftBracket && time < action.endTime){
						var temp = action.endTime;
						action.startTime = time;
						action.endTime += temp - action.endTime;
						e.Use();
					}
					
					if (e.keyCode == KeyCode.RightBracket && time > action.startTime){
						action.endTime = time;
						e.Use();
					}
				}
			}
		}
		
		void DrawGuides(){

			//draw a vertical line at 0 time
			DrawGuideLine(TimeToPos(0), Color.white);

			//draw a vertical line at length time
			DrawGuideLine(TimeToPos(length), Color.white);

			//draw a vertical line at current time
			if (cutscene.currentTime > 0){
				DrawGuideLine(TimeToPos(cutscene.currentTime), cutscene.isActive? Color.yellow : new Color(1,0.3f,0.3f));
			}

			//draw a vertical line at dragging clip start/end time
			if (CutsceneUtility.selectedObject != null && anyClipDragging){
				DrawGuideLine(TimeToPos(CutsceneUtility.selectedObject.startTime), new Color(1,1,1,0.05f));
				DrawGuideLine(TimeToPos(CutsceneUtility.selectedObject.endTime), new Color(1,1,1,0.05f));
			}

			//draw a vertical line at dragging section
			if (draggedSection != null){
				DrawGuideLine( TimeToPos(draggedSection.time), draggedSection.color );
			}

			if (cutscene.isActive){
				if (cutscene.playTimeStart > 0){
					DrawGuideLine(TimeToPos(cutscene.playTimeStart), Color.red);
				}
				if (cutscene.playTimeEnd < length){
					DrawGuideLine(TimeToPos(cutscene.playTimeEnd), Color.red);
				}
			}
		}


		void ShowWelcome(){
			
			if (cutscene == null){
				helpButtonPressed = false;
			}

			var label = string.Format("<size=30><b>{0}</b></size>", helpButtonPressed? "Important and Helpful Links" : "Welcome to SLATE!");
			var size = new GUIStyle("label").CalcSize(new GUIContent(label));
			var titleRect = new Rect(0,0,size.x,size.y);
			titleRect.center = new Vector2(Screen.width/2, (Screen.height/2) - size.y );
			GUI.Label(titleRect, label);

			var iconRect = new Rect(0, 0, 128, 128);
			iconRect.center = new Vector2(Screen.width/2, titleRect.yMin - 60);
			GUI.DrawTexture(iconRect, Styles.slateIcon);

			var buttonRect = new Rect(0,0,size.x,size.y);
			var next = 0;

			if (!helpButtonPressed){
				GUI.backgroundColor = new Color(0.8f, 0.8f, 1, 1f);
				buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
				next++;
				if (GUI.Button(buttonRect, "Create New Cutscene")){
					InitializeAll( Commands.CreateCutscene() );
				}
				GUI.backgroundColor = Color.white;
			}

			buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
			next++;
			if (GUI.Button(buttonRect, "Visit The Website")){
				Help.BrowseURL("http://slate.paradoxnotion.com");
			}

			buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
			next++;
			if (GUI.Button(buttonRect, "Read The Documentation")){
				Help.BrowseURL("http://slate.paradoxnotion.com/documentation");
			}

			buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
			next++;
			if (GUI.Button(buttonRect, "Download Extensions")){
				Help.BrowseURL("http://slate.paradoxnotion.com/downloads");
			}

			buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
			next++;
			if (GUI.Button(buttonRect, "Join The Forums")){
				Help.BrowseURL("http://slate.paradoxnotion.com/forums-page");
			}

			if (!helpButtonPressed){
				buttonRect.center = new Vector2(Screen.width/2, (Screen.height/2) + (size.y + 2) * next );
				next++;
				if (GUI.Button(buttonRect, "Leave a Review")){
					Help.BrowseURL("http://u3d.as/ozt");
				}
			}


			if (helpButtonPressed && cutscene != null){
				var backRect = new Rect(0,0,400, 20);
				backRect.center = new Vector2(Screen.width/2, 20);
				GUI.backgroundColor = new Color(0.8f, 0.8f, 1, 1f);
				if (GUI.Button(backRect, "Close Help Panel")){
					helpButtonPressed = false;
				}
				GUI.backgroundColor = Color.white;
			}
		}



		void AcceptDrops(){

			if (cutscene.currentTime > 0){
				return;
			}

			var e = Event.current;
			if (e.type == EventType.DragUpdated && DragAndDrop.objectReferences.Length == 1){
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}

			if (e.type == EventType.DragPerform){

				if (DragAndDrop.objectReferences.Length != 1)
					return;

				var o = DragAndDrop.objectReferences[0];
				if (o is GameObject){
					var go = (GameObject)o;
					
					if ( go.GetComponent<DirectorCamera>() != null ){
						ShowNotification(new GUIContent("The 'DIRECTOR' group is already used for the 'DirectorCamera' object"));
						return;
					}

					if ( cutscene.GetAffectedActors().Contains(go) ){
						ShowNotification(new GUIContent(string.Format("GameObject '{0}' is already in the cutscene", o.name)));
						return;
					}

					DragAndDrop.AcceptDrag();
					var newGroup = cutscene.AddGroup<ActorGroup>(go);
					newGroup.AddTrack<ActorActionTrack>("Action Track");
					CutsceneUtility.selectedObject = newGroup;
				}				
			}
		}

		//The toolbar...
		void ShowToolbar(){

			GUI.enabled = cutscene.currentTime <= 0 && !isPrefab;

			var e = Event.current;
		
			GUI.backgroundColor = isProSkin? new Color(1f,1f,1f,0.5f) : Color.white;
			GUI.color = Color.white;
			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			if (GUILayout.Button(string.Format("[{0}]", cutscene.name), EditorStyles.toolbarDropDown, GUILayout.Width(100))){
				GenericMenu.MenuFunction2 SelectSequencer = (object cut) => {
					Selection.activeObject = (Cutscene)cut;
					EditorGUIUtility.PingObject((Cutscene)cut);
				};

				var cutscenes = FindObjectsOfType<Cutscene>();
				var menu = new GenericMenu();
				foreach (Cutscene cut in cutscenes){
					menu.AddItem(new GUIContent(string.Format("[{0}]", cut.name) ), cut == cutscene, SelectSequencer, cut);
				}
				menu.ShowAsContext();
				e.Use();
			}


			if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(60))){
				Selection.activeObject = cutscene;
				EditorGUIUtility.PingObject(cutscene);
			}

#if !NO_UTJ
			if (GUILayout.Button("Render", EditorStyles.toolbarButton, GUILayout.Width(60))){
				RenderWindow.Open();
			}
#endif

			if (GUILayout.Button("Snap: " + Prefs.snapInterval.ToString(), EditorStyles.toolbarDropDown, GUILayout.Width(80))){
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("0.001"), false, ()=>{ Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 1000; });
				menu.AddItem(new GUIContent("0.01"), false, ()=>{ Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 100; });
				menu.AddItem(new GUIContent("0.1"), false, ()=>{ Prefs.timeStepMode = Prefs.TimeStepMode.Seconds; Prefs.frameRate = 10; });
				menu.AddItem(new GUIContent("30 FPS"), false, ()=>{ Prefs.timeStepMode = Prefs.TimeStepMode.Frames; Prefs.frameRate = 30; });
				menu.AddItem(new GUIContent("60 FPS"), false, ()=>{ Prefs.timeStepMode = Prefs.TimeStepMode.Frames; Prefs.frameRate = 60; });
				menu.ShowAsContext();
				e.Use();
			}

			GUILayout.Space(10);

			GUILayout.FlexibleSpace();

			GUI.color = new Color(1,1,1,0.3f);
			GUILayout.Label(string.Format("<size=9>Version {0}</size>", Cutscene.VERSION_NUMBER.ToString("0.00")));
			GUI.color = Color.white;

			if (GUILayout.Button(Slate.Styles.gearIcon, EditorStyles.toolbarButton, GUILayout.Width(26))){
				PreferencesWindow.Show(new Rect(Screen.width - 5 - 400, toolbarHeight + 5, 400, Screen.height - toolbarHeight - 50));
			}

			helpButtonPressed = GUILayout.Toggle(helpButtonPressed, "Help", EditorStyles.toolbarButton);

			GUI.backgroundColor = new Color(1, 0.8f, 0.8f, 1);
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50))){
				if (EditorUtility.DisplayDialog("Clear All", "You are about to delete everything in this cutscene and start anew!\nAre you sure?", "YES", "NO!")){
					Stop(true);
					cutscene.ClearAll();
					InitializeAll();
				}
			}

			GUILayout.EndHorizontal();
			GUI.backgroundColor = Color.white;

			GUI.enabled = !isPrefab;
		}


		//Scrubing....
		void DoScrubControls(){

			if (cutscene.isActive){ //no scrubbing if playing in runtime
				return;
			}

			///////
			var e = Event.current;
			if (e.type == EventType.MouseDown && topMiddleRect.Contains(mousePosition) ){
				var carretPos = TimeToPos(length) + leftRect.width;
				var isEndCarret = Mathf.Abs(mousePosition.x - carretPos) < 10 || e.control;
				
				if (e.button == 0){
					movingEndCarret = isEndCarret;
					movingScrubCarret = !movingEndCarret;
					Pause();
				}

				if (e.button == 1 && isEndCarret){
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Set To Last Clip Time"), false, ()=>
						{
							var lastClip = cutscene.directables.Where(d => d is ActionClip).OrderBy(d => d.endTime).LastOrDefault();
							length = lastClip.endTime;
						});
					menu.ShowAsContext();
				}

				e.Use();
			}

			if (e.button == 0 && e.rawType == EventType.MouseUp){
				movingScrubCarret = false;
				movingEndCarret = false;
			}

			var pointerTime = PosToTime(mousePosition.x);
			if (movingScrubCarret){
				cutscene.currentTime = SnapTime(pointerTime);
				cutscene.currentTime = Mathf.Clamp(cutscene.currentTime, Mathf.Max(viewTimeMin, 0) + float.Epsilon, viewTimeMax - float.Epsilon);
			}

			if (movingEndCarret){
				length = SnapTime(pointerTime);
				length = Mathf.Clamp(length, viewTimeMin + float.Epsilon, viewTimeMax - float.Epsilon);
			}
		}

		void DoZoomAndPan(){
			
			if (!centerRect.Contains(mousePosition)){
				return;
			}

			var e = Event.current;
			//Zoom or scroll down/up if prefs is set to scrollwheel
			if ( (e.type == EventType.ScrollWheel && Prefs.scrollWheelZooms) || (e.alt && e.button == 1) ){
				this.AddCursorRect(centerRect, MouseCursor.Zoom);
				if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel){
					var pointerTimeA = PosToTime( mousePosition.x );
					var delta = e.alt? -e.delta.x * 0.1f : e.delta.y;
					var t = (Mathf.Abs(delta * 25) / centerRect.width ) * viewTime;
					viewTimeMin += delta > 0? -t : t;
					viewTimeMax += delta > 0? t : -t;
					var pointerTimeB = PosToTime( mousePosition.x + e.delta.x );
					var diff = pointerTimeA - pointerTimeB;
					viewTimeMin += diff;
					viewTimeMax += diff;
					e.Use();
				}
			}

			//pan left/right, up/down
			if (mouseButton2Down || (e.alt && e.button == 0) ){
				this.AddCursorRect(centerRect, MouseCursor.Pan);
				if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp){
					var t = ( Mathf.Abs(e.delta.x) / centerRect.width ) * viewTime;
					viewTimeMin += e.delta.x > 0? -t : t;
					viewTimeMax += e.delta.x > 0? -t : t;
					scrollPos.y -= e.delta.y;
					e.Use();
				}
			}			
		}


		//top left - various options
		void ShowPlaybackControls(Rect topLeftRect){

			if (!cutscene.isActive && cutscene.currentTime > 0 && editorPlayback == EditorPlayback.Stoped){
				GUI.color = new Color(1, 0.4f, 0.4f);
				GUI.Label(new Rect(topLeftRect.x + 5, topLeftRect.y, 100, topLeftRect.height), "<size=14><b>REC</b></size>", Slate.Styles.leftLabel);
				GUI.color = Color.white;
			}

			//Cutscene shows the gui
			GUILayout.BeginArea(topLeftRect);

			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUI.backgroundColor = isProSkin? Color.white : Color.grey;

			Rect lastRect;
			if (GUILayout.Button(Styles.stepReverseIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20))){
				StepBackward();
				Event.current.Use();
			}
			lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect.Contains(Event.current.mousePosition)){AddCursorRect(lastRect, MouseCursor.Link);}

			var isStoped = Application.isPlaying? (cutscene.isPaused || !cutscene.isActive) : editorPlayback == EditorPlayback.Stoped;
			if (isStoped && GUILayout.Button(Styles.playReverseIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20) )){
				PlayReverse();
				Event.current.Use();
			}
			lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect.Contains(Event.current.mousePosition)){AddCursorRect(lastRect, MouseCursor.Link);}

			if (GUILayout.Button(isStoped? Styles.playIcon : Styles.pauseIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20))){
				if (isStoped) Play();
				else Pause();
				Event.current.Use();
			}
			lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect.Contains(Event.current.mousePosition)){AddCursorRect(lastRect, MouseCursor.Link);}

			if (GUILayout.Button(Styles.stopIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20))){
				Stop(false);
				Event.current.Use();
			}
			lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect.Contains(Event.current.mousePosition)){AddCursorRect(lastRect, MouseCursor.Link);}

			if (GUILayout.Button(Styles.stepIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20))){
				StepForward();
				Event.current.Use();
			}			
			lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect.Contains(Event.current.mousePosition)){AddCursorRect(lastRect, MouseCursor.Link);}
/*
			if (GUILayout.Button(Styles.loopIcon, (GUIStyle)"box", GUILayout.Width(20), GUILayout.Height(20))){
				Event.current.Use();
			}
*/
			GUI.backgroundColor = Color.white;

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();

			GUILayout.EndArea();
		}

		//top mid - viewTime selection and time info
		void ShowTimeInfo(Rect topMiddleRect){

			GUI.color = new Color(1,1,1,0.2f);
			GUI.Box(topMiddleRect, "", EditorStyles.toolbarButton);
			GUI.color = new Color(0,0,0,0.2f);
			GUI.Box(topMiddleRect, "", Styles.timeBoxStyle);
			GUI.color = Color.white;

			var timeInterval = 1000000f;
			var highMod = timeInterval;
			var lowMod = 0.01f;
			var modulos = new float[]{ 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000 }; //... O.o really?
			for (var i = 0; i < modulos.Length; i++){
				var count = viewTime / modulos[i];
				if ( centerRect.width / count > 50){ //50 is approx width of label
					timeInterval = modulos[i];
					lowMod = i > 0? modulos[ i - 1 ] : lowMod;
					highMod = i < modulos.Length - 1? modulos[i + 1] : highMod;
					break;
				}
			}

			var doFrames = Prefs.timeStepMode == Prefs.TimeStepMode.Frames;
			var timeStep = doFrames? (1f/Prefs.frameRate) : lowMod;

			var start = (float)Mathf.FloorToInt(viewTimeMin / timeInterval) * timeInterval;
			var end = (float)Mathf.CeilToInt(viewTimeMax / timeInterval) * timeInterval;
			start = Mathf.Round(start * 10) / 10;
			end = Mathf.Round(end * 10) / 10;

			//draw vertical guide lines. Do this outside the BeginArea bellow.
			for (var _i = start; _i <= end; _i += timeInterval){
				var i = Mathf.Round(_i * 10) / 10;
				var linePos = TimeToPos(i);
				DrawGuideLine(linePos, new Color(0, 0, 0, 0.4f));
				if (i % highMod == 0){
					DrawGuideLine(linePos, new Color(0,0,0,0.5f));
				}
			}


			GUILayout.BeginArea(topMiddleRect);

			//the minMax slider
			var _timeMin = viewTimeMin;
			var _timeMax = viewTimeMax;
			var sliderRect = new Rect(5, 0, topMiddleRect.width - 10, 18);
			EditorGUI.MinMaxSlider(sliderRect, ref _timeMin, ref _timeMax, 0, maxTime);
			viewTimeMin = _timeMin;
			viewTimeMax = _timeMax;
			if (sliderRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2){
				viewTimeMin = 0;
				viewTimeMax = length;
			}

			//the step interval
			if (centerRect.width / (viewTime/timeStep) > 6){
				for (var i = start; i <= end; i += timeStep){
					var posX = TimeToPos(i);
					var frameRect = Rect.MinMaxRect(posX-1, topMargin-1, posX + 1, topMargin );
					GUI.color = isProSkin? Color.white : Color.black;
					GUI.DrawTexture(frameRect, whiteTexture);
					GUI.color = Color.white;
				}
			}

			//the time interval
			for (var i = start; i <= end; i += timeInterval){

				var posX = TimeToPos(i);
				var rounded = Mathf.Round(i * 10) / 10;

				GUI.color = isProSkin? Color.white : Color.black;
				var markRect = Rect.MinMaxRect(posX - 2, topMargin-2, posX + 2, topMargin);
				GUI.DrawTexture(markRect, whiteTexture);
				GUI.color = Color.white;

				var text = doFrames? (rounded * Prefs.frameRate).ToString("0") : rounded.ToString("0.00");
				var size = new GUIStyle("label").CalcSize(new GUIContent(text));
				var stampRect = new Rect(0, 0, size.x, size.y);
				stampRect.center = new Vector2(posX, topMargin - size.y + 4);
				GUI.color = rounded % highMod == 0? Color.white : new Color(1,1,1,0.5f);
				GUI.Box(stampRect, text, "label");
				GUI.color = Color.white;
			}

			//the number showing current time when scubing
			if (cutscene.currentTime > 0){
				var label = doFrames? (cutscene.currentTime * Prefs.frameRate).ToString("0") : cutscene.currentTime.ToString("0.00");
				var text = "<b><size=17>" + label + "</size></b>";
				var size = Styles.headerBoxStyle.CalcSize(new GUIContent(text));
				var posX = TimeToPos(cutscene.currentTime);
				var stampRect = new Rect(0, 0, size.x, size.y);
				stampRect.center = new Vector2(posX, topMargin - size.y/2);
				
				GUI.backgroundColor = isProSkin? new Color(0,0,0,0.4f) : new Color(0,0,0,0.7f);
				GUI.color = cutscene.isActive? Color.yellow : new Color(1,0.2f,0.2f);
				GUI.Box(stampRect, text, Styles.headerBoxStyle);
			}


			//the length position carret texture and pre-exit length indication
			var lengthPos = TimeToPos(length);
			var lengthRect = new Rect(0, 0, 16, 16);
			lengthRect.center = new Vector2(lengthPos, topMargin - 2);
			GUI.color = isProSkin? Color.white : Color.black;
			GUI.DrawTexture(lengthRect, Styles.carretIcon);

			GUILayout.EndArea();
		}




		//left - the groups and tracks info and option per group/track
		void ShowGroupsAndTracksList(Rect leftRect){

			var e = Event.current;
			GUI.enabled = cutscene.currentTime <= 0 && !isPrefab;

			//begin area for left Rect
			GUI.BeginGroup(leftRect);

			//start height at 3.
			var nextYPos = 3f;
			var selectedColor = new Color(0.5f, 0.5f, 1, 0.3f);

			//GROUPS
			for (int g= 0; g < cutscene.groups.Count; g++){
				var group  = cutscene.groups[g];
				var groupRect = new Rect(4, nextYPos, leftRect.width - groupRightMargin - 4, groupHeight - 3);
				nextYPos += groupHeight;

				///highligh?
				var groupSelected = (group == CutsceneUtility.selectedObject || group == pickedGroup);
				GUI.color = groupSelected? selectedColor : new Color(0f, 0f, 0f, 0.2f);
				GUI.Box(groupRect, "", Styles.headerBoxStyle);
				GUI.color = Color.white;

				///CONTEXT
				GUI.color = new Color(1,1,1,0.5f);
				var plusRect = new Rect(groupRect.xMax - 12, groupRect.y + 5, 8, 8);
				if ( (e.type == EventType.ContextClick && groupRect.Contains(e.mousePosition)) || GUI.Button(plusRect, Slate.Styles.plusIcon, GUIStyle.none) ){
					var menu = new GenericMenu();
					foreach (var _info in EditorTools.GetTypeMetaDerivedFrom(typeof(CutsceneTrack))){
						var info = _info;
						if (info.attachableTypes == null || !info.attachableTypes.Contains(group.GetType())){
							continue;
						}

						var isUnique = info.type.GetCustomAttributes(typeof(UniqueElementAttribute), true).FirstOrDefault() != null;
						var exists = group.tracks.Find(track => track.GetType() == info.type) != null;
						if (!isUnique || !exists){
							menu.AddItem(new GUIContent("Add Track/" + info.name), false, ()=> { group.AddTrack(info.type); });
						} else {
							menu.AddDisabledItem(new GUIContent("Add Track/" + info.name));
						}						

					}

					if ( !(group is DirectorGroup) ){
						menu.AddItem(new GUIContent("Replace Actor"), false, ()=>{ group.actor = null; });
						menu.AddItem(new GUIContent("Select Actor (Double Click)"), false, ()=>{ Selection.activeObject = group.actor; });
						menu.AddItem(new GUIContent("Disable Group"), !group.isActive, ()=>{ group.isActive = !group.isActive; });
						menu.AddSeparator("/");
						menu.AddItem(new GUIContent("Delete Group"), false, ()=>
							{
								if (EditorUtility.DisplayDialog("Delete Group", "Are you sure?", "YES", "NO!")){
									cutscene.DeleteGroup(group);
									InitBoxes();
								}
							});
					}
					menu.ShowAsContext();
					e.Use();
				}
				GUI.color = Color.white;
				
				//GRAPHICS
				GUI.color = isProSkin? Color.yellow : Color.white;
				var foldRect = new Rect(groupRect.x + 2, groupRect.y, 20, groupRect.height);
				var isVirtual = group.referenceMode == CutsceneGroup.ActorReferenceMode.UseInstanceHideOriginal;
				group.isCollapsed = !EditorGUI.Foldout(foldRect, !group.isCollapsed, string.Format("<b>{0} {1}</b>", group.name, isVirtual? "(Ref)" : "" ));
				GUI.color = Color.white;
				if (group.actor == null){
					var oRect = Rect.MinMaxRect(groupRect.xMin + 20, groupRect.yMin, groupRect.xMax - 20, groupRect.yMax);
					group.actor = (GameObject)UnityEditor.EditorGUI.ObjectField(oRect, group.actor, typeof(GameObject), true);
				}
				//////

				///REORDERING
				if (e.type == EventType.MouseDown && e.button == 0 && groupRect.Contains(e.mousePosition)){
					CutsceneUtility.selectedObject = CutsceneUtility.selectedObject != group? group : null;
					if ( !(group is DirectorGroup) ){
						pickedGroup = group;
					}
					if (e.clickCount == 2){
						Selection.activeGameObject = group.actor;
					}
					e.Use();
				}

				if (pickedGroup != null && pickedGroup != group && !(group is DirectorGroup) ){
					if (groupRect.Contains(e.mousePosition)){
						var markRect = new Rect(groupRect.x, (cutscene.groups.IndexOf(pickedGroup) < g)? groupRect.yMax - 2 : groupRect.y, groupRect.width, 2);
						GUI.Box(markRect, "");
					}

					if (e.rawType == EventType.MouseUp && e.button == 0 && groupRect.Contains(e.mousePosition)){
						cutscene.groups.Remove(pickedGroup);
						cutscene.groups.Insert(g, pickedGroup);
						pickedGroup = null;
						e.Use();
					}
				}

				if (groupRect.Contains(e.mousePosition)){
					this.AddCursorRect(groupRect, pickedGroup == null? MouseCursor.Link : MouseCursor.MoveArrow);
				}

				///...
				if (!group.isCollapsed){

					//TRACKS
					for (int t= 0; t < group.tracks.Count; t++){
						var track     = group.tracks[t];
						var yPos      = nextYPos;
						var trackRect = new Rect(5, yPos, leftRect.width - trackRightMargin - 5, track.height);
						nextYPos += track.height + trackMargins;

						//GRAPHICS
						GUI.color = new Color(1,1,1,0.2f);
						GUI.Box(trackRect, "", (GUIStyle)"flow node 0");
						GUI.color = track.isActive? Color.white : Color.grey;
						GUI.Box(trackRect, "");
						if (track == CutsceneUtility.selectedObject || track == pickedTrack){
							GUI.color = selectedColor;
							GUI.DrawTexture(trackRect, whiteTexture);
						}
						GUI.color = Color.white;
						//


						/////
						GUILayout.BeginArea(trackRect);
						track.OnTrackInfoGUI();
						GUILayout.EndArea();
						/////

					
						//CONTEXT
						if (e.type == EventType.ContextClick && trackRect.Contains(e.mousePosition)){
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Disable Track"), !track.isActive, ()=> { track.isActive = !track.isActive; });
							menu.AddSeparator("/");
							menu.AddItem(new GUIContent("Delete Track"), false, ()=>
								{
									if (EditorUtility.DisplayDialog("Delete Track", "Are you sure?", "YES", "NO!")){
										group.DeleteTrack(track);
										InitBoxes();
									}
								});
							menu.ShowAsContext();
							e.Use();
						}

						//REORDERING
						if (e.type == EventType.MouseDown && e.button == 0 && trackRect.Contains(e.mousePosition)){
							CutsceneUtility.selectedObject = CutsceneUtility.selectedObject != track? track : null;
							pickedTrack = track;
							e.Use();
						}

						if (pickedTrack != null && pickedTrack != track && pickedTrack.parent == group){
							if (trackRect.Contains(e.mousePosition)){
								var markRect = new Rect(trackRect.x, (group.tracks.IndexOf(pickedTrack) < t)? trackRect.yMax - 2 : trackRect.y, trackRect.width, 2);
								GUI.Box(markRect, "");
							}

							if (e.rawType == EventType.MouseUp && e.button == 0 && trackRect.Contains(e.mousePosition)){
								group.tracks.Remove(pickedTrack);
								group.tracks.Insert(t, pickedTrack);
								pickedTrack = null;
								e.Use();
							}
						}

						if (trackRect.Contains(e.mousePosition)){
							this.AddCursorRect(trackRect, pickedTrack == null? MouseCursor.Link : MouseCursor.MoveArrow);
						}
					}
				}

				totalHeight = nextYPos;
			}

			GUI.EndGroup();


			//Simple button to add empty group for convenience
			var addButtonY = totalHeight + topMargin + toolbarHeight + 20;
			var addRect = Rect.MinMaxRect(leftRect.xMin + 10, addButtonY, leftRect.xMax - 10, addButtonY + 20);
			GUI.color = new Color(1,1,1,0.5f);
			if (GUI.Button(addRect, "Add Actor Group")){
				var newGroup = cutscene.AddGroup<ActorGroup>(null).AddTrack<ActorActionTrack>();
				CutsceneUtility.selectedObject = newGroup;
			}


			if (e.rawType == EventType.MouseUp){
				pickedGroup = null;
				pickedTrack = null;
			}

			GUI.enabled = !isPrefab;
			GUI.color = Color.white;
		}




		//middle - the actual timeline tracks
		void ShowTimeLines(Rect centerRect){

			var e = Event.current;

			//bg graphic
			var bgRect = new Rect(centerRect.x, centerRect.y + scrollPos.y, centerRect.width, centerRect.height - toolbarHeight);
			GUI.Box(bgRect, "", (GUIStyle)"TextField");
			GUI.color = Color.white;

			//Group and windows start
			GUILayout.BeginArea(centerRect);
			BeginWindows();

			//start at 3
			var nextYPos = 3f;

			//GROUPS
			for (int g = 0; g < cutscene.groups.Count; g++){
				var group     = cutscene.groups[g];
				var groupRect = Rect.MinMaxRect( Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(0)), nextYPos, TimeToPos(viewTimeMax), nextYPos + groupHeight);
				nextYPos += groupHeight;

				//GROUP SECTIONS. Only Director Group for now
				if (group is DirectorGroup){
					GenericMenu sectionsMenu = null;
					if (e.type == EventType.ContextClick && groupRect.Contains(e.mousePosition)){
						var t = PosToTime(mousePosition.x);
						sectionsMenu = new GenericMenu();
						sectionsMenu.AddItem( new GUIContent("Add Section Here"), false, ()=>{ group.sections.Add(new Section("Section", t)); } );
					}

					var sections = group.sections.OrderBy(s => s.time).ToList();
					if (sections.Count == 0){
						sections.Insert(0, new Section("No Sections", 0));
						sections.Add(new Section("Outro", maxTime));
					} else {
						sections.Insert(0, new Section("Intro", 0));
						sections.Add(new Section("Outro", maxTime));
					}

					for (var i = 0; i < sections.Count-1; i++){
						var section1 = sections[i];
						var section2 = sections[i + 1];
						var pos1 = TimeToPos(section1.time);
						var pos2 = TimeToPos(section2.time);
						var y = groupRect.y;
						
						var sectionRect = Rect.MinMaxRect(pos1, y, pos2 - 2, y + groupHeight - 5);
						var markRect    = new Rect(sectionRect.x + 2, sectionRect.y + 2, 2, sectionRect.height-4);
						var clickRect   = new Rect(0, y, 15, sectionRect.height);
						clickRect.center = markRect.center;

						GUI.color = section1.color;
						GUI.DrawTexture(sectionRect, whiteTexture);
						GUI.color = new Color(1,1,1,0.2f);
						GUI.DrawTexture(markRect, whiteTexture);
						GUI.color = Color.white;
						GUI.Label(sectionRect, string.Format(" <i>{0}</i>", section1.name) );

						if (sectionRect.Contains(e.mousePosition)){
							if (e.type == EventType.MouseDown && e.button == 0 ){
								if (e.clickCount == 2){
									viewTimeMin = section1.time;
									viewTimeMax = section2.time;
									e.Use();
								}
							}
							if (i != 0 && e.type == EventType.ContextClick && sectionsMenu != null){
								sectionsMenu.AddItem(new GUIContent("Edit"), false, ()=>
								{
									DoPopup(()=>
										{
											section1.name = EditorGUILayout.TextField(section1.name);
											section1.color = EditorGUILayout.ColorField(section1.color);
										});
								});
								sectionsMenu.AddItem(new GUIContent("Focus (Double Click)"), false, ()=>{ viewTimeMin = section1.time; viewTimeMax = section2.time; } );
								sectionsMenu.AddSeparator("/");
								sectionsMenu.AddItem(new GUIContent("Delete Section"), false, ()=>{ group.sections.Remove(section1); } );
							}
						}

						if (i != 0 && clickRect.Contains(e.mousePosition)){
							this.AddCursorRect(clickRect, MouseCursor.SlideArrow);
							if (e.type == EventType.MouseDown && e.button == 0){
								draggedSection = section1;
								draggedSectionShiftingMin = cutscene.GetKeyTimes().Last(t => t <= draggedSection.time);
								e.Use();
							}
						}
					}

					if (draggedSection != null){
						var lastTime = draggedSection.time;
						var newTime = PosToTime(mousePosition.x);
						var previousSectionTime = sections.Last(s => s.time < lastTime).time;
						var nextSectionTime = sections.First(s => s.time > lastTime).time;
						newTime = SnapTime(newTime);
						newTime = Mathf.Clamp(newTime, previousSectionTime + 1f, nextSectionTime - 1f); //dont think a section should be as small as 1sec anyways.
						newTime = Mathf.Clamp(newTime, e.shift? draggedSectionShiftingMin : newTime, maxTime);
						draggedSection.time = newTime;

						//shift clips and sections after drag section.
						if (e.shift){
							foreach(var cw in clipWrappers.Values.Where(c => c.action.startTime >= lastTime)){
								var max = cw.previousClip != null? cw.previousClip.endTime : 0;
								cw.action.startTime += newTime - lastTime;
								cw.action.startTime = Mathf.Max(cw.action.startTime, max);
							}
							foreach(var section in group.sections.Where(s => s != draggedSection && s.time > lastTime)){
								section.time += newTime - lastTime;
							}
						}

						//shift all clips with time > to this section if shift is down
						if (e.control && !e.shift){
							foreach(var section in group.sections.Where(s => s != draggedSection && s.time > lastTime)){
								section.time += newTime - lastTime;
							}
						}

						if (e.rawType == EventType.MouseUp){
							draggedSection = null;
							group.sections = group.sections.OrderBy(s => s.time).ToList();
						}
					}

					if (sectionsMenu != null){
						sectionsMenu.ShowAsContext();
						e.Use();
					}
				}

				//...
				if (group.isCollapsed){
					GUI.color = new Color(0,0,0,0.3f);
					var colRect = Rect.MinMaxRect(groupRect.xMin + 2, groupRect.yMin + 2, groupRect.xMax, groupRect.yMax -4);
					GUI.Box(colRect, "");
					GUI.color = Color.white;
					continue;
				}


				//TRACKS
				for (int t= 0; t < group.tracks.Count; t++){
					var track         = group.tracks[t];
					var yPos          = nextYPos;
					var trackPosRect  = Rect.MinMaxRect( Mathf.Max(TimeToPos(viewTimeMin), TimeToPos(track.startTime)), yPos, TimeToPos(viewTimeMax), yPos + track.height);
					var trackTimeRect = Rect.MinMaxRect( Mathf.Max(viewTimeMin, track.startTime), 0, viewTimeMax, 0);
					nextYPos         += track.height + trackMargins;

					//GRAPHICS
					GUI.backgroundColor = isProSkin? Color.black : new Color(0,0,0,0.1f);
					GUI.Box(trackPosRect, "");
					if (viewTimeMin < 0){ //just visual clarity
						GUI.Box(Rect.MinMaxRect(TimeToPos(viewTimeMin), trackPosRect.yMin, TimeToPos(0), trackPosRect.yMax), "");
					}
					if (track.startTime > track.parent.startTime || track.endTime < track.parent.endTime){
						Handles.color = Color.black;
						GUI.color = new Color(0,0,0,0.2f);
						if (track.startTime > track.parent.startTime){
							var tStart = TimeToPos(track.startTime);
							var r = Rect.MinMaxRect(TimeToPos(0), yPos, tStart, yPos + track.height);
							GUI.DrawTexture(r, whiteTexture);
							var a = new Vector2(tStart, trackPosRect.yMin);
							var b = new Vector2(a.x, trackPosRect.yMax);
							Handles.DrawLine(a, b);
						}
						if (track.endTime < track.parent.endTime){
							var tEnd = TimeToPos(track.endTime);
							var r = Rect.MinMaxRect(tEnd, yPos, TimeToPos(length), yPos + track.height);
							GUI.DrawTexture(r, whiteTexture);
							var a = new Vector2(tEnd, trackPosRect.yMin);
							var b = new Vector2(a.x, trackPosRect.yMax);
							Handles.DrawLine(a, b);	
						}
						GUI.color = Color.white;
						Handles.color = Color.white;
					}
					GUI.backgroundColor = Color.white;
					//////

					
					//...
					var cursorTime = SnapTime( PosToTime(mousePosition.x) );
					track.OnTrackTimelineGUI(trackPosRect, trackTimeRect, cursorTime, TimeToPos);
					//...


					//ACTION CLIPS
					for (int a= 0; a < track.actions.Count; a++){
						var action = track.actions[a];
						var ID = UID(g,t,a);
						ActionClipWrapper clipWrapper = null;

						if (!clipWrappers.TryGetValue(ID, out clipWrapper)){
							InitBoxes();
							clipWrapper = clipWrappers[ID];
						}

						if (clipWrapper.action != action){
							InitBoxes();
							clipWrapper = clipWrappers[ID];
						}

						//find and store net/previous clips to wrapper
						var nextClip = a < track.actions.Count -1? track.actions[a + 1] : null;
						var previousClip = a != 0? track.actions[a - 1] : null;
						clipWrapper.nextClip = nextClip;
						clipWrapper.previousClip = previousClip;
						

						//get the action box rect
						var clipRect = clipWrapper.rect;

						//modify it
						clipRect.y = yPos;
						clipRect.width = Mathf.Max(action.length / viewTime * centerRect.width, 6);
						clipRect.height = track.height;


					
						//get the action time and pos
						var xTime = action.startTime;
						var xPos = clipRect.x;

						if (anyClipDragging && CutsceneUtility.selectedObject == action){

							var lastTime = xTime; //for multiSelection drag
							xTime = PosToTime(xPos + leftRect.width);
							xTime = SnapTime(xTime);
							xTime = Mathf.Clamp(xTime, 0, maxTime - 0.1f);

							//handle multisection. Limit xmin, xmax by their bound rect
							if (multiSelection != null && multiSelection.Length > 1){
								var delta = xTime - lastTime;
								var boundMin = Mathf.Min( multiSelection.Select(b => b.action.startTime).ToArray() );
								var boundMax = Mathf.Max( multiSelection.Select(b => b.action.endTime).ToArray() );
								if (boundMin + delta < 0 || boundMax + delta > maxTime){
									xTime -= delta;
									delta = 0;
								}

								foreach(var cw in multiSelection){
									if (cw.action != action){
										cw.action.startTime += delta;
									}
								}
							}

							//clamp and cross blend between other nearby clips
							if ( multiSelection == null || multiSelection.Length < 1 ){
								var preCursorClip = track.actions.Where(act => act != action && act.startTime < cursorTime ).LastOrDefault();
								var postCursorClip = track.actions.Where(act => act != action && act.endTime > cursorTime ).FirstOrDefault();

								if (e.shift){ //when shifting track clips always clamp to previous clip and no need to clamp to next
									preCursorClip = previousClip;
									postCursorClip = null;
								}

								var preTime = preCursorClip != null? preCursorClip.endTime : 0 ;
								var postTime = postCursorClip != null? postCursorClip.startTime : maxTime + action.length;

								if (action is ICrossBlendable){
									if (preCursorClip is ICrossBlendable && preCursorClip.GetType() == action.GetType() ){
										preTime -= Mathf.Min( action.length/2, preCursorClip.length/2 );
									}

									if (postCursorClip is ICrossBlendable && postCursorClip.GetType() == action.GetType()){
										postTime += Mathf.Min( action.length/2, postCursorClip.length/2 );
									}
								}

								//does it fit?
								if (action.length > postTime - preTime){
									xTime = lastTime;
								}

								if (xTime != lastTime){
									xTime = Mathf.Clamp(xTime, preTime, postTime - action.length);
									//Shift all the next clips along with this one if shift is down
									if (e.shift){
										foreach(var cw in clipWrappers.Values.Where(c => c.action.parent == action.parent && c.action != action && c.action.startTime > lastTime)){
											cw.action.startTime += xTime - lastTime;
										}
									}
								}
							}


							//Apply xTime
							action.startTime = xTime;
						}

						//apply xPos
						clipRect.x = TimeToPos(xTime);


						//set crossblendable blend propertie
						if (!anyClipDragging){
							var overlap = previousClip != null? Mathf.Max(previousClip.endTime - action.startTime, 0) : 0;
							if (overlap > 0){
								action.blendIn = overlap;
								previousClip.blendOut = overlap;
							}							
						}


						//dont draw if outside of view range and not selected
						var isSelected = CutsceneUtility.selectedObject == action || (multiSelection != null && multiSelection.Select(b => b.action).Contains(action) );
						var isVisible = action.startTime <= viewTimeMax && action.endTime >= viewTimeMin;
						if ( !isSelected && !isVisible ){
							continue;
						}



						//draw selected rect
						if (isSelected){
							var selRect = Rect.MinMaxRect(clipRect.xMin-2, clipRect.yMin-2, clipRect.xMax+2, clipRect.yMax+2);
							GUI.color = isProSkin? new Color(0.65f, 0.65f, 1) : new Color(0.3f, 0.3f, 1);
							GUI.DrawTexture(selRect, Slate.Styles.whiteTexture);
							GUI.color = Color.white;
						}

						//determine color and draw clip
						var color = track.color;
						color = action.isValid? color : new Color(1, 0.3f, 0.3f);
						color = track.isActive? color : Color.grey;
						GUI.color = color;
						GUI.Box(clipRect, "", Styles.clipBoxHorizontalStyle);
						GUI.color = Color.white;

						clipWrapper.rect = GUI.Window(ID, clipRect, ActionClipWindow, string.Empty, GUIStyle.none); //this is not, but it's callback does
						if (!isProSkin){ GUI.color = new Color(1,1,1,0.5f);	GUI.Box(clipRect, ""); GUI.color = Color.white;	}

						var nextPosX = TimeToPos( nextClip != null? nextClip.startTime : viewTimeMax);
						var prevPosX = TimeToPos( previousClip != null? previousClip.endTime : viewTimeMin);
						var extRectLeft = Rect.MinMaxRect(prevPosX, clipRect.yMin, clipRect.xMin, clipRect.yMax);
						var extRectRight = Rect.MinMaxRect(clipRect.xMax, clipRect.yMin, nextPosX, clipRect.yMax);

						action.ShowClipGUIExternal(extRectLeft, extRectRight);

						//draw info text outside if clip is too small
						if (clipRect.width <= 20){
							GUI.Label(extRectRight, string.Format("<size=9>{0}</size>", action.info) );
						}
					}

					//darken a muted track's timeline after clips are drawn
					if (!track.isActive){
						GUI.color = new Color(0,0,0,0.2f);
						GUI.DrawTexture(trackPosRect, whiteTexture);
						GUI.color = Color.white;
					}
				}
			}

			EndWindows();

			//this is done in the same GUILayout.Area
			DoMultiSelection();

			GUILayout.EndArea();

			GUI.color = new Color(1,1,1,0.2f);
			GUI.Box(bgRect, "", Styles.shadowBorderStyle);
			GUI.color = Color.white;

			///darken the time after cutscene length
			if (viewTimeMax > length){
				var endPos = Mathf.Max( TimeToPos(length) + leftRect.width, centerRect.xMin );
				var darkRect = Rect.MinMaxRect(endPos, centerRect.yMin, centerRect.xMax, centerRect.yMax);
				GUI.color = new Color(0,0,0,0.3f);
				GUI.Box(darkRect, "", (GUIStyle)"TextField");
				GUI.color = Color.white;
			}

			///darken the time before zero
			if (viewTimeMin < 0){
				var startPos = Mathf.Min( TimeToPos(0) + leftRect.width, centerRect.xMax );
				var darkRect = Rect.MinMaxRect(centerRect.xMin, centerRect.yMin, startPos, centerRect.yMax);
				GUI.color = new Color(0,0,0,0.3f);
				GUI.Box(darkRect, "", (GUIStyle)"TextField");
/*				
				var labelStyle = new GUIStyle("label");
				labelStyle.alignment = TextAnchor.MiddleCenter;
				GUI.color = new Color(1,1,1,0.05f);
				GUI.Label(darkRect, "<size=30><b>DEAD ZONE</b></size>", labelStyle);
*/
				GUI.color = Color.white;
			}

			if (GUIUtility.hotControl == 0 || e.rawType == EventType.MouseUp){
				anyClipDragging = false;
			}
		}



		//This is done in a GUILayoutArea, thus must use e.mousePosition instead of this.mousePosition
		void DoMultiSelection(){
			
			var e = Event.current;
			if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0){
				multiSelection = null;
				multiSelectStartPos = e.mousePosition;
			}

			var r = new Rect();
			var bigEnough = false;
			if (multiSelectStartPos != null){
				var start = (Vector2)multiSelectStartPos;
				if ( (start - e.mousePosition).magnitude > 10 ){
					bigEnough = true;
					r.xMin = Mathf.Min(start.x, e.mousePosition.x);
					r.xMax = Mathf.Max(start.x, e.mousePosition.x);
					r.yMin = Mathf.Min(start.y, e.mousePosition.y);
					r.yMax = Mathf.Max(start.y, e.mousePosition.y);
					GUI.color = isProSkin? Color.white : new Color(1,1,1,0.3f);
					GUI.Box(r, "");
					foreach(var box in clipWrappers.Values.Where(b => AEncapsulatesB(r, b.rect))){
						GUI.color = new Color(0.5f,0.5f,1, 0.5f);
						GUI.Box(box.rect, "", Slate.Styles.clipBoxStyle);
						GUI.color = Color.white;
					}
				}
			}

			if (e.rawType == EventType.MouseUp){
				if (bigEnough){
					multiSelection = clipWrappers.Values.Where(b => AEncapsulatesB(r, b.rect) ).ToArray();
					if (multiSelection.Length == 1){
						CutsceneUtility.selectedObject = multiSelection[0].action;
						multiSelection = null;
					}
				}
				multiSelectStartPos = null;
			}

			if (multiSelection != null){
				var boundRect = GetBoundRect(multiSelection.Select(b => b.rect).ToArray(), 4f);
				GUI.color = isProSkin? Color.white : new Color(1,1,1,0.3f);
				GUI.Box(boundRect, "");
			}
			GUI.color = Color.white;
		}

		//this could be an extension but it's only used her so...
		bool AEncapsulatesB(Rect a, Rect b){
			return a.xMin <= b.xMin && a.xMax >= b.xMax && a.yMin <= b.yMin && a.yMax >= b.yMax;
		}

		///Gets the bound rect out of many rects
		Rect GetBoundRect(Rect[] rects, float offset = 0f){
			var minX = float.PositiveInfinity;
			var minY = float.PositiveInfinity;
			var maxX = float.NegativeInfinity;
			var maxY = float.NegativeInfinity;
			
			for (var i = 0; i < rects.Length; i++){
				minX = Mathf.Min(minX, rects[i].xMin);
				minY = Mathf.Min(minY, rects[i].yMin);
				maxX = Mathf.Max(maxX, rects[i].xMax);
				maxY = Mathf.Max(maxY, rects[i].yMax);
			}

			minX -= offset;
			minY -= offset;
			maxX += offset;
			maxY += offset;
			return Rect.MinMaxRect(minX, minY, maxX, maxY);
		}









		//holds info about placed actions in the timeline. Basicaly a wrapper
		class ActionClipWrapper{

			public ActionClip action;
			public bool isScalingStart;
			public bool isScalingEnd;
			public bool isControlBlendIn;
			public bool isControlBlendOut;
			public float preScaleStartTime;
			public float preScaleEndTime;

			public ActionClip nextClip;
			public ActionClip previousClip;

			private Rect _rect;
			public Rect rect {
				get {return action.isCollapsed? new Rect() : _rect;}
				set {_rect = value;}
			}

			public ActionClipWrapper(ActionClip action){
				this.action = action;
			}

			public void OnMouseUp(){
				isControlBlendIn = false;
				isControlBlendOut = false;
				isScalingStart = false;
				isScalingEnd = false;
			}
		}

		//ActionClip window callback. Its ID is based on the UID function that is based on the index path to the action
		//the ID of the window is also the same as the ID to use for for clipWrappers dictionary as key to get the clipWrapper for the action that represents this window
		void ActionClipWindow(int id){

			var e = Event.current;
			ActionClipWrapper wrapper = null;
			if (!clipWrappers.TryGetValue(id, out wrapper)){
				return;
			}

			var action = wrapper.action;
			var rect = wrapper.rect;
			var overlapIn = wrapper.previousClip != null? Mathf.Max(wrapper.previousClip.endTime - action.startTime, 0) : 0;
			var overlapOut = wrapper.nextClip != null? Mathf.Max(action.endTime - wrapper.nextClip.startTime, 0) : 0;
			var blendInPosX = (action.blendIn/action.length) * rect.width;
			var blendOutPosX = ((action.length - action.blendOut) /action.length) * rect.width;
			var hasActiveParameters = action.hasActiveParameters;


			//...
			if (e.type == EventType.KeyDown && e.shift){
				wrapper.preScaleStartTime = action.startTime;
				wrapper.preScaleEndTime = action.endTime;
			}
			var doRetime = (wrapper.isScalingEnd || wrapper.isScalingStart) && e.shift;
			action.ShowClipGUI(new Rect(0, 0, rect.width, rect.height), doRetime, wrapper.preScaleStartTime, wrapper.preScaleEndTime);
			//...

			//BLEND GRAPHICS
			if (action.blendIn > 0){
				Handles.color = new Color(0,0,0,0.5f);
				Handles.DrawAAPolyLine(2, new Vector3[]{new Vector2(0, rect.height), new Vector2(blendInPosX, 0)});
				Handles.color = new Color(0,0,0,0.3f);
				Handles.DrawAAConvexPolygon(new Vector3[]{ new Vector3(0, 0), new Vector3(0, rect.height), new Vector3(blendInPosX, 0) });
			}

			if (action.blendOut > 0 && overlapOut == 0){
				Handles.color = new Color(0,0,0,0.5f);
				Handles.DrawAAPolyLine(2, new Vector3[]{new Vector2(blendOutPosX, 0), new Vector2(rect.width, rect.height)});
				Handles.color = new Color(0,0,0,0.3f);
				Handles.DrawAAConvexPolygon(new Vector3[]{ new Vector3(rect.width, 0), new Vector2(blendOutPosX, 0), new Vector2(rect.width, rect.height) });
			}

			if (overlapIn > 0){
				Handles.color = Color.black;
				Handles.DrawAAPolyLine(2, new Vector3[]{ new Vector2(blendInPosX, 0), new Vector2(blendInPosX, rect.height) });
			}

			Handles.color = Color.white;


			//SCALING IN/OUT
			var lengthProp = action.GetType().GetProperty("length", BindingFlags.Instance | BindingFlags.Public );
			var isScalable = lengthProp != null && lengthProp.DeclaringType != typeof(ActionClip) && lengthProp.CanWrite && action.length > 0;
			var scaleRectWidth = 4;
			var allowScaleIn = isScalable && rect.width > scaleRectWidth * 2;
			var cursorRect = new Rect( (allowScaleIn? scaleRectWidth : 0), 0, (isScalable? rect.width - (allowScaleIn? scaleRectWidth*2 : scaleRectWidth ): rect.width), rect.height - (hasActiveParameters? 15 : 0) );
			this.AddCursorRect(cursorRect, MouseCursor.Link);
			var controlRectIn = new Rect(0, 0, scaleRectWidth, rect.height - (hasActiveParameters? 15 : 0) );
			var controlRectOut = new Rect(rect.width -scaleRectWidth, 0, scaleRectWidth, rect.height - (hasActiveParameters? 15 : 0) );
			if (isScalable){
				GUI.color = new Color(0,1,1,0.3f);
				if (overlapOut <= 0){
					//GUI.DrawTexture(controlRectOut, Slate.Styles.whiteTexture);
					this.AddCursorRect(controlRectOut, MouseCursor.ResizeHorizontal);
					if (e.type == EventType.MouseDown && e.button == 0 && !e.control){
						if (controlRectOut.Contains(e.mousePosition)){
							wrapper.isScalingEnd = true;
							wrapper.preScaleStartTime = action.startTime;
							wrapper.preScaleEndTime = action.endTime;
							e.Use();
						}
					}
				}

				if (overlapIn <= 0 && allowScaleIn){
					//GUI.DrawTexture(controlRectIn, Slate.Styles.whiteTexture);
					this.AddCursorRect(controlRectIn, MouseCursor.ResizeHorizontal);
					if (e.type == EventType.MouseDown && e.button == 0 && !e.control){
						if (controlRectIn.Contains(e.mousePosition)){
							wrapper.isScalingStart = true;
							wrapper.preScaleStartTime = action.startTime;
							wrapper.preScaleEndTime = action.endTime;
							e.Use();
						}
					}
				}
				GUI.color = Color.white;
			}

			//BLENDING IN/OUT
			var blendInProp = action.GetType().GetProperty("blendIn", BindingFlags.Instance | BindingFlags.Public /* | BindingFlags.DeclaredOnly */ );
			var isBlendable = blendInProp != null && blendInProp.DeclaringType != typeof(ActionClip) && blendInProp.CanWrite;
			if (isBlendable){
				if (e.type == EventType.MouseDown && e.button == 0 && e.control){
					if (controlRectIn.Contains(e.mousePosition)){
						wrapper.isControlBlendIn = true;
						e.Use();
					}
					if (controlRectOut.Contains(e.mousePosition)){
						wrapper.isControlBlendOut = true;
						e.Use();
					}
				}
			}

		
			if (wrapper.isControlBlendIn){
				action.blendIn = Mathf.Clamp(PosToTime(mousePosition.x) - action.startTime, 0, action.length - action.blendOut );
			}

			if (wrapper.isControlBlendOut){
				action.blendOut = Mathf.Clamp(action.endTime - PosToTime(mousePosition.x), 0, action.length - action.blendIn );
			}

			if (wrapper.isScalingStart){
				var prev = wrapper.previousClip != null? wrapper.previousClip.endTime : 0;
				if (action is ICrossBlendable && wrapper.previousClip is ICrossBlendable){
					prev -= Mathf.Min(action.length/2, wrapper.previousClip.length/2);
				}
				action.startTime = SnapTime(PosToTime(mousePosition.x));
				action.startTime = Mathf.Clamp( action.startTime, prev, wrapper.preScaleEndTime );
				action.endTime = wrapper.preScaleEndTime;
			}

			if (wrapper.isScalingEnd){
				var next = wrapper.nextClip != null? wrapper.nextClip.startTime : maxTime;
				if (action is ICrossBlendable && wrapper.nextClip is ICrossBlendable){
					next += Mathf.Min(action.length/2, wrapper.nextClip.length/2);
				}
				action.endTime = SnapTime(PosToTime(mousePosition.x));
				action.endTime = Mathf.Clamp( action.endTime, 0, next );
			}

			if (e.type == EventType.MouseDown){

				CutsceneUtility.selectedObject = action;

				if (e.button == 0){
					anyClipDragging = true;
				}

				if (multiSelection != null && !multiSelection.Select(cw => cw.action).Contains(action)){
					multiSelection = null;
				}
				
				if (e.button == 0 && e.clickCount == 2){
					//do this with reflection to get the declaring actor in case action has 'new' declaration. This is only done in Shot right now.
					Selection.activeObject = action.GetType().GetProperty("actor").GetValue(action, null) as Object;
				}
			}



			//FINALIZATION
			if (e.rawType == EventType.MouseUp){
				wrapper.OnMouseUp();			
			}


			//CONTEXT
			if (e.rawType == EventType.ContextClick){

				var menu = new GenericMenu();

				if (multiSelection != null && multiSelection.Contains(wrapper)){
					menu.AddItem(new GUIContent("Delete Clips"), false, ()=>{
						SafeDoAction( ()=>
							{
								foreach(var act in multiSelection.Select(b => b.action).ToArray()){
									(act.parent as CutsceneTrack).DeleteAction(act);
								}
								InitBoxes();
								multiSelection = null;
							} );
					});

				} else {

					menu.AddItem(new GUIContent("Copy Clip"), false, ()=> {CutsceneUtility.CopyClip(action);} );
					menu.AddItem(new GUIContent("Cut Clip"), false, ()=> {CutsceneUtility.CutClip(action);} );

					if (isScalable){

						menu.AddItem(new GUIContent("Stretch Clip"), false, ()=>
						{
							action.startTime = wrapper.previousClip != null? wrapper.previousClip.endTime : action.parent.startTime;
							action.endTime = wrapper.nextClip != null? wrapper.nextClip.startTime : action.parent.endTime;
						});

						menu.AddItem(new GUIContent("Split Here"), false, ()=>
						{
							var clickTime = SnapTime(PosToTime(mousePosition.x));
							action.Split(clickTime);
						});

						if (isScalable){
							menu.AddItem(new GUIContent("Set Start ( [ )"), false, ()=>
							{
								var temp = action.endTime;
								action.startTime = PosToTime(mousePosition.x);
								action.endTime += temp - action.endTime;								
							});

							menu.AddItem(new GUIContent("Set End ( ] )"), false, ()=>
							{
								action.endTime = PosToTime(mousePosition.x);
							});
						}
					}

					menu.AddSeparator("/");

					if (hasActiveParameters){
						menu.AddItem(new GUIContent("Reset Animation Data"), false, ()=>
						{
							if (EditorUtility.DisplayDialog("Reset Animation Data", "All Animation Curve keys of all animated parameters for this clip will be removed.\nAre you sure?", "Yes", "No")){
								SafeDoAction( ()=>{ action.ResetAnimatedParameters(); } );
							}
						});
					}

					menu.AddItem(new GUIContent("Delete Clip"), false, ()=>
					{
						SafeDoAction( ()=>{ (action.parent as CutsceneTrack).DeleteAction(action); InitBoxes(); } );
					});
				}

				menu.ShowAsContext();
				e.Use();
			}


			//Draw info text if beg enough
			if (wrapper.rect.width > 20){
				var r = new Rect(0, 0, wrapper.rect.width, wrapper.rect.height);
				if (overlapIn > 0){	r.xMin = blendInPosX; }
				if (overlapOut > 0){ r.xMax = blendOutPosX;	}
				var label = string.Format("<size=10>{0}</size>", action.info);
				GUI.color = Color.black;
				GUI.Label(r, label);
				GUI.color = Color.white;
			}

			//middle mouse click just Use
			if (e.button == 0){
				GUI.DragWindow();
			}
		}
	}
}

#endif