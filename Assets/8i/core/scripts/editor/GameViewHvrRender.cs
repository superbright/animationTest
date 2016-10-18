using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using HVR;

namespace HVR.Editor
{
    [InitializeOnLoad]
    public class GameViewHvrRender : MonoBehaviour
    {
        static GameViewHvrRender()
        {
            EditorApplication.update += Update;
        }

        GameViewHvrRender()
        {
            EditorApplication.update -= Update;
        }

        static void Update()
        {
            CheckActors();
        }

        static void CheckActors()
        {
            HvrActor[] actors = GameObject.FindObjectsOfType<HvrActor>();

            bool shouldUpdate = false;

            foreach (HvrActor actor in actors)
            {
                if (actor.GetAsset() != null && actor.GetAsset().HasInternalTimeChanged())
                {
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate && !Application.isPlaying)
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
