using UnityEngine;
using System.Collections;

public class LoaderSpin : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 rotation = transform.eulerAngles;
		rotation.z -= Time.deltaTime * 150;
		transform.eulerAngles = rotation;
	}
}
