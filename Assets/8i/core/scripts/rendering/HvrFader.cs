using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrFader : MonoBehaviour
    {
        public bool fadeEnabled = false;

        public float fadeValue;
        public float fadeHeightMin;
        public float fadeHeightMax;
        public Color fadeColor;

        void OnDisable()
        {
            SetFaders(false, Color.white, 0, 0, 0);
        }

        void Update()
        {
            SetFaders(fadeEnabled, fadeColor, fadeValue, fadeHeightMin, fadeHeightMax);
        }

        void SetFaders(bool _fadeEnabled, Color _fadeColor, float _fadeValue, float _fadeHeightMin, float _fadeHeightMax)
        {
            List<HvrRender> hvrRenders = GameObject.FindObjectsOfType<HvrRender>().ToList();

#if UNITY_EDITOR
            //Get the scene cameras
            Camera[] sceneCameras = InternalEditorUtility.GetSceneViewCameras();

            foreach (Camera sceneCamera in sceneCameras)
            {
                if (sceneCamera != null && sceneCamera.GetComponent<HvrRender>())
                    hvrRenders.Add(sceneCamera.GetComponent<HvrRender>());
            }
#endif

            foreach (HvrRender render in hvrRenders)
            {
                render.SetFadeSettings(_fadeEnabled ? 1 : 0, _fadeColor, _fadeValue, _fadeHeightMin, _fadeHeightMax);
            }
        }
    }
}


