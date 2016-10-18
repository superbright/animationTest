using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Audio;

namespace Slate{

	[UniqueElement]
	[Attachable(typeof(DirectorGroup))]
	[Description("The Camera Track is the track within wich you create your camera shots and moves. Once the Camera Track becomes active, the Director Camera will be enabled. You can control when the Director Camera takes effect by setting the 'Active Time Offset', while the Blend In/Out parameters control the ammount of blending there will be from the game camera to the first and the last shot of the track. If you don't want a cinematic letterbox effect, you can set it's time to 0.")]
	///The CameraTrack is responsible to camera direction of the Cutscene
	public class CameraTrack : CutsceneTrack {

		//there can only be one active camera track!
		private static CameraTrack activeCameraTrack;

		[SerializeField] [HideInInspector]
		private float _startTimeOffset;
		[SerializeField] [HideInInspector]
		private float _endTimeOffset;

		[HideInInspector]
		public float _blendIn = 0f;
		[HideInInspector]
		public float _blendOut = 0f;
		[HideInInspector]
		public EaseType interpolation = EaseType.QuarticInOut;
		[HideInInspector]
		public float cineBoxFadeTime = 0.5f;
		[HideInInspector]
		public float appliedSmoothing = 0f;
		[HideInInspector]
		public Camera exitCameraOverride;

		private GameCamera entryCamera;

		public CameraShot firstShot{get; private set;}
		public CameraShot lastShot{get; private set;}
		public CameraShot currentShot{get;set;}

		public override string info{
			get {return string.Format("Blend In {0} / Out {1}", _blendIn.ToString(), _blendOut.ToString() );}
		}

		public override float startTime{
			get {return _startTimeOffset;}
			set {_startTimeOffset = Mathf.Clamp(value, 0, parent.endTime/2);}
		}

		public override float endTime{
			get {return parent.endTime - _endTimeOffset;}
			set {_endTimeOffset = Mathf.Clamp( parent.endTime - value, 0, parent.endTime/2);}
		}

		public override float blendIn{
			get
			{
				if (_blendIn == 0) return 0;
				return firstShot != null? firstShot.startTime - this.startTime + _blendIn : 0;
			}
			set {_blendIn = value;}
		}

		public override float blendOut{
			get
			{
				if (_blendOut == 0) return 0;
				return lastShot != null? this.endTime - lastShot.endTime + _blendOut : 0;
			}
			set {_blendOut = value;}
		}


		protected override void OnEnter(){
			if (activeCameraTrack != null){
				return;
			}

			activeCameraTrack = this;

			firstShot = (CameraShot)actions.Where(s => s.startTime >= this.startTime).FirstOrDefault();
			lastShot = (CameraShot)actions.Where(s => s.endTime <= this.endTime).LastOrDefault();
			currentShot = firstShot;

			DirectorCamera.Enable();
			DirectorCamera.CutToMain();
		}

		protected override void OnUpdate(float time, float previousTime){
			
			if (activeCameraTrack != this){
				return;
			}

			if (cineBoxFadeTime > 0){ //use fixed fade in/out time since blend times are used for the effects
				if (time <= cineBoxFadeTime) DirectorGUI.UpdateLetterbox(time/cineBoxFadeTime);
				else if (time >= endTime - startTime - cineBoxFadeTime) DirectorGUI.UpdateLetterbox(  (((endTime - startTime) - time) / cineBoxFadeTime)  );
				else DirectorGUI.UpdateLetterbox(1f);
			} else {
				DirectorGUI.UpdateLetterbox(0);
			}
			   

			if (exitCameraOverride != null){
				if (time > blendIn && entryCamera == null){
					entryCamera = DirectorCamera.gameCamera;
					var gc = exitCameraOverride.GetComponent<GameCamera>();
					if (gc == null){
						gc = exitCameraOverride.gameObject.AddComponent<GameCamera>();
					}
					DirectorCamera.gameCamera = gc;
				}

				if (time <= blendIn && entryCamera != null){
					DirectorCamera.gameCamera = entryCamera;
					entryCamera = null;
				}
			}


			var weight = GetTrackWeight(time);

			IDirectableCamera source = null;
			IDirectableCamera target = null;

			if (currentShot != null && currentShot.targetShot != null){
				target = currentShot.targetShot;
				if (currentShot.blendInEffect == CameraShot.BlendInEffectType.EaseIn){
					if (currentShot != firstShot && time < (lastShot.startTime + lastShot.blendIn - this.startTime) ){
						weight *= currentShot.GetClipWeight(time - currentShot.startTime + this.startTime) * weight;
						source = currentShot.previousShot.targetShot;					
					}
				}
			}

			//passing null source = game camera, null target = the director camera itself.
			DirectorCamera.Update(source, target, interpolation, weight, appliedSmoothing);
		}

		protected override void OnExit(){
			if (activeCameraTrack == this){
				activeCameraTrack = null;
				DirectorCamera.Disable();
			}
		}

		protected override void OnReverseEnter(){
			if (activeCameraTrack == null){
				activeCameraTrack = this;
				DirectorCamera.Enable();
				DirectorCamera.CutToMain();
			}
		}

		protected override void OnReverse(){
			if (activeCameraTrack == this){
				activeCameraTrack = null;
				DirectorCamera.Disable();
			}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public override float height{
			get {return Prefs.showShotThumbnails? 60f : base.height;}
		}

		public override Texture icon{
			get {return Styles.cameraIcon;}
		}

		public override void OnTrackTimelineGUI(Rect posRect, Rect timeRect, float cursorTime, System.Func<float, float> TimeToPos){

			base.OnTrackTimelineGUI(posRect, timeRect, cursorTime, TimeToPos);

			UnityEditor.Handles.color = Color.white;
			if (blendIn > 0){
				var first = actions.FirstOrDefault();
				if (first != null && first.startTime > this.startTime){
					var a = new Vector2(TimeToPos(this.startTime), posRect.y + posRect.height/2);
					var b = new Vector2(TimeToPos(first.startTime), a.y);
					b.x -= 1;
					UnityEditor.Handles.DrawLine(a, b);
					UnityEditor.Handles.DrawLine(b, new Vector2(b.x-5, b.y-5));
					UnityEditor.Handles.DrawLine(b, new Vector2(b.x-5, b.y+5));
				}
			}

			if (blendOut > 0){
				var last = actions.LastOrDefault();
				if (last != null && last.endTime < this.endTime){
					var a = new Vector2(TimeToPos(this.endTime), posRect.y + posRect.height/2);
					var b = new Vector2(TimeToPos(last.endTime), a.y);
					UnityEditor.Handles.DrawLine(a, b);
					UnityEditor.Handles.DrawLine(a, new Vector2(a.x-5, a.y-5));
					UnityEditor.Handles.DrawLine(a, new Vector2(a.x-5, a.y+5));
				}
			}


			if (exitCameraOverride != null){
				var text = string.Format("ExitCamera: '{0}'", exitCameraOverride.name);
				var size = GUI.skin.GetStyle("Label").CalcSize(new GUIContent(text));
				var r = Rect.MinMaxRect(TimeToPos(endTime) + 5, posRect.center.y - (size.y/2), posRect.xMax, posRect.yMax);
				GUI.Label(r, text);
			}
		}

		#endif
	}
}