using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Slate{

	///Able to create a snapshot of an object by recoding and restoring it's properties
	///Providing a GameObject in the constructor, records a snapshot of it and all of it's components
	public class ObjectSnapshot{

/*
		#if UNITY_EDITOR
		private Dictionary<Object, string> serialized;
		#endif

		///Create new ObjectSnapShot storing target object
		public ObjectSnapshot(Object target, bool fullObjectHierarchy = false){
			Store(target, fullObjectHierarchy);
		}

		public void Store(Object target, bool fullObjectHierarchy = false){
			
			#if UNITY_EDITOR

			if (!Application.isPlaying){
				serialized = new Dictionary<Object, string>();
				serialized[target] = UnityEditor.EditorJsonUtility.ToJson(target);
				if (target is GameObject){
					var go = (GameObject)target;
					var components = fullObjectHierarchy? go.GetComponentsInChildren<Component>(true) : go.GetComponents<Component>();
					foreach(var component in components){
						serialized[component] = UnityEditor.EditorJsonUtility.ToJson(component);
					}
				}
			}

			#endif
		}

		public void Restore(){
			
			#if UNITY_EDITOR

			if (!Application.isPlaying){
				foreach(var pair in serialized){
					if (pair.Key != null){
						UnityEditor.EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
					}
				}
			}

			#endif
		}
*/


		private List<Record> records;
		///Create new ObjectSnapShot storing target object
		public ObjectSnapshot(Object target, bool fullObjectHierarchy = false){
			Store(target, fullObjectHierarchy);
		}

		struct Record{
			public Object target;
			public PropertyInfo prop;
			public FieldInfo field;
			public object value;
			public Record(Object target, PropertyInfo property, object value){
				this.target = target;
				this.prop   = property;
				this.field = null;
				this.value  = value;
			}
			public Record(Object target, FieldInfo field, object value){
				this.target = target;
				this.prop = null;
				this.field   = field;
				this.value  = value;
			}
		}

		private static readonly System.Type[] recordableTypes = new System.Type[]{
			typeof(float),
			typeof(int),
			typeof(bool),
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4),
			typeof(Quaternion),
			typeof(Color),
			typeof(Object)
		};

		private static readonly System.Type[] excludedObjectTypes = new System.Type[]{
			typeof(Camera),
			typeof(Animator),
			typeof(Animation),
			typeof(MeshFilter),
			typeof(MeshRenderer),
			typeof(Material),
			typeof(Rigidbody)
		};

		///Store a snapshot
		public void Store(Object target, bool fullObjectHierarchy = false){
			
			if (target == null){
				return;
			}

			records = new List<Record>();
			var objects = new List<Object>();
			objects.Add(target);
			if (target is GameObject){
				var go = (GameObject)target;
				var components = fullObjectHierarchy? go.GetComponentsInChildren<Component>(true) : go.GetComponents<Component>();
				objects.AddRange( components.Where( c => c != null && !excludedObjectTypes.Contains(c.GetType()) ).Cast<Object>() );
			}

			foreach(var o in objects){

				foreach(var prop in o.GetType().RTGetProperties()){
					
					if (!prop.CanRead || !prop.CanWrite){
						continue;
					}

					var getter = prop.RTGetGetMethod();
					if (getter == null){
						continue;
					}

					if (getter.IsStatic || !getter.IsPublic){
						continue;
					}

					if (prop.RTGetAttribute<System.ObsoleteAttribute>(true) != null){
						continue;
					}

					if (excludedObjectTypes.Contains(prop.PropertyType)){
						continue;
					}

					if (!recordableTypes.Any(t => t.RTIsAssignableFrom(prop.PropertyType))){
						continue;
					}

					try {records.Add( new Record( o, prop, getter.Invoke(o, null) ) ); }
					catch { Debug.Log(prop.Name); continue; }
				}

				foreach(var field in o.GetType().RTGetFields()){
					if (field.IsStatic || !field.IsPublic){
						continue;
					}
					records.Add(new Record( o, field, field.GetValue(o) ));
				}
			}
		}

		///Restore the snapshot
		public void Restore(){

			foreach (var record in records){
				try
				{
					if (record.prop != null){
						record.prop.RTGetSetMethod().Invoke(record.target, new object[]{record.value} );
					}
					if (record.field != null){
						record.field.SetValue(record.target, record.value);
					}
				}
				catch { continue; }
			}
		}

	}
}