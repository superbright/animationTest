using UnityEngine;
using System.Collections;

namespace Slate{

	[System.Serializable]
	///Defines a section...
	public class Section{

		[SerializeField]
		private string _UID;
		[SerializeField]
		private string _name;
		[SerializeField]
		private float _time;
		[SerializeField]
		private Color _color = new Color(0,0,0,0.4f);

		//Unique ID.
		public string UID{
			get {return _UID;}
			private set {_UID = value;}
		}

		///The name of the section.
		public string name{
			get {return _name;}
			set {_name = value;}
		}

		///It's time.
		public float time{
			get {return _time;}
			set {_time = value;}
		}

		///Preferrence color.
		public Color color{
			get {return _color.a > 0.1f? _color : new Color(0,0,0,0.4f);}
			set {_color = value;}
		}

		public Section(string name, float time){
			this.name = name;
			this.time = time;
			UID = System.Guid.NewGuid().ToString();
		}

		public override string ToString(){
			return name;
		}
	}
}