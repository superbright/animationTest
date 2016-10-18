using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using HVR.Android;

public class LoadingSceneManager : MonoBehaviour {

	void Start () {
		#if UNITY_ANDROID
		AndroidFileUtils.Unpack8iAssets();
		LoadNextScene ();

		#else
		LoadNextScene ();
		#endif
	}

	void LoadNextScene(){
		Scene currentScene = SceneManager.GetActiveScene ();
		int buildIndex = currentScene.buildIndex;

		if (SceneManager.GetSceneAt (buildIndex + 1) != null) {
			SceneManager.LoadScene (buildIndex + 1);
		}
	}
}
