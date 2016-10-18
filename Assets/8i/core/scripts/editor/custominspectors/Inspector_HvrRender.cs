using HVR.Core;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HVR;
using HVR.Interface;


[CustomEditor(typeof(HvrRender))]
[CanEditMultipleObjects]
public class Inspector_HvrRender : Editor
{
    Camera cam;
    HvrRender hvrRender;

    public float texScrollArea = 0;
    public Vector2 scrollViewPos = Vector2.zero;

    Shader hvrRenderEditorPreviewShader;
    Material hvrRenderEditorPreviewMat;

    bool showDebugTextures = false;

    bool CheckResources()
    {
        if (hvrRenderEditorPreviewShader == null)
            hvrRenderEditorPreviewShader = Resources.Load("8iEditor/shaders/HVRRender_EditorPreview") as Shader;

        if (hvrRenderEditorPreviewShader != null)
        {
            hvrRenderEditorPreviewMat = new Material(hvrRenderEditorPreviewShader);
        }
        else
        {
            return false;
        }

        return true;
    }


    public override void OnInspectorGUI()
    {
        hvrRender = (HvrRender)target;
        cam = hvrRender.GetComponent<Camera>();

        if (hvrRender && CheckResources())
            DrawCustomInspector(target);
    }

    public void DrawCustomInspector(UnityEngine.Object target)
    {
        Undo.RecordObject(target, "HVR Render Object");

        hvrRender.compositeMethod = (HvrRender.CompositeMethods)EditorGUILayout.EnumPopup("Composite Method", hvrRender.compositeMethod);

        if (hvrRender.compositeMethod == HvrRender.CompositeMethods.simple)
        {
            EditorGUILayout.LabelField("The `Simple` method does not take into account depth");
        }

        showDebugTextures = EditorGUILayout.Toggle("Debug", showDebugTextures);

        if (showDebugTextures)
        {
            DrawDebugTextures();
        }
    }

    void DrawDebugTextures()
    {
        scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, GUILayout.MinHeight(240));
        {
            EditorGUILayout.BeginHorizontal();
            {
                {
                    Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(220), GUILayout.Width(210));
                    {
                        EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 15), "Color:");

                        HVRViewportInterface viewport = hvrRender.renderInterface.CurrentViewport();
                        Texture tex = viewport.frameBuffer.renderColourBuffer;
                        if (tex)
                            EditorGUI.DrawPreviewTexture(new Rect(rect.x + 5, rect.y + 15, 200, 200), tex, hvrRenderEditorPreviewMat);

                        GUILayout.Space(200);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                {
                    Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(220), GUILayout.Width(210));
                    {
                        EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 15), "Depth:");

                        HVRViewportInterface viewport = hvrRender.renderInterface.CurrentViewport();
                        Texture tex = viewport.frameBuffer.renderDepthBuffer;
                        if (tex)
                            EditorGUI.DrawPreviewTexture(new Rect(rect.x + 5, rect.y + 15, 200, 200), tex, hvrRenderEditorPreviewMat);

                        GUILayout.Space(200);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
