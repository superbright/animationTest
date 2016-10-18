using UnityEngine;
using System.Collections;

namespace Slate{

	///The master director render camera for all cutscenes.
	public class DirectorCamera : MonoBehaviour, IDirectableCamera {

		public const string SET_MAIN_KEY = "Slate.SetMainWhenActive";
		public const string MATCH_MAIN_KEY = "Slate.MatchMainCamera";

		///Raised when a camera cut takes place from one shot to another.
		public static event System.Action<IDirectableCamera> OnCut;
		///Raised when the Director Camera is activated/enabled.
		public static event System.Action OnActivate;
		///Raised when the Director Camera is deactivated/disabled.
		public static event System.Action OnDeactivate;

		private static DirectorCamera _current;
		private static Camera _cam;
		private static IDirectableCamera lastTargetShot;

		public static DirectorCamera current{
			get
			{
				if (_current == null){
						_current = FindObjectOfType<DirectorCamera>();
						if (_current == null){
							_current = new GameObject("★ Director Camera Root").AddComponent<DirectorCamera>();
							_current.cam.nearClipPlane = 0.01f;
							_current.cam.farClipPlane = 1000;
						}
					}
				return _current;
			}
		}

		/////////

		public Camera cam{
			get
			{
				if (_cam == null){
					_cam = GetComponentInChildren<Camera>(true);
					if (_cam == null){
						_cam = CreateRenderCamera();
					}
				}
				return _cam;
			}
		}

		public Vector3 position{
			get{return current.transform.position;}
			set {current.transform.position = value;}
		}

		public Quaternion rotation{
			get {return current.transform.rotation;}
			set {current.transform.rotation = value;}
		}

		public float fieldOfView{
			get {return cam.orthographic? cam.orthographicSize : cam.fieldOfView;}
			set {cam.fieldOfView = value; cam.orthographicSize = value;}
		}

		///We do this through reflection in case user doesn't have DepthOfField in project. It's ugly! :/
		[System.NonSerialized]
		private object dof;
		[System.NonSerialized]
		private bool hasDof = true;
		public float focalPoint{
			get
			{
				if (!hasDof){ return 0.5f; }
				if (dof == null){
					dof = cam.GetComponent("DepthOfField");
				}
				if (dof != null){
					var settings = dof.GetType().RTGetField("focus").GetValue(dof);
					if (settings != null){
						return (float)settings.GetType().RTGetField("plane").GetValue(settings);
					}					
				}
				hasDof = false;
				return 0.5f;
			}
			set
			{
				if (!hasDof){ return; }
				if (dof == null){
					dof = cam.GetComponent("DepthOfField");
				}
				if (dof != null){
					var settings = dof.GetType().RTGetField("focus").GetValue(dof);
					if (settings != null){
						settings.GetType().RTGetField("plane").SetValue(settings, value);
						dof.GetType().RTGetField("focus").SetValue(dof, settings);
						return;
					}					
				}
				hasDof = false;
			}
		}
		/////////

		///Should DirectorCamera be set as Camera.main when active?
		public static bool setMainWhenActive{
			get {return PlayerPrefs.GetInt(SET_MAIN_KEY, 1) == 1;}
			set {PlayerPrefs.SetInt(SET_MAIN_KEY, value? 1 : 0);}
		}

		///Should DirectorCamera be matched to Camera.main when becomes active?
		public static bool matchMainCamera{
			get {return PlayerPrefs.GetInt(MATCH_MAIN_KEY, 1) == 1;}
			set {PlayerPrefs.SetInt(MATCH_MAIN_KEY, value? 1 : 0);}
		}

		public static GameCamera gameCamera{get; set;}
		public static bool isEnabled{get; private set;}

		void Awake(){

			if (_current != null && _current != this){
				DestroyImmediate(this.gameObject);
				return;
			}

			_current = this;
			DontDestroyOnLoad(this.gameObject);
			Disable();
		}

		void Reset(){
			CreateRenderCamera();
			Disable();
		}

		void OnValidate(){
			if (this == _current){
				Disable();
			}
		}

		Camera CreateRenderCamera(){
			_cam = new GameObject("Render Camera").AddComponent<Camera>();
			_cam.gameObject.AddComponent<AudioListener>();
			_cam.gameObject.AddComponent<GUILayer>();
			_cam.gameObject.AddComponent<FlareLayer>();
			_cam.transform.SetParent(this.transform);
			return _cam;
		}

