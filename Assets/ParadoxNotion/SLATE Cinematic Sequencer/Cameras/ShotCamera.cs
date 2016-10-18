using UnityEngine;
using System.Collections;

namespace Slate{

	[AddComponentMenu("SLATE/Shot Camera")]
	[RequireComponent(typeof(Camera))]
	///A camera for a shot within a Camera Track. We never render tnrough this. It only acts a parameters container.
	public class ShotCamera : MonoBehaviour, IDirectableCamera {

		[SerializeField]
		private float _focalPoint = 0.5f;

		private Camera _cam;
		public Camera cam{
			get {return _cam != null? _cam : _cam = GetComponent<Camera>();}
		}

		public Vector3 position{
			get {return transform.position;}
			set {transform.position = value;}			
		}

		public Quaternion rotation{
			get {return transform.rotation;}
			set {transform.rotation = value;}
		}
		
		public Vector3 localPosition{
			get {return transform.localPosition;}
			set {transform.localPosition = value;}
		}

		public Vector3 localEulerAngles{
			get {return transform.GetLocalEulerAngles();}
			set {transform.SetLocalEulerAngles(value);}
		}

		public float fieldOfView{
			get {return cam.orthographic? cam.orthographicSize : cam.fieldOfView;}
			set {cam.fieldOfView = value; cam.orthographicSize = value;}
		}

		public float focalPoint{
			get {return _focalPoint;}
			set {_focalPoint = value;}
		}


		void Awake(){
			cam.enabled = false;
			if (cam.targetTexture != null){
				cam.targetTexture.Release();
				DestroyImmediate(cam.targetTexture);
			}
		}

		public RenderTexture GetRenderTexture(int width, int height){
			var rt = cam.targetTexture;
			if (rt == null){
				rt = new RenderTexture(width, height, 24);
			}
			if (rt.width != width || rt.height != height){
				rt.Release();
				DestroyImmediate(rt, true);
				rt = new RenderTexture(width, height, 24);
			}
			cam.targetTexture = rt;
			cam.Render();
			return rt;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		void OnValidate(){Validate();}
		void Reset(){Validate();}

		void Validate(){
			cam.enabled = false;
			cam.cameraType = CameraType.Preview;
			cam.renderingPath = RenderingPath.DeferredShading; //provides better preview
			cam.nearClipPlane = 0.01f;
			cam.hideFlags = HideFlags.HideInHierarchy; //to hide default camera gizmos, specificaly the frustum which gets distracting
			if (cam.targetTexture != null){
				cam.targetTexture.Release();
				DestroyImmediate(cam.targetTexture, true);
			}
		}

		void OnDrawGizmos(){
			Gizmos.DrawIcon(transform.position, "Camera Gizmo");
			var color = Prefs.gizmosColor;
			Gizmos.color = color;

			var hit = new RaycastHit();
			if (Physics.Linecast(position, position - new Vector3(0, 100, 0), out hit)){
				var d = Vector3.Distance(hit.point, position);
				Gizmos.DrawLine(position, hit.point);
				Gizmos.DrawCube(hit.point, new Vector3(0.2f, 0.05f, 0.2f));
				Gizmos.DrawCube(hit.point + new Vector3(0, d/2, 0), new Vector3(0.02f, d, 0.02f));
			}
			Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

			var selectedInEditor = CutsceneUtility.selectedObject is CameraShot && (CutsceneUtility.selectedObject as CameraShot).targetShot == this;
			color.a = selectedInEditor? 1 : 0.3f;
			Gizmos.color = color;
			
			Gizmos.DrawFrustum(position, fieldOfView, 0f, 0.5f, 1);
			Gizmos.color = Color.white;
		}

			
		public static ShotCamera Create(Transform targetParent = null){
			var rootName = "[ CAMERA SHOTS ]";
			GameObject root = null;
			if (targetParent == null){
				root = GameObject.Find(rootName);
				if (root == null){
					root = new GameObject(rootName);
				}
			} else {
				var child = targetParent.Find(rootName);
				if (child != null){
					root = child.gameObject;
				} else {
					root = new GameObject(rootName);
				}
			}
			root.transform.SetParent(targetParent, false);

			var shot = new GameObject("Shot Camera").AddComponent<ShotCamera>();
			shot.transform.SetParent(root.transform, false);
			shot.cam.nearClipPlane = 0.01f;
			shot.cam.farClipPlane = 1000;

			if (UnityEditor.SceneView.lastActiveSceneView != null){
				var sc = UnityEditor.SceneView.lastActiveSceneView.camera;
				shot.position = sc.transform.position;
				shot.rotation = sc.transform.rotation;
				shot.cam.orthographic = UnityEditor.SceneView.lastActiveSceneView.in2DMode;
				shot.fieldOfView = UnityEditor.SceneView.lastActiveSceneView.in2DMode? sc.orthographicSize : sc.fieldOfView;
				shot.cam.orthographicSize = sc.orthographicSize;

			} else {
				Debug.Log("Remember that creating a ShotCamera with the Scene View open, creates it at the editor camera position");
			}

			return shot;
		}

		#endif
		
	}
}