using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	[Attachable(typeof(CameraTrack))]
	[Description("Camera Shots can be keyframed directly within this clip. You don't need to create a Actor Group to animate the shot. (The SteadyCam Effect is only visible in playmode)")]
	public class CameraShot : DirectorActionClip {

		public enum BlendInEffectType{
			None,
			FadeIn,
			CrossDissolve,
			EaseIn
		}
		
		public enum BlendOutEffectType{
			None,
			FadeOut
		}

		public enum ShotAnimationMode{
			DontUse,
			UseInternal,
			UseExternalAnimationClip
		}

		[SerializeField] [HideInInspector]
		private float _length = 5;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0.5f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0.5f;

		[SerializeField] [HideInInspector]
		private ShotCamera _targetShot;

		public BlendInEffectType blendInEffect;
		public BlendOutEffectType blendOutEffect;
		public ShotAnimationMode shotAnimationMode = ShotAnimationMode.UseInternal;
		[HideInInspector]
		public AnimationClip externalAnimationClip;
		[Range(0, 1)]
		public float steadyCamEffect;

		//blend effects
		private Color lastColor;
		private RenderTexture dissolver;
		//

		//applied effects
		private Vector3 posOffset;
		private Vector3 rotOffset;
		private float steadyCamTimer;
		private Vector3 targetPosOffset;
		private Vector3 targetRotOffset;
		private Vector3 steadyCamPosVel;
		private Vector3 steadyCamRotVel;
		//

		
		public CameraShot previousShot{get; private set;}

		public ShotCamera targetShot{
			get {return _targetShot;}
			set
			{
				if (_targetShot != value){
					_targetShot = value;

					#if !NO_UTJ
					if (value != null){ //in case of alembic camera, it's own animation is used.
						if (value.gameObject.GetComponent<UTJ.AlembicCamera>() != null){
							shotAnimationMode = ShotAnimationMode.DontUse;
						}
					}
					#endif
					
					base.ResetAnimatedParameters();
				}
			}
		}

		public override string info{
			get
			{
				if (targetShot != null && shotAnimationMode == ShotAnimationMode.UseExternalAnimationClip && externalAnimationClip != null){
					return externalAnimationClip.name;
				}				

				#if UNITY_EDITOR
				return targetShot != null? (Prefs.showShotThumbnails && length > 0? null : targetShot.gameObject.name) : "No Shot Selected";
				#else
				return targetShot != null? targetShot.gameObject.name : "No Shot Selected";
				#endif
			}
		}

		public override bool isValid{
			get {return targetShot != null;}
		}

		public override float length{
			get {return _length;}
			set {_length = value;}
		}

		public override float blendIn{
			get {return blendInEffect != BlendInEffectType.None? _blendIn : -1;}
			set {_blendIn = value;}
		}

		public override float blendOut{
			get {return blendOutEffect != BlendOutEffectType.None? _blendOut : -1;}
			set {_blendOut = value;}
		}

		new public GameObject actor{ //not REALY needed
			get {return targetShot? targetShot.gameObject : base.actor;}
		}

		private CameraTrack track{
			get {return (CameraTrack)parent;}
		}


		[AnimatableParameter]
		public Vector3 position{
			get {return targetShot? targetShot.localPosition - posOffset : Vector3.zero;}
			set {if (targetShot != null) targetShot.localPosition = value + posOffset;}
		}

		[AnimatableParameter]
		public Vector3 rotation{
			get {return targetShot? targetShot.localEulerAngles - rotOffset : Vector3.zero;}
			set {if (targetShot != null) targetShot.localEulerAngles = value + rotOffset;}
		}

		[AnimatableParameter(0.01f, 170f)]
		public float fieldOfView{
			get {return targetShot? targetShot.fieldOfView : 60f;}
			set {if (targetShot != null) targetShot.fieldOfView = Mathf.Clamp(value, 0.01f, 170);}
		}

		[AnimatableParameter(0f, 1f)]
		public float focalPoint{
			get {return targetShot? targetShot.focalPoint : 0.5f;}
			set {if (targetShot != null) targetShot.focalPoint = Mathf.Clamp(value, 0, 1) ;}
		}


		protected override void OnEnter(){
			previousShot = track.currentShot;
			track.currentShot = this;

			lastColor = DirectorGUI.fadeColor;

			if (blendInEffect == BlendInEffectType.CrossDissolve && previousShot != null){
				#if !UNITY_EDITOR
				if (dissolver == null){ dissolver = new RenderTexture(Screen.width, Screen.height, 24); }
				previousShot.targetShot.cam.targetTexture = dissolver;
				#endif
			}
		}


		protected override void OnUpdate(float deltaTime){

			if (Application.isPlaying && steadyCamEffect > 0){
				var posMlt = Mathf.Lerp(0.2f, 0.4f, steadyCamEffect);
				var rotMlt = Mathf.Lerp(5, 10f, steadyCamEffect);
				var damp = Mathf.Lerp(3, 1, steadyCamEffect);
				if (steadyCamTimer <= 0){
					steadyCamTimer = Random.Range(0.2f, 0.3f);
					targetPosOffset = Random.insideUnitSphere * posMlt;
					targetRotOffset = Random.insideUnitSphere * rotMlt;
				}
				steadyCamTimer -= Time.deltaTime;
				posOffset = Vector3.SmoothDamp(posOffset, targetPosOffset, ref steadyCamPosVel, damp);
				rotOffset = Vector3.SmoothDamp(rotOffset, targetRotOffset, ref steadyCamRotVel, damp);
			}


			if (shotAnimationMode == ShotAnimationMode.UseExternalAnimationClip && externalAnimationClip != null){
				externalAnimationClip.SampleAnimation(targetShot.gameObject, deltaTime);
			}

			if (blendInEffect == BlendInEffectType.FadeIn){
				if (deltaTime <= blendIn){
					var color = Color.black;
					color.a = Easing.Ease(EaseType.QuadraticInOut, 1, 0, GetClipWeight(deltaTime));
					DirectorGUI.UpdateFade(color);
				} else if (deltaTime < length - blendOut) {
					DirectorGUI.UpdateFade(Color.clear);
				}
			}

			if (blendOutEffect == BlendOutEffectType.FadeOut){
				if (deltaTime >= length - blendOut){
					var color = Color.black;
					color.a = Easing.Ease(EaseType.QuadraticInOut, 1, 0, GetClipWeight(deltaTime));
					DirectorGUI.UpdateFade(color);
				} else if (deltaTime > blendIn) {
					DirectorGUI.UpdateFade(Color.clear);
				}
			}

			if (blendInEffect == BlendInEffectType.CrossDissolve && deltaTime <= length - blendOut){
				#if UNITY_EDITOR
				dissolver = EditorTools.GetCameraTexture(previousShot.targetShot.cam, Screen.width, Screen.height);
				#else
				previousShot.targetShot.cam.Render();
				#endif
				var ease = Easing.Ease(EaseType.QuadraticInOut, 0, 1, GetClipWeight(deltaTime));
				DirectorGUI.UpdateDissolve(dissolver, ease);
			}
		}


		protected override void OnReverse(){
			DirectorGUI.UpdateFade(lastColor);
			DirectorGUI.UpdateDissolve(null, 0);
			if (dissolver != null) dissolver.Release();

			track.currentShot = previousShot;
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		[System.NonSerialized]
		private int thumbRefresher;
		[System.NonSerialized]
		private Texture thumbnail;
		[System.NonSerialized]
		private float lastSampleTime;
		[System.NonSerialized]
		private Vector3 lastPos;
		[System.NonSerialized]
		private Quaternion lastRot;
		
		[System.NonSerialized]
		private bool _lookThrough;

		public bool lookThrough{
			get { return _lookThrough; }
			set
			{
				if (_lookThrough != value){
					_lookThrough = value;
					if (value == true){
						var sc = UnityEditor.SceneView.lastActiveSceneView;
						if (sc != null && targetShot != null){
							targetShot.cam.orthographic = sc.in2DMode;
							sc.rotation = targetShot.rotation;
							sc.pivot = targetShot.position + (targetShot.transform.forward * 5);
							sc.size = 5;
							lastPos = sc.camera.transform.position;
							lastRot = sc.camera.transform.rotation;
						}
					}
				}
			}
		}


		protected override void OnSceneGUI(){
			
			if (targetShot == null){
				return;
			}

			UnityEditor.Handles.BeginGUI();
			GUI.backgroundColor = lookThrough? Color.red : Color.white;
			if (targetShot != null && GUI.Button(new Rect(2,2,200,20), lookThrough? "Exit Look Through Camera" : "Look Through Camera")){
				lookThrough = !lookThrough;
			}
			GUI.backgroundColor = Color.white;
			UnityEditor.Handles.EndGUI();

			var sc = UnityEditor.SceneView.lastActiveSceneView;
			if (lookThrough && sc != null){
				if (root.currentTime == lastSampleTime){

					if (sc.camera.transform.position != lastPos || sc.camera.transform.rotation != lastRot){
						UnityEditor.Undo.RecordObject(targetShot.transform, "Shot Change");
						targetShot.position = sc.camera.transform.position;
						targetShot.rotation = sc.camera.transform.rotation;
						UnityEditor.EditorUtility.SetDirty(targetShot.gameObject);
					}

					lastPos = sc.camera.transform.position;
					lastRot = sc.camera.transform.rotation;

				} else {

					if (sc.camera.transform.position != targetShot.position || sc.camera.transform.rotation != targetShot.rotation){
						sc.rotation = targetShot.rotation;
						sc.pivot = targetShot.position + (targetShot.transform.forward * 5);
						sc.size = 5;
					}
				}

				lastSampleTime = root.currentTime;
			}	


			//show 3D curves only if not looking through
			if (!lookThrough && shotAnimationMode == ShotAnimationMode.UseInternal){
				var posParam = GetParameter("position");
				CurveEditor3D.Draw3DCurve(posParam.GetCurves(), this, targetShot.transform.parent, length/2, length );
			}
		}
		

		protected override void OnClipGUI(Rect rect){

			if (Prefs.showShotThumbnails && rect.width > 20 && targetShot != null){

				if (thumbRefresher == 0 || thumbRefresher % Prefs.thumbnailsRefreshInterval == 0){
					var res = EditorTools.GetGameViewSize();
					var width = (int)res.x;
					var height = (int)res.y;
					thumbnail = targetShot.GetRenderTexture(width, height);
				}

				thumbRefresher ++;
				
				if (thumbnail != null){
					GUI.backgroundColor = Color.clear;
					var style = new GUIStyle("Box");
					style.alignment = TextAnchor.MiddleCenter;
					var thumbRect1 = new Rect(0, 0, 100, rect.height);
					GUI.Box(thumbRect1, thumbnail, style);
					if (rect.width > 400){
						var thumbRect2 = new Rect(rect.width - 100, 0, 100, rect.height);
						GUI.Box(thumbRect2, thumbnail, style);
					}
					GUI.backgroundColor = Color.white;
				}
			}
		}

		#endif
	}
}