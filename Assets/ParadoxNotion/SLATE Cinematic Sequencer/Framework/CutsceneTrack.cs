using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	///Tracks are contained within CutsceneGroups and contain ActionsClips within
	abstract public class CutsceneTrack : MonoBehaviour, IDirectable{

		[SerializeField]
		private string _name;
		[SerializeField]
		private Color _color = Color.white;
		[SerializeField] [HideInInspector]
		private bool _active = true;
		[SerializeField] [HideInInspector]
		private List<ActionClip> _actionClips = new List<ActionClip>();

		//the actor to be used in the track taken from it's parent group
		public GameObject actor{
			get {return parent != null? parent.actor : null;}
		}

		//the name...
		new public string name{
			get {return string.IsNullOrEmpty(_name)? GetType().Name.SplitCamelCase() : _name;}
			set
			{
				if (_name != value){
					_name = value;
					base.name = value;
				}
			}
		}

		///Coloring of clips within this track
		public Color color{
			get {return _color.a > 0.1f? _color : Color.white;}
		}

		virtual public string info{
			get {return string.Empty;}
		}

		//all action clips of this track
		public List<ActionClip> actions{
			get {return _actionClips;;}
			set {_actionClips = value;}
		}

		IEnumerable<IDirectable> IDirectable.children{
			get {return actions.Cast<IDirectable>();}
		}

		public int layerOrder{get; set;}

		public IDirector root{ get {return parent != null? parent.root : null;} }
		public IDirectable parent{ get; private set; }

		public bool isCollapsed{
			get {return parent != null? parent.isCollapsed : false;}
		}

		public bool isActive{
			get {return parent != null? parent.isActive && _active : false;}
			set {_active = value;}
		}

		virtual public float startTime{
			get {return parent.startTime;}
			set {}
		}

		virtual public float endTime{
			get {return parent.endTime;}
			set {}
		}

		virtual public float blendIn{
			get {return 0f;}
			set {}
		}

		virtual public float blendOut{
			get {return 0f;}
			set {}
		}

		bool IDirectable.Initialize(){
			//layers are type based
			layerOrder = parent.children.Where( t => t.GetType() == this.GetType() ).Reverse().ToList().IndexOf(this);
			return OnInitialize();
		}

		//when the cutscene starts
		void IDirectable.Enter(){OnEnter();}
		//when the cutscene is updated
		void IDirectable.Update(float time, float previousTime){OnUpdate(time, previousTime);}
		//when the cutscene stops
		void IDirectable.Exit(){OnExit();}
		//when the cutscene enters backwards
		void IDirectable.ReverseEnter(){OnReverseEnter();}
		//when the cutscene is reversed/rewinded
		void IDirectable.Reverse(){OnReverse();}

#if UNITY_EDITOR
		//Gizmos selected
		void IDirectable.DrawGizmos(bool selected){ if (selected) OnDrawGizmosSelected();}
		//Scene GUI stuff
		void IDirectable.SceneGUI(bool selected){ OnSceneGUI();}
#endif

		virtual protected bool OnInitialize(){return true;}
		virtual protected void OnEnter(){}
		virtual protected void OnUpdate(float time, float previousTime){}
		virtual protected void OnExit(){}
		virtual protected void OnReverseEnter(){}
		virtual protected void OnReverse(){}
		virtual protected void OnDrawGizmosSelected(){}
		virtual protected void OnSceneGUI(){}
		virtual protected void OnCreate(){}
		virtual protected void OnAfterValidate(){}

		///After creation
		public void PostCreate(IDirectable parent){
			this.parent = parent;
			OnCreate();
		}

		///Validate the track and it's clips
		public void Validate(IDirector root, IDirectable parent){
			this.parent = parent;
			actions = GetComponents<ActionClip>().OrderBy(a => a.startTime).ToList();
			OnAfterValidate();
		}

		///The weight of the track at time
		public float GetTrackWeight(float time){
			if (time < blendIn){
				return time/blendIn;
			}
			if (time > (endTime - startTime) - blendOut){
				return ((endTime - startTime) - time) / blendOut;
			}
			return 1;
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

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		virtual public float height{
			get {return 30;}
		}

		virtual public Texture icon{
			get {return null;}
		}

		public T AddAction<T>(float time) where T:ActionClip { return (T)AddAction(typeof(T), time); }
		public ActionClip AddAction(System.Type type, float time){

			var catAtt = type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
			if (catAtt != null && actions.Count == 0){
				name = catAtt.category + " Track";
			}

			var newAction = UnityEditor.Undo.AddComponent(gameObject, type) as ActionClip;
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "New Action");
			newAction.startTime = time;
			actions.Add(newAction);
			newAction.PostCreate(this);

			var nextAction = actions.FirstOrDefault(a => a.startTime > newAction.startTime);
			if (nextAction != null){
				newAction.endTime = Mathf.Min(newAction.endTime, nextAction.startTime);
			}

			root.Validate();
			CutsceneUtility.selectedObject = newAction;

			return newAction;
		}

		public void DeleteAction(ActionClip action){
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Remove Action");
			actions.Remove(action);
			UnityEditor.Undo.DestroyObjectImmediate(action);
			root.Validate();
		}
		
		///The Editor GUI in the track info on the left
		virtual public void OnTrackInfoGUI(){

			GUILayout.BeginHorizontal();

			GUI.backgroundColor = UnityEditor.EditorGUIUtility.isProSkin? new Color(0,0,0,0.7f) : new Color(0,0,0,0.2f);
			GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(this.height));
			GUI.backgroundColor = Color.white;
			if (icon != null){
				var temp = GUILayoutUtility.GetLastRect();
				var iconRect = new Rect(0,0,16,16);
				iconRect.center = temp.center - new Vector2(0,2);
				GUI.color = CutsceneUtility.selectedObject == this? Color.white : new Color(1,1,1,0.8f);
				GUI.DrawTexture(iconRect, this.icon);
				GUI.color = Color.white;
			}

			var nameString = string.Format("<size=11>{0}</size>", name);
			var infoString = string.Format("<size=9><color=#707070>{0}</color></size>", info);
			GUILayout.Label( string.Format("{0}\n{1}", nameString, infoString));

			GUILayout.FlexibleSpace();
			GUILayout.Label(isActive? "" : "<b>Disabled</b>", GUILayout.Width(62));

			GUILayout.EndHorizontal();

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}


		///The Editor GUI within the timeline rectangle
		virtual public void OnTrackTimelineGUI(Rect posRect, Rect timeRect, float cursorTime, System.Func<float, float> TimeToPos){
			var e = Event.current;
			if (e.type == EventType.ContextClick && posRect.Contains(e.mousePosition)){

				var attachableTypeInfos = new List<EditorTools.TypeMetaInfo>();
				
				var existing = actions.Count > 0? actions.First() : null;
				var existingCatAtt = existing != null? existing.GetType().GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute : null;
				foreach (var info in EditorTools.GetTypeMetaDerivedFrom(typeof(ActionClip))){

					if (!info.attachableTypes.Contains(this.GetType())){
						continue;
					}

					if (existingCatAtt != null){
						if (existingCatAtt.category == info.category){
							attachableTypeInfos.Add(info);
						}
					} else {
						attachableTypeInfos.Add(info);
					}
				}

				if (attachableTypeInfos.Count > 0){
					var menu = new UnityEditor.GenericMenu();
					foreach (var _info in attachableTypeInfos){
						var info = _info;
						var category = string.IsNullOrEmpty(info.category)? "" : (info.category + "/");
						var tName = info.name;
						menu.AddItem(new GUIContent(category + tName), false, ()=> { AddAction(info.type, cursorTime); } );
					}

					var copyType = CutsceneUtility.GetCopyType();
					if (copyType != null && attachableTypeInfos.Select(i => i.type).Contains(copyType) ){
						menu.AddSeparator("/");
						menu.AddItem(new GUIContent( string.Format("Paste Clip ({0})", copyType.Name) ), false, ()=>{ CutsceneUtility.PasteClip(this, cursorTime); });
					}

					menu.ShowAsContext();
					e.Use();
				}
			}
		}

		#endif
	}
}