#if !NO_UTJ

using UnityEngine;
using System.Collections;
using UTJ;

namespace Slate{

	public class AlembicStreamRoot : AlembicStream {

		public void Sample(float time){

			if ( !AbcIsValid() ){
				Debug.LogError(string.Format("Alembic {0} is invalid", gameObject.name), gameObject);
				return;
			}

			AbcUpdateBegin(time);
		}

		public void Validate(){
			foreach(var abcCam in GetComponentsInChildren<AlembicCamera>(true)){
				var shot = abcCam.GetComponent<ShotCamera>();
				if (shot == null){
					shot = abcCam.gameObject.AddComponent<ShotCamera>();
				}
			}			
		}

		public void Initialize(){
			base.Refresh();
		}
	}
}

#endif