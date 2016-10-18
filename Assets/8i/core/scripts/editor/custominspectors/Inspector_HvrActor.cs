using HVR;
using HVR.Interface;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HvrActor))]
[CanEditMultipleObjects]
public class Inspector_HvrActor : Editor
{
    HvrActor actor;

    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        actor = (HvrActor)target;

        DrawCustomInspector(target);

        Repaint();
    }

    public void DrawCustomInspector(UnityEngine.Object target)
    {
        Undo.RecordObject(target, "HVR Actor");

        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("Options", EditorStyles.boldLabel);

            actor.useBoxCollider = EditorGUILayout.ToggleLeft("Box Collider", actor.useBoxCollider);

            EditorGUILayout.BeginVertical("box");
            {
                actor.useOcclusionCulling = EditorGUILayout.ToggleLeft("Occlusion Checking", actor.useOcclusionCulling);
                EditorGUILayout.BeginHorizontal();
                {
                    if (actor.useOcclusionCulling)
                    {
                        EditorGUILayout.LabelField("Radius Offset");
                        actor.occlusionRadiusOffset = EditorGUILayout.Slider(actor.occlusionRadiusOffset, 0, 2);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("Debug", EditorStyles.boldLabel);

            actor.debugDrawBounds = EditorGUILayout.ToggleLeft("Render Bounds", actor.debugDrawBounds);
            actor.debugDrawOccluder = EditorGUILayout.ToggleLeft("Render Occluder Bounds", actor.debugDrawOccluder);
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("HvrAsset", EditorStyles.boldLabel);

            UnityEngine.Object assetClip = actor.GetAsset();

            EditorGUI.BeginChangeCheck();
            {
                assetClip = EditorGUILayout.ObjectField("Active Asset", assetClip, typeof(HvrAsset), false);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (assetClip == null)
                {
                    actor.SetAsset(null);
                }
                else
                {
                    if (assetClip.GetType() == typeof(HvrAsset))
                    {
                        actor.SetAsset((HvrAsset)assetClip);
                    }
                    else
                    {
                        actor.SetAsset(null);
                    }
                }
            }

            Inspector_HvrAsset.DrawInspector_AssetClipState(actor.GetAsset());
        }
        GUILayout.EndVertical();

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }
}