		///Enable the Director Camera, while disabling the main camera if any
		public static void Enable(){

			if (gameCamera == null){
				var main = Camera.main;
				if (main != null){
					gameCamera = main.GetComponent<GameCamera>();
					if (gameCamera == null){
						gameCamera = main.gameObject.AddComponent<GameCamera>();
					}					
				}
			}

			if (gameCamera != null){
				gameCamera.gameObject.SetActive(false);
			}


			if (setMainWhenActive){
				current.cam.gameObject.tag = "MainCamera";
			}

			foreach (var component in current.gameObject.GetComponentsInChildren<Behaviour>()){
				component.enabled = true;
			}

			isEnabled = true;
			lastTargetShot = null;

			if (OnActivate != null){
				OnActivate();
			}
		}

		///Disable the Director Camera, while enabling back the main camera if any
		public static void Disable(){

			if (OnDeactivate != null){
				OnDeactivate();
			}

			foreach (var component in current.GetComponentsInChildren<Behaviour>()){
				component.enabled = false;
			}

			current.cam.gameObject.tag = "Untagged";

			if (gameCamera != null){
				gameCamera.gameObject.SetActive(true);
			}

			isEnabled = false;
		}

		///Matches DirectorCamera to MainCamera
		public static void CutToMain(){
			if (gameCamera != null && current != null){
				if (matchMainCamera){
					current.cam.CopyFrom(gameCamera.cam);
				}
				current.transform.position = gameCamera.transform.position;
				current.transform.rotation = gameCamera.transform.rotation;
			}

			current.cam.transform.localPosition = Vector3.zero;
			current.cam.transform.localRotation = Quaternion.identity;
		}

		///Ease from game camera to target. If target is null, eases to DirectorCamera current.
		public static void Update(IDirectableCamera source, IDirectableCamera target, EaseType interpolation, float weight, float damping = 3f){

			if (current == null){
				Debug.LogError("Director Render Camera is, or became null.", current.gameObject);
				return;
			}

			if (source == null){ source = gameCamera != null? (IDirectableCamera)gameCamera : (IDirectableCamera)current; }
			if (target == null){ target = current; }

			var isCut = target != lastTargetShot;
			if (isCut && OnCut != null){
				OnCut(target);
			}

			var targetPosition = weight < 1? Easing.Ease(interpolation, source.position, target.position, weight)		 : target.position;
			var targetRotation = weight < 1? Easing.Ease(interpolation, source.rotation, target.rotation, weight)		 : target.rotation;
			var targetFOV      = weight < 1? Easing.Ease(interpolation, source.fieldOfView, target.fieldOfView, weight)	 : target.fieldOfView;
			var targetFocal    = weight < 1? Easing.Ease(interpolation, source.focalPoint, target.focalPoint, weight)	 : target.focalPoint;

			if (!isCut && Application.isPlaying && damping > 0){
				current.position = Vector3.Lerp(current.position, targetPosition, Time.deltaTime * damping);
				current.rotation = Quaternion.Lerp(current.rotation, targetRotation, Time.deltaTime * damping);
				current.fieldOfView = Mathf.Lerp(current.fieldOfView, targetFOV, Time.deltaTime * damping);
				current.focalPoint = Mathf.Lerp(current.focalPoint, targetFocal, Time.deltaTime * damping);
			
			} else {
				current.position    = targetPosition;
				current.rotation    = targetRotation;
				current.fieldOfView = targetFOV;
				current.focalPoint  = targetFocal;
			}
				
			lastTargetShot = target;
		}



		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		void OnDrawGizmos(){

			var color = Prefs.gizmosColor;
			if (!isEnabled){ color.a = 0.2f;}
			Gizmos.color = color;

			var hit = new RaycastHit();
			if (Physics.Linecast(cam.transform.position, cam.transform.position - new Vector3(0, 100, 0), out hit)){
				var d = Vector3.Distance(hit.point, cam.transform.position);
				Gizmos.DrawLine(cam.transform.position, hit.point);
				Gizmos.DrawCube(hit.point, new Vector3(0.2f, 0.05f, 0.2f));
				Gizmos.DrawCube(hit.point + new Vector3(0, d/2, 0), new Vector3(0.02f, d, 0.02f));
			}

			Gizmos.DrawLine(transform.position, cam.transform.position);

			if (isEnabled){color = Color.green;}
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
			Gizmos.DrawFrustum(cam.transform.position, fieldOfView, 0, isEnabled? 0.8f : 0.5f, 1);

			color.a = 0.2f;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			Gizmos.DrawFrustum(transform.position, fieldOfView, 0, 0.5f, 1);
		}			

		#endif
	}
}