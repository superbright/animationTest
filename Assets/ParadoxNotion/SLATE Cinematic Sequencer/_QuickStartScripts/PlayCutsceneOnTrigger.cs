using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Slate{

	[AddComponentMenu("SLATE/Play Cutscene On Trigger")]
	public class PlayCutsceneOnTrigger : MonoBehaviour {

		public Cutscene cutscene;
		public bool checkSpecificTagOnly = true;
		public string tagName = "Player";
		public bool once;
		public UnityEvent onFinish;

		void OnTriggerEnter(Collider other){

			if (cutscene == null){
				Debug.LogError("Cutscene is not provided", gameObject);
				return;
			}

			if (checkSpecificTagOnly && !string.IsNullOrEmpty(tagName)){
				if (other.gameObject.tag == tagName){
					cutscene.Play( ()=>{onFinish.Invoke();} );
				}
			} else {
				cutscene.Play( ()=>{onFinish.Invoke();} );
			}
			
			if (once){
				Destroy(this.gameObject);
			}
		}

		void Reset(){
			var collider = GetComponent<Collider>();
			if (collider == null){
				collider = gameObject.AddComponent<BoxCollider>();
			}
			collider.isTrigger = true;
		}

		public static GameObject Create(){
			return new GameObject("Cutscene Trigger").AddComponent<PlayCutsceneOnTrigger>().gameObject;
		}
	}
}