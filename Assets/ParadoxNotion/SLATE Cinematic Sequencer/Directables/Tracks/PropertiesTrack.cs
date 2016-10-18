using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	[UniqueElement]
	[Name("Properties Track")]
	[Description("With the Properties Track, you can select to animate any supported type property or field on any component on the actor, or within it's whole transform hierarchy.")]
	///Properties Tracks are able to animate any supported property type with AnimationCurves
	abstract public class PropertiesTrack : CutsceneTrack, IKeyable {

		[SerializeField] [HideInInspector]
		private AnimationDataCollection _animationData = new AnimationDataCollection();

		public AnimationDataCollection animationData{
			get {return _animationData;}
			set {_animationData = value;}
		}

		public object animatedParametersTarget{
			get {return actor;}
		}

		protected override void OnEnter(){
			animationData.SetTransformContext( GetSpaceTransform(TransformSpace.CutsceneSpace) );
			animationData.SetSnapshot(actor);
		}

		protected override void OnUpdate(float deltaTime, float previousTime){
			animationData.SetEvaluatedValues(actor, deltaTime);
		}

		//Try record keys
		void IKeyable.RecordKeys(float deltaTime){
			#if UNITY_EDITOR
			if (UnityEditor.Selection.activeGameObject != null && actor != null){
				if (UnityEditor.Selection.activeGameObject.transform.IsChildOf(actor.transform)){
					animationData.TryKeyChangedValues(actor, deltaTime, false);
				}
			}
			#endif
		}

		protected override void OnReverse(){
			animationData.RestoreSnapshot(actor);
		}



		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		private bool trackExpanded = false;
		private int inspectedIndex  = -1;
		private float proposedHeight = 0;

		public override float height{
			get {return trackExpanded? proposedHeight : 16f;}
		}

		public override void OnTrackInfoGUI(){

			var animParams = animationData.animatedParameters;
			if (animParams == null || inspectedIndex >= animParams.Count){
				inspectedIndex = -1;
			}

			GUILayout.BeginVertical(GUILayout.Width(5));
			trackExpanded = UnityEditor.EditorGUILayout.Foldout(trackExpanded, this.name);
			GUILayout.EndVertical();

			var wasEnable = GUI.enabled;  
			GUI.enabled = true;

			if (trackExpanded){

				if (animParams != null){
					for (var i = 0; i < animParams.Count; i++){
						var animParam = animParams[i];
						var name = string.Format("<size=9><color=#252525>{0}</color></size>", animParam.ToString() );

						GUI.color = inspectedIndex == i? new Color(0.5f, 0.5f, 1f, 0.4f) : new Color(0, 0.5f, 0.5f, 0.5f);
						GUILayout.BeginHorizontal(Slate.Styles.headerBoxStyle);
						GUI.color = Color.white;
						GUILayout.Space(5);

						var buttonLabel = (inspectedIndex == i? string.Format("<b>{0}</b>", name) : name);
						GUILayout.Label(buttonLabel, GUILayout.MinWidth(10), GUILayout.ExpandWidth(true));

						GUILayout.FlexibleSpace();

						GUI.backgroundColor = new Color(0, 0.4f, 0.4f, 0.5f);
						if (GUILayout.Button(Slate.Styles.gearIcon, GUIStyle.none, GUILayout.Height(18))){
							var menu = new UnityEditor.GenericMenu();
							if (animParam.HasKey(root.currentTime)){
								menu.AddDisabledItem(new GUIContent("Add Key"));
								menu.AddItem(new GUIContent("Remove Key"), false, ()=>{ animParam.RemoveKey(root.currentTime); });
							} else {
								menu.AddItem(new GUIContent("Add Key"), false, ()=>{ animParam.SetKeyCurrent(actor, root.currentTime); });
								menu.AddDisabledItem(new GUIContent("Remove Key"));
							}
							menu.AddItem(new GUIContent("Post Wrap Mode/Once"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.Once); });
							menu.AddItem(new GUIContent("Post Wrap Mode/Loop"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.Loop); });
							menu.AddItem(new GUIContent("Post Wrap Mode/PingPong"), false, ()=>{ animParam.SetPostWrapMode(WrapMode.PingPong); });

							menu.AddItem(new GUIContent("Pre Wrap Mode/Once"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.Once); });
							menu.AddItem(new GUIContent("Pre Wrap Mode/Loop"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.Loop); });
							menu.AddItem(new GUIContent("Pre Wrap Mode/PingPong"), false, ()=>{ animParam.SetPreWrapMode(WrapMode.PingPong); });

							menu.AddSeparator("/");
							menu.AddItem(new GUIContent("Reset Animation"), false, ()=>
							{
								if (UnityEditor.EditorUtility.DisplayDialog("Reset Animation", "All animation keys will be removed for this property.\nAre you sure?", "Yes", "No")){
									animParam.RestoreSnapshot(actor);
									animParam.Reset();
									animParam.SetSnapshot(actor);
								}
							});
							menu.AddItem(new GUIContent("Remove Property"), false, ()=>
							{
								if (UnityEditor.EditorUtility.DisplayDialog("Remove Property", "Completely Remove Property.\nAre you sure?", "Yes", "No")){
									animParam.RestoreSnapshot(actor);
									animParams.RemoveAt( animParams.IndexOf(animParam) );
									inspectedIndex = -1;
								}
							});
							menu.ShowAsContext();
							Event.current.Use();
						}
						GUI.backgroundColor = Color.white;

						GUILayout.Space(5);
						GUILayout.EndHorizontal();

						if ( Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
							inspectedIndex = inspectedIndex == i? -1 : i;
							CurveEditor.FrameAllCurves(animParams[i]);
							Event.current.Use();
						}

						if ( Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
							Event.current.Use();
						}

						GUILayout.Space(2);
					}
				}

				GUILayout.Space(5);
				
				if (inspectedIndex != -1){
					ShowParamKeyControls(animParams[inspectedIndex]);
					GUI.enabled = root.currentTime > 0;
					ShowParameterField( animParams[inspectedIndex], actor, root.currentTime);
					GUI.enabled = wasEnable;
				}

				GUILayout.Space(5);

				if (actor == null || !wasEnable){ GUI.enabled = false; }
				if (GUILayout.Button("Add Property")){
					EditorTools.ShowAnimatedPropertySelectionMenu(actor, AnimatedParameter.supportedTypes, (p, t)=>{
						animationData.TryAddParameter(p, t, actor.transform);
					});
				}


				GUILayout.Space(15);
				GUILayout.FlexibleSpace();

				if (inspectedIndex == -1){
					if (Event.current.type == EventType.Repaint){
						var lastRect = GUILayoutUtility.GetLastRect();
						proposedHeight = lastRect.y;
					}
				} else {
					proposedHeight = 300f;
				}

				GUI.backgroundColor = Color.white;
			}

			GUI.enabled = wasEnable;
		}


		public override void OnTrackTimelineGUI(Rect posRect, Rect timeRect, float cursorTime, System.Func<float, float> TimeToPos){

			var animParams = animationData.animatedParameters;
			if (animParams == null || inspectedIndex >= animParams.Count){
				inspectedIndex = -1;
			}

			var dopeHeight = 16f;
			var baseDopeRect = new Rect(posRect.x, posRect.y, posRect.width, dopeHeight);

			if (!trackExpanded){
				GUI.color = new Color(0.5f,0.5f,0.5f,0.3f);
				GUI.Box(baseDopeRect, "", Slate.Styles.clipBoxHorizontalStyle);
				GUI.color = Color.white;
				DopeSheetEditor.DrawDopeSheet(animationData, this, baseDopeRect, timeRect.x, timeRect.width );
				return;
			}

			var expansionRect = new Rect(posRect.x, posRect.y + dopeHeight, posRect.width, posRect.height - dopeHeight);
			GUI.color = UnityEditor.EditorGUIUtility.isProSkin? new Color(0,0,0,0.5f) : new Color(0,0,0,0.3f);
			GUI.Box(expansionRect, "", (GUIStyle)"textfield");
			GUI.color = Color.white;
			if (animParams == null || animParams.Count == 0){
				var labelStyle = new GUIStyle("label");
				labelStyle.alignment = TextAnchor.MiddleCenter;
				GUI.Label( posRect, "There are no Animated Properties. You can add one on the left side.", labelStyle );
				return;
			}


			if (inspectedIndex == -1){

				GUI.color = new Color(0.5f,0.5f,0.5f,0.25f);
				GUI.Box(baseDopeRect, "", Slate.Styles.clipBoxHorizontalStyle);
				GUI.color = Color.white;
				DopeSheetEditor.DrawDopeSheet(animationData, this, baseDopeRect, timeRect.x, timeRect.width );

				var y = baseDopeRect.yMax + 6;
				foreach (var animParam in animParams){
					var subDopeRect = new Rect(posRect.x, y, posRect.width, dopeHeight);

					GUI.color = UnityEditor.EditorGUIUtility.isProSkin? new Color(0f,0.5f,0.5f,0.25f) : new Color(0f,0.5f,0.5f,0.1f);
					GUI.Box(subDopeRect, "", Slate.Styles.clipBoxHorizontalStyle);
					GUI.color = Color.white;

					DopeSheetEditor.DrawDopeSheet(animParam, this, subDopeRect, timeRect.x, timeRect.width );
					y += dopeHeight + 4;
				}

			} else {

				GUI.color = new Color(0f,0.5f,0.5f,0.25f);
				GUI.Box(baseDopeRect, "", Slate.Styles.clipBoxHorizontalStyle);
				GUI.color = Color.white;

				var animParam = animParams[inspectedIndex];
				DopeSheetEditor.DrawDopeSheet(animParam, this, baseDopeRect, timeRect.x, timeRect.width );
				CurveEditor.DrawCurves(animParam, expansionRect, timeRect);
			}
		}


		protected override void OnSceneGUI(){

			if (actor == null || actor.Equals(null) || !trackExpanded || !isActive || animationData == null || !animationData.isValid){
				return;
			}

			for (var i = 0; i < animationData.animatedParameters.Count; i++){
				var animParam = animationData.animatedParameters[i];
				if (animParam.isValid && animParam.parameterName == "localPosition"){
					var transform = animParam.ResolvedObject(animatedParametersTarget) as Transform;
					if (transform != null){
						var context = transform.parent != null? transform.parent : GetSpaceTransform(TransformSpace.CutsceneSpace);
						CurveEditor3D.Draw3DCurve(animParam.GetCurves(), this, context, root.currentTime);
					}
				}
			}
		}


		void ShowParamKeyControls(AnimatedParameter animParam){
			
			if (animParam == null){
				return;
			}

			GUI.color = new Color(0,0,0,0.1f);
			GUILayout.BeginHorizontal(Slate.Styles.headerBoxStyle);
			GUI.color = Color.white;
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button(Slate.Styles.previousKeyIcon, GUIStyle.none, GUILayout.Height(18), GUILayout.Width(18))){
				root.currentTime = animParam.GetKeyPrevious(root.currentTime);
			}

			var hasKey = animParam.HasKey(root.currentTime);
			GUI.color = hasKey? Color.red : Color.white;
			if (GUILayout.Button(Slate.Styles.keyIcon, GUIStyle.none, GUILayout.Height(18), GUILayout.Width(18))){
				if (hasKey){ animParam.RemoveKey(root.currentTime); }
				else { animParam.SetKeyCurrent(actor, root.currentTime); }
			}
			GUI.color = Color.white;

			if (GUILayout.Button(Slate.Styles.nextKeyIcon, GUIStyle.none, GUILayout.Height(18), GUILayout.Width(18))){
				root.currentTime = animParam.GetKeyNext(root.currentTime);
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		void ShowParameterField(AnimatedParameter animParam, object obj, float time){

			if (animParam == null || obj == null || obj.Equals(null)){
				GUILayout.Label("null actor");
				return;
			}

			GUI.color = new Color(0.5f,0.5f,0.5f,0.5f);
			GUILayout.BeginVertical(Slate.Styles.clipBoxFooterStyle);
			GUI.color = Color.white;

			try
			{
				var temp = animParam.GetCurrentValue(obj);
				var type = temp != null? temp.GetType() : animParam.animatedType;
				if (type == typeof(bool)){
					temp = UnityEditor.EditorGUILayout.Toggle("Current Value", (bool)temp, GUILayout.Height(18));
				}
				if (type == typeof(float)){
					temp = UnityEditor.EditorGUILayout.FloatField("Current Value", (float)temp, GUILayout.Height(18));
				}
				if (type == typeof(int)){
					temp = UnityEditor.EditorGUILayout.IntField("Current Value", (int)temp, GUILayout.Height(18));
				}
				if (type == typeof(Vector2)){
					temp = UnityEditor.EditorGUILayout.Vector2Field("", (Vector2)temp, GUILayout.Height(18));
				}
				if (type == typeof(Vector3)){
					temp = UnityEditor.EditorGUILayout.Vector3Field("", (Vector3)temp, GUILayout.Height(18));
				}				
				if (type == typeof(Color)){
					temp = UnityEditor.EditorGUILayout.ColorField("", (Color)temp, GUILayout.Height(18));
				}

				if (GUI.changed){
					animParam.SetCurrentValue(obj, temp);
					animParam.TryKeyChangedValues(obj, time);
				}
			}

			catch (System.Exception e)
			{
				GUILayout.Label(e.Message);
			}			

			GUILayout.EndVertical();
		}

		#endif
	}
}