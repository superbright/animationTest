using HVR;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HvrAsset))]
[CanEditMultipleObjects]
public class Inspector_HvrAsset : Editor
{
    HvrAsset asset;
    static Texture2D icon_pause, icon_play, icon_stop;
    static bool drawRange = false;

    public override void OnInspectorGUI()
    {
        asset = (HvrAsset)target;

        DrawCustomInspector(target);

        Repaint();
    }

    public void DrawCustomInspector(UnityEngine.Object target)
    {
        Undo.RecordObject(target, "HVR Asset Object");

        Object dataObj = asset.data;

        EditorGUI.BeginChangeCheck();
        {
            dataObj = EditorGUILayout.ObjectField("Folder", asset.data, typeof(Object), false);
        }
        if (EditorGUI.EndChangeCheck())
            asset.SetDataFolderObject(dataObj);

        asset.assetScaleFactor = EditorGUILayout.FloatField("Scale Factor", asset.assetScaleFactor);

        if (asset.data == null)
        {
            EditorGUILayout.PrefixLabel("Data not specified");
        }
        else
        {
            Inspector_HvrAsset.DrawInspector_AssetClipState(asset);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(asset);
            SceneView.RepaintAll();
        }
    }

    public static void DrawInspector_AssetClipState(HvrAsset asset)
    {
        if (asset == null)
            return;

        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                DrawInspector_ControlButtons(asset);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            DrawInspector_AssetTime(asset);

            GUILayout.Space(2);
        }
        EditorGUILayout.EndVertical();
    }

    public static void DrawInspector_AssetTime(HvrAsset asset)
    {
        if (!icon_pause)
            icon_pause = (Texture2D)Resources.Load("8iEditor/icon_hvrasset_pause");

        if (!icon_play)
            icon_play = (Texture2D)Resources.Load("8iEditor/icon_hvrasset_play");

        if (!icon_stop)
            icon_stop = (Texture2D)Resources.Load("8iEditor/icon_hvrasset_stop");

        Rect progressRect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(20), GUILayout.MaxHeight(20));
        {
            GUILayout.Space(20);

            Color backgroundcolor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            Color progressColor = new Color(0.36f, 0.129f, 0.407f, 1.0f);
            Color trimColor = new Color(0.36f, 0.129f, 0.407f, 0.2f);

            Handles.BeginGUI();
            {
                Vector3[] points = new Vector3[]
            {
                new Vector3(progressRect.xMin,  progressRect.yMin, 0),
                new Vector3(progressRect.xMax,  progressRect.yMin, 0),
                new Vector3(progressRect.xMax, progressRect.yMax, 0),
                new Vector3(progressRect.xMin, progressRect.yMax, 0)
            };

                Handles.color = backgroundcolor;
                Handles.DrawAAConvexPolygon(points);
            }
            Handles.EndGUI();

            float progressX = Mathf.Lerp(progressRect.xMin, progressRect.xMax, (asset.GetCurrentTime() / asset.GetDuration()));

            // Progress
            Handles.BeginGUI();
            {
                Vector3[] points = new Vector3[]
			    {
				    new Vector3(progressRect.xMin,  progressRect.yMin, 0),
				    new Vector3(progressX,  progressRect.yMin, 0),
				    new Vector3(progressX,  progressRect.yMax, 0),
				    new Vector3(progressRect.xMin,  progressRect.yMax, 0)
			    };

                Handles.color = progressColor;
                Handles.DrawAAConvexPolygon(points);
            }
            Handles.EndGUI();

            //ProgressLine
            Handles.BeginGUI();
            {
                Vector3[] points = new Vector3[]
				{
					new Vector3(progressX, progressRect.yMin, 0),
					new Vector3(progressX, progressRect.yMax, 0)
				};
                Handles.color = Color.white;

                Handles.DrawLine(points[0], points[1]);
            }
            Handles.EndGUI();

            GUIStyle timeStyle = new GUIStyle("label");
            timeStyle.alignment = TextAnchor.MiddleCenter;
            timeStyle.normal.textColor = Color.white;
            EditorGUI.LabelField(progressRect, asset.GetCurrentTime().ToString("f2") + " / " + asset.GetDuration().ToString("f2"), timeStyle);
        }
        EditorGUILayout.EndHorizontal();

        float mouseXPos = Event.current.mousePosition.x;

        if (progressRect.Contains(Event.current.mousePosition))
        {
            Handles.BeginGUI();
            {
                Vector2 startPoint = new Vector2(mouseXPos, progressRect.yMin);
                Vector2 endPoint = new Vector2(mouseXPos, progressRect.yMax);

                Vector2 startTangent = new Vector2(mouseXPos, progressRect.yMax);
                Vector2 endTangent = new Vector2(mouseXPos, progressRect.yMin);
                Handles.DrawBezier(startPoint, endPoint, startTangent, endTangent, Color.white, null, 3);
            }
            Handles.EndGUI();

            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
            {
                float mouseTimeProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, mouseXPos);
                float time = Mathf.Lerp(0, asset.GetDuration(), mouseTimeProgress);
                asset.Seek(time);
            }
        }
    }


    public static void DrawInspector_ControlButtons(HvrAsset asset)
    {
        EditorGUILayout.BeginHorizontal();
        {
            GUILayoutOption[] options = new GUILayoutOption[]{
				GUILayout.MinWidth(20),
				GUILayout.MinHeight(20),
				GUILayout.MaxHeight(20),
				GUILayout.MaxWidth(30),
			};

            Color origColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);

            if (GUILayout.Button(icon_play, options))
                asset.Play();

            if (GUILayout.Button(icon_pause, options))
                asset.Pause();

            if (GUILayout.Button(icon_stop, options))
                asset.Stop();

            GUI.backgroundColor = origColor;
        }
        EditorGUILayout.EndHorizontal();
    }
}
