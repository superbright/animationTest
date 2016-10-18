using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using HVR.Android;
using HVR.Core;
using HVR.Interface;

namespace HVR
{
    public class UniqueIdentifierAttribute : PropertyAttribute { }

    [ExecuteInEditMode]
    public class HvrAsset : ScriptableObject
    {
        HVRAssetInterface m_assetInterface;
        HVRAssetInterface assetInterface
        {
            get
            {
                if (m_assetInterface == null)
                    SetAssetFiles(GetDataLocation());

                return m_assetInterface;
            }
            set
            {
                m_assetInterface = value;
            }
        }

        enum eState
        {
            eState_None = 0,
            eState_Initialising = 1 << 0,
            eState_Playing = 1 << 1,
            eState_Seeking = 1 << 2,
            eState_Count
        };

        public enum eRenderMethod
        {
            eRenderMethod_Point,
            eRenderMethod_PointBlend
        };
        eRenderMethod renderMethod;

        public UnityEngine.Object data;

#if UNITY_EDITOR
        string lastPathForDataInEditor;
#endif

        [UniqueIdentifier]
        public string uniqueID = "";

        public float assetScaleFactor = 0.01f;

        public float lastCurrentTime = 0;

        public delegate void OnHvrPlay();
        public OnHvrPlay onHvrPlay;
        public delegate void OnHvrSeek(float time);
        public OnHvrSeek onHvrSeek;
        public delegate void OnHvrPause();
        public OnHvrPause onHvrPause;
        public delegate void OnHvrStop();
        public OnHvrStop onHvrStop;

        public HvrAsset()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                renderMethod = eRenderMethod.eRenderMethod_PointBlend;

            if (Application.platform == RuntimePlatform.IPhonePlayer)
                renderMethod = eRenderMethod.eRenderMethod_Point;

            if (Application.platform == RuntimePlatform.Android)
                renderMethod = eRenderMethod.eRenderMethod_Point;
        }

        // OnEnable is called upon ScritableObject creation
        void OnEnable()
        {
#if UNITY_EDITOR
            if (String.IsNullOrEmpty(uniqueID))
                uniqueID = UniqueIdRegistry.GetNewID();

            if (UniqueIdRegistry.Contains(uniqueID))
            {
                if (UniqueIdRegistry.GetInstanceId(uniqueID) != GetInstanceID())
                    uniqueID = UniqueIdRegistry.GetNewID();
            }

            UniqueIdRegistry.Register(uniqueID, GetInstanceID());

            // Attach the Update to the Editor Update Loop
            if (!Application.isPlaying)
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            }
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            // Detach the Update from the Editor Update Loop
            if (!Application.isPlaying)
                EditorApplication.update -= Update;
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            if (data != null)
            {
                if (AssetDatabase.GetAssetPath(data.GetInstanceID()) != lastPathForDataInEditor)
                {
                    lastPathForDataInEditor = AssetDatabase.GetAssetPath(data.GetInstanceID());
                    SetAssetFiles(GetDataLocation());
                }
            }
#endif
        }

        public void SetDataFolderObject(UnityEngine.Object dataFolderObject)
        {
            data = dataFolderObject;

            SetAssetFiles(GetDataLocation());
        }

        string GetDataLocation()
        {
            string dataFolder = "";

            if (Application.isEditor)
            {
#if UNITY_EDITOR
                if (data != null)
                {
                    string projectAssetPath = Application.dataPath;
                    projectAssetPath = projectAssetPath.Substring(0, projectAssetPath.Length - 6);

                    dataFolder = projectAssetPath + AssetDatabase.GetAssetPath(data);
                }
#endif
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                string buildDataFolder = AndroidFileUtils.GetExternalPublicDirectory("8i/" + uniqueID);

                if (Directory.Exists(buildDataFolder))
                    dataFolder = buildDataFolder;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                string buildDataFolder = Application.dataPath + "/8i/" + uniqueID + "/";

                if (Directory.Exists(buildDataFolder) || File.Exists(buildDataFolder))
                {
                    dataFolder = buildDataFolder;
                }
            }
            else
            {
                string buildDataFolder = Application.dataPath + "/../8i/" + uniqueID;
                dataFolder = buildDataFolder;
            }

            return dataFolder;
        }

        public void SetAssetFiles(string files)
        {
            assetInterface = new HVRAssetInterface(files);
            SetRenderMethod(renderMethod);
        }

        public HVRAssetInterface GetAssetInterface()
        {
            return assetInterface;
        }

        public void Delete()
        {
            if (assetInterface != null)
            {
                assetInterface.Delete();
                assetInterface = null;
            }

            HVRPlayerInterfaceAPI.Player_GarbageCollect();
        }
        public void Play()
        {
            if (assetInterface != null)
            {
                assetInterface.Play();

                if (onHvrPlay != null)
                    onHvrPlay();
            }
        }
        public void Pause()
        {
            if (assetInterface != null)
            {
                assetInterface.Pause();

                if (onHvrPause != null)
                    onHvrPause();
            }
        }
        public void Stop()
        {
            if (assetInterface != null)
            {
                assetInterface.Seek(0);
                assetInterface.Pause();

                if (onHvrStop != null)
                    onHvrStop();
            }
        }
        public void Seek(float seconds)
        {
            if (assetInterface != null)
            {
                if (seconds <= GetDuration())
                    assetInterface.Seek(seconds);

                if (onHvrSeek != null)
                    onHvrSeek(seconds);
            }
        }
        public void Step(int frames)
        {
            if (assetInterface != null)
                assetInterface.Step(frames);
        }
        public void SetLooping(bool looping)
        {
            if (assetInterface != null)
                assetInterface.SetLooping(looping);
        }
        public bool IsPlaying()
        {
            eState state = eState.eState_None;
            if (assetInterface != null)
                state = (eState)assetInterface.GetState();

            return (state & eState.eState_Playing) != 0;
        }
        public int GetState()
        {
            if (assetInterface != null)
                return assetInterface.GetState();

            return 0;
        }
        public float GetCurrentTime()
        {
            if (assetInterface != null)
                return assetInterface.GetCurrentTime();

            return 0;
        }
        public float GetDuration()
        {
            if (assetInterface != null)
                return assetInterface.GetDuration();

            return 0;
        }
        public void SetRenderMethod(eRenderMethod rrenderMethod)
        {
            renderMethod = rrenderMethod;
            if (assetInterface != null)
                assetInterface.SetRenderMethodType((int)rrenderMethod);
        }
        public eRenderMethod GetRenderMethod()
        {
            return renderMethod;
        }
        public bool HasInternalTimeChanged()
        {
            if (lastCurrentTime != GetCurrentTime())
            {
                lastCurrentTime = GetCurrentTime();
                return true;
            }

            return false;
        }
    }
}
