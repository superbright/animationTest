using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	///The topmost IDirectable of a Cutscene, containing CutsceneTracks and targeting a specific GameObject Actor
	abstract public class CutsceneGroup : MonoBehaviour, IDirectable {

		public enum ActorReferenceMode{
			UseOriginal,
			UseInstanceHideOriginal
		}

		public enum ActorInitialTransformation{
			UseOriginal,
			UseLocal
		}

		///Raised when a section has been reached
		public event System.Action<Section> OnSectionReached;

		[SerializeField] [HideInInspector]
		private List<CutsceneTrack> _tracks = new List<CutsceneTrack>();
		[SerializeField] [HideInInspector]
		private List<Section> _sections = new List<Section>();
		[SerializeField] [HideInInspector]
		private bool _isCollapsed;
		[SerializeField] [HideInInspector]
		private bool _active = true;		

		private TransformSnapshot transformSnapshot;
		private ObjectSnapshot objectSnapshot;
		private GameObject originalActor;

		new abstract public string name{get;}

		///The actor gameobject that is attached to this group
		abstract public GameObject actor{get;set;}
		///The mode of reference for target actor
		abstract public ActorReferenceMode referenceMode{get;set;}
		///The mode of initial transformation for target actor
		abstract public ActorInitialTransformation initialTransformation{get;set;}
		///The local position of the actor in Cutscene Space if set to UseLocal
		abstract public Vector3 initialLocalPosition{get;set;}
		///The local rotation of the actor in Cutscene Space if set to UseLocal
		abstract public Vector3 initialLocalRotation{get;set;}

		//the child tracks
		public List<CutsceneTrack> tracks{
			get {return _tracks;}
			set {_tracks = value;}
		}

		//the sections defined for this group
		public List<Section> sections{
			get {return _sections;}
			set {_sections = value;}
		}

		IEnumerable<IDirectable> IDirectable.children{ get {return tracks.Cast<IDirectable>();} }
		float IDirectable.startTime{ get {return 0;} }
		float IDirectable.endTime{ get {return root.length;} }
		float IDirectable.blendIn{	get {return 0f;} }
		float IDirectable.blendOut{ get {return 0f;} }
		IDirectable IDirectable.parent{ get {return null;} }
		public IDirector root{get; private set;}

		public bool isActive{
			get	{return _active;}
			set {_active = value;}
		}
		
		public bool isCollapsed{
			get {return _isCollapsed;}
			set {_isCollapsed = value;}
		}

		//Validate the group and it's tracks
		public void Validate(IDirector root, IDirectable parent){
			this.root = root;
			var foundTracks = GetComponentsInChildren<CutsceneTrack>();
			for (var i = 0; i < foundTracks.Length; i++){
				if (!tracks.Contains(foundTracks[i])){
					tracks.Add(foundTracks[i]);
				}
			}
			if (tracks.Any(t => t == null)){ tracks = foundTracks.ToList(); }
		}

		//Get a Section it's name
		public Section GetSectionByName(string name){
			if (name.ToUpper() == "INTRO") return new Section("Intro", 0);
			return sections.Find(s => s.name.ToUpper() == name.ToUpper());
		}

		//Get a Section it's UID
		public Section GetSectionByUID(string UID){
			return sections.Find(s => s.UID == UID);
		}

		///Get a Section whos time is great specified time
		public Section GetSectionAfter(float time){
			return sections.Where(s => s.time > time).FirstOrDefault();
		}

		///Get a Section whos time is less specified time
		public Section GetSectionBefore(float time){
			return sections.Where(s => s.time < time).LastOrDefault();
		}

		///Transforms a point in specified space
		public Vector3 TransformPoint(Vector3 point, TransformSpace space){
			var t = GetSpaceTransform(space);
			return t != null? t.TransformPoint(point) : point;
		}

		///Inverse Transforms a point in specified space
		public Vector3 InverseTransformPoint(Vector3 point, TransformSpace space){
			var t = GetSpaceTransform(space);
			return t != null? t.InverseTransformPoint(point) : point;
		}

		///Returns the transform object used for specified Space transformations. Null if World Space.
		public Transform GetSpaceTransform(TransformSpace space){
			if (space == TransformSpace.CutsceneSpace){
				return root != null? root.context.transform : null;
			}
			if (space == TransformSpace.ActorSpace){
				return actor != null? actor.transform : null;
			}
			return null; //world space
		}

		///Returns the final actor position in specified Space (InverseTransform Space)
		public Vector3 ActorPositionInSpace(TransformSpace space){
			return actor != null? InverseTransformPoint(actor.transform.position, space) : root.context.transform.position;
		}


		bool IDirectable.Initialize(){
			
			if (actor == null){
				return false;
			}

			#if UNITY_EDITOR //do a fail safe checkup at least in editor
			var prefabType = UnityEditor.PrefabUtility.GetPrefabType(actor);
			if (prefabType == UnityEditor.PrefabType.Prefab || prefabType == UnityEditor.PrefabType.ModelPrefab){
				if (referenceMode == ActorReferenceMode.UseOriginal){
					Debug.LogWarning("A prefab is referenced in an Actor Group, but the Reference mode is set to Use Original. This is not allowed to avoid prefab corruption. Please select the Actor Group and set Refrence Mode to 'Use Instance'");
					return false;
				}
			}
			#endif			
	
			return true;
		}

		///Store undo snapshot
		void IDirectable.Enter(){

			if (referenceMode == ActorReferenceMode.UseInstanceHideOriginal){
				InstantiateLocalActor(); //if we get an instance, no need to store anything
				return;
			}

			Store();

			if (initialTransformation == ActorInitialTransformation.UseLocal){
				InitLocalCoords(actor);
			}
		}

		///Restore undo snapshot
		void IDirectable.Reverse(){

			if (referenceMode == ActorReferenceMode.UseInstanceHideOriginal){
				ReleaseLocalActorInstance();
				return; //if we had a now destroyed instance, no need to restore anything
			}

			Restore();
		}

		///...
		void IDirectable.Update(float time, float previousTime){
			if (OnSectionReached != null){
				for (var i = 0; i < sections.Count; i++){
					if (time >= sections[i].time && previousTime < sections[i].time){
						OnSectionReached(sections[i]);
					}
				}
			}
		}

		///...
		void IDirectable.Exit(){
			if (Application.isPlaying){
				if (referenceMode == ActorReferenceMode.UseInstanceHideOriginal){
					ReleaseLocalActorInstance();
				}
			}
		}

		///...
		void IDirectable.ReverseEnter(){
			if (Application.isPlaying){
				if (referenceMode == ActorReferenceMode.UseInstanceHideOriginal){
					InstantiateLocalActor();
				}
			}
		}
		


#if UNITY_EDITOR
		///Draw the gizmos of virtual actor references
		void IDirectable.DrawGizmos(bool selected){

			if (actor != null && isActive && root.currentTime == 0){
				if (initialTransformation == ActorInitialTransformation.UseOriginal){
					return;
				}

				var t = root.context.transform;
				foreach(var renderer in actor.GetComponentsInChildren<Renderer>()){
					Mesh mesh = null;
					var childPosOffset = renderer.transform.root.InverseTransformPoint(renderer.transform.position);
					var pos = t.TransformPoint(initialLocalPosition + childPosOffset);
					var rot = Quaternion.Euler( (t.eulerAngles + initialLocalRotation) );
					if (renderer is SkinnedMeshRenderer){ mesh = ((SkinnedMeshRenderer)renderer).sharedMesh; }
					else
					{
						var filter = renderer.GetComponent<MeshFilter>();
						if (filter != null){ mesh = filter.sharedMesh; }
					}
					Gizmos.DrawMesh(mesh, pos, rot, renderer.transform.localScale);
				}
			}
		}

		///Just the tools to handle the initial virtual actor reference pos and rot
		void IDirectable.SceneGUI(bool selected){
			
			if (!selected || !isActive){
				return;
			}

			if (initialTransformation == ActorInitialTransformation.UseOriginal){
				return;
			}

			if (actor != null && root.currentTime == 0){
				UnityEditor.EditorGUI.BeginChangeCheck();
				var pos = root.context.transform.TransformPoint(initialLocalPosition);
				pos = UnityEditor.Handles.PositionHandle(pos, Quaternion.identity);
				var rot = UnityEditor.Handles.RotationHandle( Quaternion.Euler(initialLocalRotation), pos ).eulerAngles;
				if (UnityEditor.EditorGUI.EndChangeCheck()){
					UnityEditor.Undo.RecordObject(this, "Local Actor Coordinates");
					initialLocalPosition = root.context.transform.InverseTransformPoint(pos);
					initialLocalRotation = rot;
					UnityEditor.EditorUtility.SetDirty(this);
				}

				UnityEditor.Handles.color = new Color(1,1,1,0.3f);
				UnityEditor.Handles.DrawLine(root.context.transform.position, pos);
				UnityEditor.Handles.color = Color.white;
			}
		}
#endif


		//Store snapshots
		void Store(){
			objectSnapshot = new ObjectSnapshot(actor);
			transformSnapshot = new TransformSnapshot(actor);
		}

		//Restore snapshots
		void Restore(){
			if (objectSnapshot != null){
				objectSnapshot.Restore();
			}
			if (transformSnapshot != null){
				transformSnapshot.Restore();
			}			
		}

		//Initialize actor reference mode
		void InstantiateLocalActor(){
			originalActor = actor;
			actor = (GameObject)Instantiate(actor);
			actor.SetActive(true);
			SceneManager.MoveGameObjectToScene(actor, root.context.scene);
			
			#if UNITY_EDITOR //not really needed, but avoids duplicate instaces when user undo immediately after an initialize.
			UnityEditor.Undo.RegisterCreatedObjectUndo(actor, "Reference Instance");
			#endif

			if (initialTransformation == ActorInitialTransformation.UseLocal){
				InitLocalCoords(actor);
			}

			originalActor.SetActive(false);
		}

		//Release actor reference mode
		void ReleaseLocalActorInstance(){
			if (actor != originalActor){ //just a failsafe
				DestroyImmediate(actor);
				actor = originalActor;
				actor.SetActive(true);
				originalActor.SetActive(true);
				originalActor = null;
			}
		}

		void InitLocalCoords(GameObject target){
			var parentedOffset = target.transform.parent != null? target.transform.localPosition : Vector3.zero;

			target.transform.position = root.context.transform.TransformPoint(initialLocalPosition);
			target.transform.eulerAngles = root.context.transform.eulerAngles + initialLocalRotation;

			target.transform.position += parentedOffset;			
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public T AddTrack<T>(string name = null) where T : CutsceneTrack { return (T)AddTrack(typeof(T), name); }
		public CutsceneTrack AddTrack(System.Type type, string name = null){

			if ( !type.IsSubclassOf(typeof(CutsceneTrack)) || type.IsAbstract ){
				return null;
			}

			var go = new GameObject(type.Name.SplitCamelCase());
			UnityEditor.Undo.RegisterCreatedObjectUndo(go, "New Track");
			var newTrack = UnityEditor.Undo.AddComponent(go, type) as CutsceneTrack;
			UnityEditor.Undo.SetTransformParent(newTrack.transform, this.transform, "New Track");
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "New Track");
			newTrack.transform.localPosition = Vector3.zero;
			if (name != null){ newTrack.name = name; }
			tracks.Add(newTrack);
			newTrack.PostCreate(this);
			root.Validate();
			CutsceneUtility.selectedObject = newTrack;
			return newTrack;
		}

		public void DeleteTrack(CutsceneTrack track){
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Delete Track");
			tracks.Remove(track);
			UnityEditor.Undo.DestroyObjectImmediate(track.gameObject);
			root.Validate();
		}
		
		#endif
	}
}