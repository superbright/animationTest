using UnityEngine;

using HVR.Core;
using HVR;


using UnityEditor;
using UnityEditorInternal;

namespace HVR.Editor
{
    public class MenuItems : MonoBehaviour
    {
        [MenuItem("GameObject/8i/Create HVR Actor", false, 10)]
        static void CreateObject_HVRActor(MenuCommand menuCommand)
        {
            CreateObject<HvrActor>(menuCommand, "HVR Actor");
        }

        [MenuItem("GameObject/8i/Create HVR Color Grade", false, 10)]
        static void CreateObject_HVRColorGrade(MenuCommand menuCommand)
        {
            CreateObject<HvrColorGrading>(menuCommand, "HVR Color Grade");
        }

        [MenuItem("GameObject/8i/Create HVR Fader", false, 10)]
        static void CreateObject_HVRFader(MenuCommand menuCommand)
        {
            CreateObject<HvrFader>(menuCommand, "HVR Fader");
        }

        static void CreateObject<T>(MenuCommand menuCommand, string name)
        {
            // Create a custom game object
            GameObject go = new GameObject(name);
            go.AddComponent(typeof(T));

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }


    }
}

