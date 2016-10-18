using UnityEngine;
using System.Collections;

namespace Slate.ActionClips{

	[Category("Events")]
	[Description("Send a Unity Message to the actor")]
	public class SendMessage : ActorActionClip {

		public string message;

		public override string info{
			get {return "Message\n" + message;}
		}

		public override bool isValid{
			get {return !string.IsNullOrEmpty(message);}
		}

		protected override void OnEnter(){

			if (!Application.isPlaying)
				return;

			Debug.Log(string.Format("<b>Actor Message Send:</b> {0}", message));
			actor.SendMessage(message);
		}
	}
}