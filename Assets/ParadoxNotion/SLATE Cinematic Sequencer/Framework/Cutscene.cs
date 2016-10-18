using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	[DisallowMultipleComponent]
	public class Cutscene : MonoBehaviour, IDirector{

		public const float VERSION_NUMBER = 1.5f;

		///What happens when cutscene Stop is called
		public enum StopMode{
			Skip,
			Rewind,
			Hold
		}

		///How the cutscene wraps
		public enum WrapMode{
			Once,
			Loop,
			PingPong
		}

		///Update modes for cutscene
		public enum UpdateMode{
			Normal,
			AnimatePhysics,
			UnscaledTime
		}

		///The direction the cutscene can play
		public enum PlayingDirection{
			Forwards,
			Backwards
		}

		///Raised when any cutscene starts playing.
		public static event System.Action<Cutscene> OnCutsceneStarted;
		///Raised when any cutscene stops playing.
		public static event System.Action<Cutscene> OnCutsceneStopped;

		///Raised when a cutscene section has been reached.
		public event System.Action<Section> OnSectionReached;
		///Raised when a global message has been send by this cutscene.
		public event System.Action<string, object> OnGlobalMessageSend;
		
		//used internaly for a callback provided in Play
		private event System.Action OnStop;

		[SerializeField]
		private UpdateMode _updateMode;
		[SerializeField]
		private StopMode _defaultStopMode;
		[SerializeField]
		private WrapMode _defaultWrapMode;
		[SerializeField]
		private bool _explicitActiveLayers;
		[SerializeField]
		private LayerMask _activeLayers = -1;

		[HideInInspector]
		public List<CutsceneGroup> groups = new List<CutsceneGroup>();

		[SerializeField] [HideInInspector]
		private float _length = 20f;
		[SerializeField] [HideInInspector]
		private float _viewTimeMin = 0f;
		[SerializeField] [HideInInspector]
		private float _viewTimeMax = 21f;

		[System.NonSerialized]
		private float _currentTime;
		[System.NonSerialized]
		private float _playTimeStart;
		[System.NonSerialized]
		private float _playTimeEnd;
		[System.NonSerialized]
		private Transform _groupsRoot;
		[System.NonSerialized]
		private List<IDirectableTimePointer> timePointers;
		[System.NonSerialized]
		private List<IDirectableTimePointer> trackOrderedTimePointers;
		[System.NonSerialized]
		private Dictionary<GameObject, bool> affectedLayerGOStates;
		[System.NonSerialized]
		private static Dictionary<string, Cutscene> allSceneCutscenes = new Dictionary<string, Cutscene>();

		//The root on which groups are added for organization
		public Transform groupsRoot{
			get
			{
				if (_groupsRoot == null){
					_groupsRoot = transform.Find("__GroupsRoot__");
					if (_groupsRoot == null){
						_groupsRoot = new GameObject("__GroupsRoot__").transform;
					}

					#if UNITY_EDITOR
					_groupsRoot.gameObject.hideFlags = Prefs.showTransforms? 0 : HideFlags.HideInHierarchy;
					#endif
				}

				return _groupsRoot;
			}
		}

		///When the cutscene gets updated
		public UpdateMode updateMode{
			get {return _updateMode;}
			set {_updateMode = value;}
		}

		///What will happen when the cutscene is stopped by default
		public StopMode defaultStopMode{
			get {return _defaultStopMode;}
			set {_defaultStopMode = value;}
		}

		///How the cutscene wraps when playing by default
		public WrapMode defaultWrapMode{
			get {return _defaultWrapMode;}
			set {_defaultWrapMode = value;}
		}

		///Will active layers set be used?
		public bool explicitActiveLayers{
			get {return _explicitActiveLayers;}
			set {_explicitActiveLayers = value;}
		}

		///The layers that will be active when the cutscene is active. Everything else is disabled for the duration of the cutscene
		public LayerMask activeLayers{
			get {return _activeLayers;}
			set {_activeLayers = value;}
		}

		///The single Director Group of the cutscene
		public DirectorGroup directorGroup{
			get {return groups.Find( g => g is DirectorGroup) as DirectorGroup;}
		}

		///The single Camera Track of the cutscene
		public CameraTrack cameraTrack{
			get {return directorGroup.tracks.Find( t => t is CameraTrack ) as CameraTrack;}
		}

		///The current sample time
		public float currentTime{
			get{return _currentTime;}
			set{_currentTime = Mathf.Clamp(value, 0, length);}
		}

		///Total length
		public float length{
			get {return _length;}
			set {_length = Mathf.Max(value, 1);}
		}

		//Min view time
		public float viewTimeMin{
			get {return _viewTimeMin;}
			set {if (viewTimeMax > 0) _viewTimeMin = Mathf.Min(value, viewTimeMax - 0.25f);}
		}

		//Max view time
		public float viewTimeMax{
			get {return _viewTimeMax;}
			set {_viewTimeMax = Mathf.Max(value, viewTimeMin + 0.25f, 0 );}
		}

		//The time the WrapMode is taking effect in runtime. Usually equal to 0.
		public float playTimeStart{
			get {return _playTimeStart;}
			set {_playTimeStart = Mathf.Clamp(value, 0, playTimeEnd);}
		}

		//The time the WrapMode is taking effect in runtime. Usually equal to length.
		public float playTimeEnd{
			get {return _playTimeEnd;}
			set {_playTimeEnd = Mathf.Clamp(value, playTimeStart, length);}
		}

		///All directable elements within the cutscene
		public List<IDirectable> directables{get; private set;}

		///Is cutscene playing? (Note: it can be paused and isActive still be true)
		public bool isActive{get; private set;}
		
		///Is cutscene paused?
		public bool isPaused{get; private set;}

		///The direction the cutscene is playing if at all
		public PlayingDirection playingDirection{get; private set;}
		
		///The WrapMode the cutscene is currently using
		public WrapMode playingWrapMode{get; private set;}

		///The last sampled time
		public float previousTime{get; private set;}

		//internal use
		GameObject IDirector.context{get {return this.gameObject;}}

		///The remaining playing time.
		public float remainingTime{
			get
			{
				if (playingDirection == PlayingDirection.Forwards){
					return playTimeEnd - currentTime;
				}
				if (playingDirection == PlayingDirection.Backwards){
					return currentTime - playTimeStart;
				}
				return 0;
			}
		}


		///Get all affected actors within the groups of the cutscene
		public IEnumerable<GameObject> GetAffectedActors(){
			return groups.OfType<ActorGroup>().Select(g => g.actor);
		}

		///Get the key in/out time pointers of clips
		public float[] GetKeyTimes(){
			if (timePointers == null){
				InitializeTimePointers();
			}
			return timePointers.Select(t => t.time).ToArray();
		}

		///Start or resume playing the cutscene at optional start time and optional provided callback for when it stops
		public void Play() { Play(0); }
		public void Play(System.Action callback){ Play(0, callback); }
		public void Play(float startTime){ Play(startTime, length, defaultWrapMode); }
		public void Play(float startTime, System.Action callback){ Play(startTime, length, defaultWrapMode, callback); }
		public void Play(float startTime, float endTime,
			WrapMode wrapMode = WrapMode.Once, System.Action callback = null, PlayingDirection playDirection = PlayingDirection.Forwards
			)
		{

			if (startTime > endTime && playDirection != PlayingDirection.Backwards){
				Debug.LogError("End Time must be greater than Start Time.", gameObject);
				return;
			}

			if (isPaused){ //if it's paused resume.
				Debug.LogWarning("Play called on a Paused cutscene. Cutscene will now resume.", gameObject);
				playingDirection = playDirection;
				Resume();
				return;
			}

			if (isActive){
				Debug.LogWarning("Cutscene already Running.", gameObject);
				return;
			}

			Validate();

			playTimeEnd      = endTime;
			playTimeStart    = startTime;
			currentTime      = startTime;
			playingWrapMode  = wrapMode;
			playingDirection = playDirection;

			if (playDirection == PlayingDirection.Forwards){
				if (currentTime >= playTimeEnd){
					currentTime = playTimeStart;
				}
			}

			if (playDirection == PlayingDirection.Backwards){
				if (currentTime <= playTimeStart){
					currentTime = playTimeEnd;
				}
			}


			isActive = true;
			isPaused = false;
			OnStop   = callback != null? callback : OnStop;

			SendGlobalMessage("OnCutsceneStarted");
			if (OnCutsceneStarted != null){
				OnCutsceneStarted(this);
			}

			StartCoroutine(Internal_UpdateCutscene());
		}


		///Stops the cutscene completely.
		public void Stop(){ Stop(defaultStopMode); }
		public void Stop(StopMode stopMode){
		
			if (!isActive){
				Debug.Log("Called stop on a non-active cutscene", gameObject);
			}

			isActive = false;
			isPaused = false;

			if (stopMode == StopMode.Skip){
				Sample( playingDirection == PlayingDirection.Forwards? length : 0 );
			}

			if (stopMode == StopMode.Rewind){
				Sample( playingDirection == PlayingDirection.Forwards? 0 : length );
			}

			SendGlobalMessage("OnCutsceneStopped");
			if (OnCutsceneStopped != null){
				OnCutsceneStopped(this);
			}

			if (OnStop != null){
				OnStop();
			}
		}


		///Start or resume playing the cutscene at reverse, at optional new start time and optional provided callback for when it stops
		public void PlayReverse(){ PlayReverse(0, length); }
		public void PlayReverse(float startTime, float endTime){ Play(startTime, endTime, WrapMode.Once, null, PlayingDirection.Backwards); }
		///Pause the cutscene
		public void Pause(){ isPaused = true; }
		///Resume if cutscene was active
		public void Resume(){ isPaused = false;	}
		///Rewinds the cutscene to it's initial 0 time state
		public void Rewind(){ if (isActive) Stop(StopMode.Rewind); else Sample(0); }
		///Rewinds the cutscene to it's initial 0 time state without undoing anything, thus keeping current state as finalized.
		public void RewindNoUndo(){
			// TODO: maybe create a new StopMode: RewindNoUndo?
			previousTime = playingDirection == PlayingDirection.Forwards? 0 : length;
			currentTime = playingDirection == PlayingDirection.Forwards? 0 : length;
			if (isActive){
				Stop(StopMode.Rewind);
			} else {
				Sample();
			}
		}

		///Skip the cutscene to the end
		public void SkipAll(){ if (isActive) Stop(StopMode.Skip); else Sample(length); }

		///Skip the cutscene time to the next Section or end time if none.
		public void Skip(){
			var forward = playingDirection == PlayingDirection.Forwards;
			var section = forward? directorGroup.GetSectionAfter(currentTime) : directorGroup.GetSectionBefore(currentTime);
			currentTime = section != null? section.time : (forward? length : 0);
		}


		////Set the cutscene time to a specific section by name
		public bool JumpToSection(string name){ return JumpToSection(GetSectionByName(name)); }
		public bool JumpToSection(Section section){
			if (section == null){
				Debug.LogError("Null Section Provided", gameObject);
				return false;
			}
			currentTime = section.time;
			return true;
		}

		///Start playing from a specific Section
		public bool PlayFromSection(string name){ return PlayFromSection(name, defaultWrapMode); }
		public bool PlayFromSection(string name, WrapMode wrap, System.Action callback = null){
			var section = directorGroup.GetSectionByName(name);
			if (section == null){
				Debug.LogError("Null Section Provided", gameObject);
				return false;
			}
			Play(section.time, length, wrap, callback);
			return true;
		}

		///Play a specific Section only
		public bool PlaySection(string name){ return PlaySection(GetSectionByName(name), defaultWrapMode); }
		public bool PlaySection(string name, WrapMode wrap, System.Action callback = null){ return PlaySection(GetSectionByName(name), wrap, callback); }

		public bool PlaySection(Section section){ return PlaySection(section, defaultWrapMode); }
		public bool PlaySection(Section section, WrapMode wrap, System.Action callback = null){
			if (section == null){
				Debug.LogError("Null Section Provided", gameObject);
				return false;
			}
			var nextSection = directorGroup.GetSectionAfter(section.time);
			Play(section.time, nextSection.time, wrap, callback);
			return true;
		}



		///Sample cutscene state at time specified (currentTime by default)
		///You can call this however and whenever you like without any requirements
		public void Sample(){ Sample(currentTime); }
		public void Sample(float time){

			currentTime = time;

			//ignore same minmax times
			if ( (currentTime == 0 || currentTime == length) && previousTime == currentTime ){
				return;
			}

			if (currentTime > 0 && currentTime < length && (previousTime == 0 || previousTime == length)){
				OnSampleEnable();
			}

			if ((currentTime == 0 || currentTime == length) && previousTime > 0 && previousTime < length){
				OnSampleDisable();
			}

			//initialize time pointers if required
			if (currentTime > 0 && previousTime == 0){
				InitializeTimePointers();
			}

			if (timePointers != null){
				
				//Update timePointers triggering forwards
				if (!Application.isPlaying || currentTime > previousTime){
					for (var i = 0; i < timePointers.Count; i++){
						try {timePointers[i].TriggerForward(currentTime, previousTime);}
						catch (System.Exception e){
							Debug.LogError(string.Format("{0}\n{1}", e.Message, e.StackTrace), gameObject);
							continue; //always continue
						}
					}
				}

				//Update timePointers triggering backwards
				if (!Application.isPlaying || currentTime < previousTime){
					for (var i = timePointers.Count-1; i >= 0; i--){
						try {timePointers[i].TriggerBackward(currentTime, previousTime);}
						catch (System.Exception e){
							Debug.LogError(string.Format("{0}\n{1}", e.Message, e.StackTrace), gameObject);
							continue; //always continue
						}
					}
				}

				//Update timePointers
				if (trackOrderedTimePointers != null){
					for (var i = 0; i < trackOrderedTimePointers.Count; i++){
						try {trackOrderedTimePointers[i].Update(currentTime, previousTime);}
						catch (System.Exception e){
							Debug.LogError(string.Format("{0}\n{1}", e.Message, e.StackTrace), gameObject);
							continue; //always continue
						}
					}
				}
			}

			previousTime = currentTime;
		}


		//initialize the time pointers. Everything is bottom-to-top.
		void InitializeTimePointers(){

			timePointers = new List<IDirectableTimePointer>();
			trackOrderedTimePointers = new List<IDirectableTimePointer>();

			foreach(IDirectable group in groups.AsEnumerable().Reverse()){
				if (group.isActive && group.Initialize()){
					var p1 = new TimeInPointer(group);
					timePointers.Add(p1);

					foreach (IDirectable track in group.children.Reverse()){
						if (track.isActive && track.Initialize()){
							var p2 = new TimeInPointer(track);
							timePointers.Add(p2);

							foreach(IDirectable clip in track.children){
								if (clip.isActive && clip.Initialize()){
									var p3 = new TimeInPointer(clip);
									timePointers.Add(p3);
									trackOrderedTimePointers.Add(p3);

									timePointers.Add(new TimeOutPointer(clip));
								}
							}

							trackOrderedTimePointers.Add(p2);
							timePointers.Add(new TimeOutPointer(track));
						}
					}

					trackOrderedTimePointers.Add(p1);
					timePointers.Add(new TimeOutPointer(group));
				}
			}
			
			timePointers = timePointers.OrderBy(p => p.time).ToList();
		}


		//When Sample begins
		void OnSampleEnable(){
			SetLayersActive();
		}

		//When Sample ends
		void OnSampleDisable(){
			RestoreLayersActive();
		}

		//use of active layers to toggle root object on or off during cutscene
		void SetLayersActive(){
			if (explicitActiveLayers){
				var rootObjects = this.gameObject.scene.GetRootGameObjects();
				affectedLayerGOStates = new Dictionary<GameObject, bool>();
				foreach(var o in rootObjects){
					affectedLayerGOStates[o] = o.activeInHierarchy;
					o.SetActive( (activeLayers.value & (1 << o.layer)) > 0 );
				}
			}
		}

		//restore layer object states.
		void RestoreLayersActive(){
			if (affectedLayerGOStates != null){
				foreach(var pair in affectedLayerGOStates){
					if (pair.Key != null){
						pair.Key.SetActive(pair.Value);
					}
				}
			}
		}


		//internal updater
		IEnumerator Internal_UpdateCutscene(){

			while (isActive){

				while (isPaused){
					if (updateMode == UpdateMode.AnimatePhysics){
						yield return new WaitForFixedUpdate();
					}
					Sample(); //sample current time even while is paused
					yield return null;
				}

				if (!isActive){
					yield break;
				}

				if (updateMode == UpdateMode.AnimatePhysics){
					yield return new WaitForFixedUpdate();
				}

				var delta = Time.deltaTime;
				if (updateMode == UpdateMode.AnimatePhysics){
					delta = Time.fixedDeltaTime;
				}
				if (updateMode == UpdateMode.UnscaledTime){
					delta = Time.unscaledDeltaTime;
				}

				//update time
				currentTime += playingDirection == PlayingDirection.Forwards? delta : -delta;


				if (playingWrapMode == WrapMode.Once){
					if (currentTime >= playTimeEnd && playingDirection == PlayingDirection.Forwards){
						Stop();
						yield break;
					}

					if (currentTime <= playTimeStart && playingDirection == PlayingDirection.Backwards){
						Stop();
						yield break;
					}
				}

				if (playingWrapMode == WrapMode.Loop){
					if (currentTime >= playTimeEnd && playingDirection == PlayingDirection.Forwards){
						currentTime = playTimeStart + delta;
					}
					if (currentTime <= playTimeStart && playingDirection == PlayingDirection.Backwards){
						currentTime = playTimeEnd - delta;
					}
				}

				if (playingWrapMode == WrapMode.PingPong){
					if (currentTime >= playTimeEnd && playingDirection == PlayingDirection.Forwards){
						playingDirection = PlayingDirection.Backwards;
						currentTime -= delta;
					}
					if (currentTime <= playTimeStart && playingDirection == PlayingDirection.Backwards){
						playingDirection = PlayingDirection.Forwards;
						currentTime += delta;
					}
				}

				Sample();

				yield return null;
			}
		}

		///Resamples cutscene. Useful when action settings have been changed
		public void ReSample(){
			if (currentTime > 0 && currentTime < length){
				var time = currentTime;
				Sample(float.Epsilon);

				//very cheap way. Need to find better one!
				for (var i = 0; i < directables.Count; i++){
					var directable = directables[i];
					if (directable is CutsceneTrack && directable.isActive && directable.actor != null){
						directable.Reverse();
						directable.Enter();
					}
				}

				Sample(time);
			}
		}

		protected void OnValidate(){
			if (!Application.isPlaying){
				Validate();
			}
		}

		protected void Awake(){
			allSceneCutscenes[this.name] = this;
			directorGroup.OnSectionReached += (section)=>{ if (this.OnSectionReached != null) OnSectionReached(section); };
			Validate();
		}

		protected void OnDestroy(){
			//if (isActive){ Stop(); }
			allSceneCutscenes.Remove(this.name);
		}

		//recursive validation and gather of directables
		public void Validate(){

			if (groupsRoot.transform.parent != this.transform){	groupsRoot.transform.parent = this.transform; }

			directables = new List<IDirectable>();
			foreach(IDirectable group in groups){
				group.Validate(this, null);
				directables.Add(group);
				foreach(IDirectable track in group.children){
					track.Validate(this, group);
					directables.Add(track);
					foreach(IDirectable clip in track.children){
						clip.Validate(this, track);
						directables.Add(clip);
					}
				}
			}
		}

		///Play a cutscene of specified name that exists either in the Resources folder or in the scene. In that order.
		public static Cutscene Play(string name){ return Play(name, null); }
		public static Cutscene Play(string name, System.Action callback){
			Cutscene cutscene;
			cutscene = FindFromResources(name);
			if (cutscene != null){
				var instance = (Cutscene)Instantiate(cutscene);
				Debug.Log("Instantiating cutscene from Resources");
				instance.Play(()=>
				{
					Destroy(instance.gameObject);
					Debug.Log("Instantiated Cutscene Destroyed");
					if (callback != null){
						callback();
					}
				});
				return cutscene;
			}
			
			cutscene = Find(name);
			if (cutscene != null){
				cutscene.Play(callback);
				return cutscene;
			}

			return null;
		}

		///Find a cutscene from Resources folder
		public static Cutscene FindFromResources(string name){
			var go = Resources.Load(name, typeof(GameObject)) as GameObject;
			if (go != null){
				var cut = go.GetComponent<Cutscene>();
				if (cut != null){
					return cut;
				}
			}
			Debug.LogWarning(string.Format("Cutscene of name '{0}' does not exists in the Resources folder", name));
			return null;
		}

		///Find a cutscene of specified name that exists in the scene
		public static Cutscene Find(string name){
			if (allSceneCutscenes.ContainsKey(name)){
				return allSceneCutscenes[name];
			}
			Debug.LogError(string.Format("Cutscene of name '{0}' does not exists in the scene", name));
			return null;
		}
	
		///Sends a message to all affected gameObject actors (includes Director Camera), as well as the cutscene gameObject itself.
		public void SendGlobalMessage(string message, object value = null){
			this.gameObject.SendMessage(message, SendMessageOptions.DontRequireReceiver);
			foreach(var actor in GetAffectedActors()){
				actor.SendMessage(message, SendMessageOptions.DontRequireReceiver);
			}

			if (OnGlobalMessageSend != null){
				OnGlobalMessageSend(message, value);
			}

			#if UNITY_EDITOR
			Debug.Log(string.Format("<b>({0}) Global Message Send:</b> '{1}' ({2})", name, message, value), gameObject);
			#endif
		}

		///Set the target actor of an Actor Group by the group's name.
		public void SetGroupActorOfName(string groupName, GameObject newActor){
			var group = groups.OfType<ActorGroup>().Where(g => g.name.ToUpper() == groupName.ToUpper()).FirstOrDefault();
			if (group == null){
				Debug.LogError(string.Format("Actor Group with name '{0}' doesn't exist in cutscene", groupName), gameObject);
				return;
			}

			group.actor = newActor;
		}

		//...
		public override string ToString(){
			return string.Format("'{0}' Cutscene\n Time: {1}", name, currentTime);
		}


		///Get a section by name
		public Section GetSectionByName(string name){
			return directorGroup.GetSectionByName(name);
		}

		///Get a section by UID
		public Section GetSectionByUID(string UID){
			return directorGroup.GetSectionByUID(UID);
		}

		///All section names of the DirectorGroup
		public Section[] GetSections(){
			return directorGroup.sections.ToArray();
		}

		///All section names of the DirectorGroup
		public string[] GetSectionNames(){
			return directorGroup.sections.Select(s => s.name).ToArray();
		}

		///Get all names of SendGlobalMessage ActionClips
		public string[] GetDefinedEventNames(){
			var result = new List<string>();
			foreach(var track in directorGroup.tracks.OfType<DirectorActionTrack>()){
				foreach(var clip in track.actions.OfType<Slate.ActionClips.SendGlobalMessage>()){
					result.Add(clip.message);
				}
			}
			return result.ToArray();
		}



		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		[ContextMenu("Reset")] //override
		void Reset(){ ClearAll(); }
		[ContextMenu("Copy Component")] //override
		void CopyComponent(){}
		[ContextMenu("Remove Component")] //override
		void RemoveComponent(){	Debug.LogWarning("Removing the Cutscene Component is not possible. Please delete the GameObject instead");	}
		[ContextMenu("Show Transforms")]
		void ShowTransforms(){ Prefs.showTransforms = true; groupsRoot.hideFlags = 0; }
		[ContextMenu("Hide Transforms")]
		void HideTransforms(){ Prefs.showTransforms = false; groupsRoot.hideFlags = HideFlags.HideInHierarchy; }

		void OnDrawGizmos(){
			var l = Prefs.gizmosLightness;
			Gizmos.color = new Color(l,l,l);
			Gizmos.DrawSphere(transform.position, 0.025f);
			Gizmos.color = Color.white;
			Gizmos.DrawIcon(transform.position, "Cutscene Gizmo");
			for (var i = 0; i < directables.Count; i++){
				var directable = directables[i];
				directable.DrawGizmos( CutsceneUtility.selectedObject == directable );
			}
		}


		public static Cutscene Create(Transform parent = null){
			var cutscene = new GameObject("Cutscene").AddComponent<Cutscene>();
			if (parent != null){
				cutscene.transform.SetParent(parent, false);
			}
			cutscene.transform.localPosition = Vector3.zero;
			cutscene.transform.localRotation = Quaternion.identity;
			return cutscene;
		}

		public T AddGroup<T>(GameObject targetActor = null) where T:CutsceneGroup{ return (T)AddGroup(typeof(T), targetActor); }
		public CutsceneGroup AddGroup(System.Type type, GameObject targetActor = null){
			
			if (!type.IsSubclassOf(typeof(CutsceneGroup)) || type.IsAbstract ){
				return null;
			}

			var newGroup = new GameObject(type.Name).AddComponent(type) as CutsceneGroup;
			newGroup.actor = targetActor;
			newGroup.transform.parent = groupsRoot;
			newGroup.transform.localPosition = Vector3.zero;
			groups.Add(newGroup);
			Validate();
			CutsceneUtility.selectedObject = newGroup;
			return newGroup;
		}

		public void DeleteGroup(CutsceneGroup group){
			
			if (group is DirectorGroup){
				Debug.LogWarning("The Director Group can't be removed from the Cutscene", gameObject);
				return;
			}

			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Delete Group");
			groups.Remove(group);
			Validate();
			UnityEditor.Undo.DestroyObjectImmediate(group.gameObject);
		}


		public void ClearAll(){
			
			if (_groupsRoot != null){
				Sample(0); //rewind first
				UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Clear Cutscene");
				foreach(var group in groups.ToArray()){
					UnityEditor.Undo.DestroyObjectImmediate(group.gameObject);
				}
				groups.Clear();
			}

			var directorGroup = AddGroup<DirectorGroup>();
			directorGroup.AddTrack<CameraTrack>();
			directorGroup.AddTrack<DirectorAudioTrack>();
			CutsceneUtility.selectedObject = null;
			length = 20;
			viewTimeMin = 0;
			viewTimeMax = 21;
		}

		#endif
	}
}