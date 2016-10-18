using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Slate{

	[Attachable(typeof(ActionTrack))]
	///Clips are added in CutsceneTracks to make stuff happen
	abstract public class ActionClip : MonoBehaviour, IDirectable, IKeyable {

		///Attribute to mark a field or property as an animatable parameter
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class AnimatableParameterAttribute : PropertyAttribute{
			public string link;
			public bool external;
			public float? min;
			public float? max;
			public AnimatableParameterAttribute(){}
			public AnimatableParameterAttribute(float min, float max){
				this.min = min;
				this.max = max;
			}
		}

		///Attribute used along with a Vector3 to show it's trajectory in the scene
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class ShowTrajectoryAttribute : Attribute{}

		///Attribute used along with a Vector3 to control it with a position handle in the scene
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
		public class PositionHandleAttribute : Attribute{}


		[SerializeField] [HideInInspector]
		private float _startTime;
		[SerializeField] [HideInInspector]
		private AnimationDataCollection _animationData;

		public IDirector root{ get {return parent != null? parent.root : null;} }
		public IDirectable parent{ get; private set; }
		public GameObject actor{ get {return parent != null? parent.actor : null;} }
		IEnumerable<IDirectable> IDirectable.children{ get {return null;} }

		///All animated parameters animation are stored within this collection object
		public AnimationDataCollection animationData{
			get {return _animationData;}
			private set {_animationData = value;}
		}

		///...
		public float startTime{
			get {return _startTime;}
			set
			{
				if (_startTime != value){
					_startTime = Mathf.Max(value, 0);
					blendIn = Mathf.Clamp(blendIn, 0, length - blendOut);
					blendOut = Mathf.Clamp(blendOut, 0, length - blendIn);
				}
			}
		}

		///...
		public float endTime{
			get {return startTime + length;}
			set
			{
				if (startTime + length != value){
					length = Mathf.Max(value - startTime, 0);
					blendOut = Mathf.Clamp(blendOut, 0, length - blendIn);
					blendIn = Mathf.Clamp(blendIn, 0, length - blendOut);
				}
			}
		}

		///...
		public bool isActive{
			get {return parent != null? parent.isActive && isValid : false;}
		}

		///...
		public bool isCollapsed{
			get {return parent != null? parent.isCollapsed : false;}
		}

		///...
		virtual public float length{
			get {return 0;}
			set {} //override for scalable clips
		}

		///The blend in value of the clip. A value of zero means instant
		virtual public float blendIn{
			get {return 0;}
			set {} //override for blendable in clips
		}

		///The blend out value of the clip. A value of zero means instant
		virtual public float blendOut{
			get {return 0;}
			set {} //override for blendable out clips
		}

		///A short summary. Overide this to show something specific in the action clip in the editor
		virtual public string info{
			get
			{
				var nameAtt = this.GetType().RTGetAttribute<NameAttribute>(true);
				if (nameAtt != null){
					return nameAtt.name;
				}
				return this.GetType().Name.SplitCamelCase();
			}
		}

		///Is everything ok for the clip to work?
		virtual public bool isValid{
			get { return actor != null; }
		}

		virtual public TransformSpace defaultTransformSpace{
			get {return TransformSpace.WorldSpace;}
		}

		//An array of properties/fields that will be possible to animate.
		//By default all properties/fields in the actionclip class with an [AnimatableParameter] attribute will be used.
		private MemberInfo[] _cachedParamsInfo;
		private MemberInfo[] animatedParametersInfo{
			get { return _cachedParamsInfo != null? _cachedParamsInfo : _cachedParamsInfo = this.GetType().RTGetPropsAndFields().Where( p => p.RTGetAttribute<AnimatableParameterAttribute>(true) != null).ToArray(); }
		}

		//If the params target is not this, registration of parameters should be handled manually
		private bool handleParametersRegistrationManually{
			get { return (object)animatedParametersTarget != this; }
		}

		///The target instance of the animated properties/fields.
		///By default the instance of THIS action clip is used.
		///Do NOT override if you don't know why! :)
		virtual public object animatedParametersTarget{
			get { return this; }
		}

		///Does the clip has animation
		public bool hasParameters{
			get {return animationData != null && animationData.isValid;}
		}

		///Does the clip has active animated parameters
		public bool hasActiveParameters{
			get
			{
				if (!hasParameters || !isValid){ return false; }
				for (var i = 0; i < animationData.animatedParameters.Count; i++){
					if (animationData.animatedParameters[i].enabled){
						return true;
					}
				}
				return false;
			}
		}

		bool IDirectable.Initialize(){ return OnInitialize(); }
		void IDirectable.Enter(){ SetAnimParamsSnapshot(); OnEnter(); }
		void IDirectable.Update(float time, float previousTime){ UpdateAnimParams(time, previousTime); OnUpdate(time, previousTime); }
		void IDirectable.Exit(){ OnExit(); }
		void IDirectable.ReverseEnter(){ OnReverseEnter(); }
		void IDirectable.Reverse(){ RestoreAnimParamsSnapshot(); OnReverse(); }
		

#if UNITY_EDITOR			
		void IDirectable.DrawGizmos(bool selected){
			if (selected && actor != null && isValid){
				OnDrawGizmosSelected();
			}
		}
		
		private Dictionary<MemberInfo, Attribute[]> paramsAttributes = new Dictionary<MemberInfo, Attribute[]>();
		void IDirectable.SceneGUI(bool selected){
			
			if (!selected || actor == null || !isValid){
				return;
			}

			if (hasParameters){
				for (var i = 0; i < animatedParametersInfo.Length; i++){

					var m = animatedParametersInfo[i];
					var animParam = animationData.GetParameterOfName(m.Name);
					if (animParam == null || animParam.animatedType != typeof(Vector3)){
						continue;
					}

					Attribute[] attributes = null;
					if (!paramsAttributes.TryGetValue(m, out attributes)){
						attributes = (Attribute[])m.GetCustomAttributes(false);
						paramsAttributes[m] = attributes;
					}

					ITransformableHelperParameter link = null;
					var animAtt = attributes.FirstOrDefault(a => a is AnimatableParameterAttribute) as AnimatableParameterAttribute;
					if (animAtt != null){ //only in case parameter has been added manualy. Probably never.
						if (!string.IsNullOrEmpty(animAtt.link)){
							try {link = (GetType().GetField(animAtt.link).GetValue(this) as ITransformableHelperParameter);}
							catch (Exception exc) {Debug.LogError(exc.Message);}
						}
					}

					if (link == null || link.useAnimation){

						var space = link != null? link.space : defaultTransformSpace;

						var posHandleAtt = attributes.FirstOrDefault(a => a is PositionHandleAttribute) as PositionHandleAttribute;
						if (posHandleAtt != null){
							DoParameterPositionHandle(animParam, space);
						}

						var trajAtt = attributes.FirstOrDefault(a => a is ShowTrajectoryAttribute) as ShowTrajectoryAttribute;
						if (trajAtt != null && animParam.enabled){
							CurveEditor3D.Draw3DCurve(animParam.GetCurves(), this, GetSpaceTransform(space), length/2, length);
						}
					}
				}
			}

			OnSceneGUI();
		}

		protected void DoParameterPositionHandle(AnimatedParameter animParam, TransformSpace space){
			UnityEditor.EditorGUI.BeginChangeCheck();
			var originalPos = (Vector3)animParam.GetCurrentValue(animatedParametersTarget);
			var pos = TransformPoint( originalPos, space );
			var newPos = UnityEditor.Handles.PositionHandle(pos, Quaternion.identity);
			newPos = InverseTransformPoint(newPos, space);
			UnityEditor.Handles.SphereCap(-10, pos, Quaternion.identity, 0.1f);
			if (UnityEditor.EditorGUI.EndChangeCheck()){
				UnityEditor.Undo.RecordObject(this, "Position Change");
				if (RootTimeWithinRange()){
					if (!Event.current.shift){
						animParam.SetCurrentValue(animatedParametersTarget, newPos );
					} else {
						animParam.OffsetValue(newPos - originalPos);
					}
				} else {
					animParam.SetCurrentValue(animatedParametersTarget, newPos );
					animParam.OffsetValue(newPos - originalPos);
				}

				UnityEditor.EditorUtility.SetDirty(this);
			}			
		}

		protected void DoVectorPositionHandle(TransformSpace space, ref Vector3 position){
			UnityEditor.EditorGUI.BeginChangeCheck();
			var pos = TransformPoint(position, space);
			var newPos = UnityEditor.Handles.PositionHandle(pos, Quaternion.identity);
			UnityEditor.Handles.SphereCap(-10, pos, Quaternion.identity, 0.1f);
			if (UnityEditor.EditorGUI.EndChangeCheck()){
				UnityEditor.Undo.RecordObject(this, "Parameter Change");
				position = InverseTransformPoint(newPos, space);
				UnityEditor.EditorUtility.SetDirty(this);
			}			
		}

#endif


		virtual protected bool OnInitialize(){return true;}
		virtual protected void OnEnter(){}
		virtual protected void OnUpdate(float time, float previousTime){OnUpdate(time);}
		virtual protected void OnUpdate(float time){}
		virtual protected void OnExit(){}
		virtual protected void OnReverse(){}
		virtual protected void OnReverseEnter(){}
		virtual protected void OnDrawGizmosSelected(){}
		virtual protected void OnSceneGUI(){}
		virtual protected void OnCreate(){}
		virtual protected void OnAfterValidate(){}


		///After creation
		public void PostCreate(IDirectable parent){
			this.parent = parent;
			CreateAnimationDataCollection();
			OnCreate();
		}

		//Validate the clip
		public void Validate(){OnAfterValidate();}
		public void Validate(IDirector root, IDirectable parent){
			this.parent = parent;
			ValidateAnimParams();
			hideFlags = HideFlags.HideInHierarchy;
			OnAfterValidate();
		}

		///Is the root time within clip time range?
		public bool RootTimeWithinRange(){
			return root.currentTime >= startTime && root.currentTime <= endTime && root.currentTime > 0;
		}

		///Transforms a point in specified space
		public Vector3 TransformPoint(Vector3 point, TransformSpace space){
			return parent != null? parent.TransformPoint(point, space) : point;
		}

		///Inverse Transforms a point in specified space
		public Vector3 InverseTransformPoint(Vector3 point, TransformSpace space){
			return parent != null? parent.InverseTransformPoint(point, space) : point;
		}

		///Returns the final actor position in specified Space (InverseTransform Space)
		public Vector3 ActorPositionInSpace(TransformSpace space){
			return parent != null? parent.ActorPositionInSpace(space) : Vector3.zero;
		}

		///Returns the transform object used for specified Space transformations. Null if World Space.
		public Transform GetSpaceTransform(TransformSpace space){
			return parent != null? parent.GetSpaceTransform(space) : null;
		}

		///The current clip weight
		public float GetClipWeight(){ return GetClipWeight(root.currentTime - startTime); }
		///The weight of the clip at specified local time based on its blend properties.
		public float GetClipWeight(float time){

			if (time <= 0){
				return blendIn == 0? 1 : 0;
			}

			if (time >= length){
				return blendOut == 0? 1 : 0;
			}

			if (time < blendIn){
				return time/blendIn;
			}

			if (time > length - blendOut){
				return (length - time)/blendOut;
			}

			return 1;			
		}

		///Get the AnimatedParameter of name
		public AnimatedParameter GetParameter(string paramName){
			return animationData != null? animationData.GetParameterOfName(paramName) : null;
		}

		///Enable/Disable an AnimatedParameter of name
		public void SetParameterEnabled(string paramName, bool enabled){
			var animParam = GetParameter(paramName);
			if (animParam != null){
				animParam.SetEnabled(enabled, animatedParametersTarget, root.currentTime - startTime);
			}
		}

		//Re-Init/Reset all existing animated parameters
		public void ResetAnimatedParameters(){
			animationData.Reset();
		}

		//Creates the animation data collection out of the fields/properties marked with [AnimatableParameter] attribute
		void CreateAnimationDataCollection(){

			if (handleParametersRegistrationManually){
				return;
			}

			if (animatedParametersInfo != null && animatedParametersInfo.Length != 0 ){
				animationData = new AnimationDataCollection(animatedParametersInfo, animatedParametersTarget, null);
			}
		}

		//Validate the animation parameters vs the animation data collection to be synced, adding or removing as required.
		void ValidateAnimParams(){

			if (handleParametersRegistrationManually){
				return;
			}

			if (animatedParametersInfo == null || animatedParametersInfo.Length == 0){
				animationData = null;
				return;
			}

			//try append new
			foreach (var member in animatedParametersInfo){
				if (member != null){
					animationData.TryAddParameter(member, animatedParametersTarget, null);
				}
			}

			//cleanup
			foreach(var animParam in animationData.animatedParameters.ToArray()){
				if (!animParam.isValid){
					animationData.animatedParameters.Remove(animParam);
					continue;
				}

				if (!animatedParametersInfo.Select(m => m.Name).Contains(animParam.GetMemberInfo().Name )){
					animationData.animatedParameters.Remove(animParam);
					continue;
				}
			}
		}

		//Set an animation snapshot for all parameters
		void SetAnimParamsSnapshot(){
			if (hasParameters){
				animationData.SetTransformContext( GetSpaceTransform(TransformSpace.CutsceneSpace) );
				animationData.SetSnapshot(animatedParametersTarget);
			}
		}

		//Update the animaiton parameters, setting their evaluated values
		void UpdateAnimParams(float time, float previousTime){
			if (hasParameters){
				animationData.SetEvaluatedValues(animatedParametersTarget, time);
			}			
		}

		//Try record keys
		void IKeyable.RecordKeys(float time){
			#if UNITY_EDITOR
			if (CutsceneUtility.selectedObject == this && hasParameters){
				animationData.TryKeyChangedValues(animatedParametersTarget, time, Prefs.doPairedKeying);
			}
			#endif
		}

		//Restore the animation snapshot on all parameters
		void RestoreAnimParamsSnapshot(){
			if (hasParameters){
				animationData.RestoreSnapshot(animatedParametersTarget);
			}
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		private Dictionary<AnimationCurve, Keyframe[]> retimingKeys;

		///Show clip GUI contents
		public void ShowClipGUI(Rect rect, bool retime, float preScaleStartTime, float preScaleEndTime){
			OnClipGUI(rect);
			if (hasActiveParameters){
				TryRetime(retime, preScaleStartTime, preScaleEndTime);
				ShowClipDopesheet(rect);
			}
		}

		///This is called outside of the clip for UI on the the left/right sides of the clip.
		public void ShowClipGUIExternal(Rect left, Rect right){
			OnClipGUIExternal(left, right);
		}

		///Override for extra clip GUI contents.
		virtual protected void OnClipGUI(Rect rect){}
		///Override for extra clip GUI contents outside of clip.
		virtual protected void OnClipGUIExternal(Rect left, Rect right){}

		//Handles retiming clip keys
		void TryRetime(bool retime, float preScaleStartTime, float preScaleEndTime){
			
			if (!retime){
				retimingKeys = null;
				return;
			}

			var allCurves = animationData.GetCurvesAll(); //get all curves even if param disabled for retiming
			//initialize original keys dictionary
			if (retimingKeys == null){
				retimingKeys = new Dictionary<AnimationCurve, Keyframe[]>();
				foreach(var curve in allCurves){
					retimingKeys[curve] = curve.keys;
				}
			}

			//retime keys
			foreach (var curve in allCurves){
				for (var i = 0; i < curve.keys.Length; i++){
					var preKey = retimingKeys[curve][i];
					
					//in case key outside of range, simply offset it
					if (curve[i].time > length){
						var offsetDiff = (endTime - preScaleEndTime) + (preScaleStartTime - startTime);
						preKey.time += offsetDiff;
						curve.MoveKey(i, preKey );
						continue;
					}

					var preLength = preScaleEndTime - preScaleStartTime;
					var newTime = Mathf.Lerp(0, length, preKey.time/preLength);
					preKey.time = newTime;
					curve.MoveKey(i, preKey );
				}

				curve.UpdateTangentsFromMode();
			}
		}

		//Show the clip dopesheet
		void ShowClipDopesheet(Rect rect){
			var dopeRect = new Rect(0, rect.height-13, rect.width, 13);
			GUI.color = UnityEditor.EditorGUIUtility.isProSkin?  new Color(0,0.2f,0.2f,0.5f) : new Color(0,0.8f,0.8f,0.5f);
			GUI.Box(dopeRect, "", Slate.Styles.clipBoxFooterStyle);
			GUI.color = Color.white;
			DopeSheetEditor.DrawDopeSheet(animationData, this, dopeRect, 0, length, false);
		}

		///Split the clip in two, at specified local time
		public ActionClip Split(float time){
			
			if (hasParameters){
				foreach(var param in animationData.animatedParameters){
					param.TryKeyIdentity(animatedParametersTarget, time - startTime);
				}
			}

			CutsceneUtility.CopyClip(this);
			var copy = CutsceneUtility.PasteClip( (CutsceneTrack)parent, time);
			copy.startTime = time;
			copy.endTime = this.endTime;
			this.endTime = time;
			copy.blendIn = 0;
			this.blendOut = 0;
			CutsceneUtility.selectedObject = null;
			CutsceneUtility.SetCopyType(null);

			if (hasParameters){
				foreach(var param in copy.animationData.animatedParameters){
					foreach(var curve in param.curves){
						var finalKeys = new List<Keyframe>();
						foreach (var key in curve.keys){
							var modKey = key;
							modKey.time -= length;
							if (modKey.time >= 0){
								finalKeys.Add(modKey);
							}
						}
						curve.keys = finalKeys.ToArray();
					}
				}
			}

			if (copy is ISubClipContainable){
				(copy as ISubClipContainable).subClipOffset -= length;
			}

			return copy;
		}


		#endif
	}
}