using HVR.Core;
using HVR.Interface;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrActor : MonoBehaviour
    {
        HVRActorInterface m_actorInterface;
        HVRActorInterface actorInterface
        {
            get
            {
                if (m_actorInterface == null)
                    m_actorInterface = new HVRActorInterface();

                return m_actorInterface;
            }
        }
            
        public HvrAsset hvrAsset;
        int activeAssetInterfaceHandle;

        public bool debugDrawBounds = false;
        public bool debugDrawOccluder = false;

        public bool useBoxCollider;

        public bool useOcclusionCulling;
        public float occlusionRadiusOffset = 0.5f;
        CullingGroup cullingGroup;
        BoundingSphere[] cullingSpheres = new BoundingSphere[100];
        bool isOcclusionCulled = false;

        void OnEnable()
        {
#if UNITY_EDITOR
            // Attach the HvrActor to the Editor Update Loop
            if (!Application.isPlaying)
                EditorApplication.update += Update;
#endif

            SetVisibility(IsVisible());
        }
        void OnDisable()
        {
#if UNITY_EDITOR
            // Detach the HvrActor from the Editor Update Loop
            if (!Application.isPlaying)
                EditorApplication.update -= Update;
#endif

           SetVisibility(false);
        }

        void Update()
        {
            CheckIfAssetChanged();

            SetTransform(transform, hvrAsset ? hvrAsset.assetScaleFactor : 1.0f);

            SetVisibility(IsVisible());
        }

        void LateUpdate()
        {
            actorInterface.SetTransform(transform, hvrAsset == null ? 1.0f : hvrAsset.assetScaleFactor);
            AttachToActiveScene(); // HACK ABROWN
            UpdateBoxCollider();
            UpdateCullingGroup();
        }

        void OnDestroy()
        {
            DetachFromActiveScene();

            m_actorInterface.Delete();

            if (hvrAsset != null)
                hvrAsset.Delete();

            HVRPlayerInterfaceAPI.Player_GarbageCollect();

            if (cullingGroup != null)
                cullingGroup.Dispose();
        }

        // Called upon Editor Play Mode exit, or upon Standalone application quit
        void OnApplicationQuit()
        {
            if (hvrAsset != null)
                hvrAsset.Stop();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (actorInterface != null)
            {
                Bounds boundsAABB = actorInterface.GetAABB();

                if (boundsAABB.IntersectRay(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition)))
                {
                    //This draws an invisible cube around the bounds at all times, so you can click on it and get it as a selection
                    if (enabled && IsVisible())
                    {
                        Gizmos.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        Gizmos.DrawCube(boundsAABB.center, boundsAABB.size);
                    }
                }

                if (debugDrawBounds)
                {
                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.3f);
                    Gizmos.DrawCube(boundsAABB.center, boundsAABB.size);
                }

                if (debugDrawOccluder)
                {
                    Gizmos.color = new Color(0.0f, 0.8f, 0.0f, 0.5f);
                    Gizmos.DrawSphere(boundsAABB.center, Vector3.Distance(boundsAABB.center, boundsAABB.max) + occlusionRadiusOffset);
                }

                if (Selection.Contains(gameObject))
                {
                    Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.8f);
                    Bounds b = actorInterface.GetBounds();

                    float scaleFactor = 1.0f;

                    if (hvrAsset)
                        scaleFactor = hvrAsset.assetScaleFactor;

                    b.center = b.center * scaleFactor;
                    b.size = b.size * scaleFactor;

                    DrawBounds.Draw(b, transform);
                }
            }
        }
#endif
        // Actor Functions
        //-------------------------------------------------------------------------
        void AttachToActiveScene()
        {
            if (!SceneInterface.Self().hvrSceneInterface.ContainsActor(actorInterface))
                SceneInterface.Self().hvrSceneInterface.AttachActor(actorInterface);
        }
        void DetachFromActiveScene()
        {
            if (SceneInterface.Self().hvrSceneInterface.ContainsActor(actorInterface))
                SceneInterface.Self().hvrSceneInterface.DetachActor(actorInterface);
        }
        public bool IsActorVisible()
        {
            return actorInterface.IsVisible();
        }
        public void SetVisibility(bool visible)
        {
            actorInterface.SetVisible(visible);
        }
        public void SetTransform(Transform trans, float scaleFactor)
        {
            actorInterface.SetTransform(trans, scaleFactor);
        }
        public Bounds GetAABB()
        {
            return actorInterface.GetAABB();
        }
        public Bounds GetBounds()
        {
            return actorInterface.GetBounds();
        }
        public HvrAsset GetAsset()
        {
            return hvrAsset;
        }
        public void SetAsset(HvrAsset asset)
        {
            hvrAsset = asset;
        }

        // Checks
        //-------------------------------------------------------------------------
        public void CheckIfAssetChanged()
        {
            if (hvrAsset != null && hvrAsset.GetAssetInterface() != null)
            {
                // If the HvrAsset Asset object has changed
                if (activeAssetInterfaceHandle != hvrAsset.GetAssetInterface().handle)
                {
                    activeAssetInterfaceHandle = hvrAsset.GetAssetInterface().handle;
                    actorInterface.SetAssetInterface(hvrAsset.GetAssetInterface());
                }
            }
            else
            {
                // If the HvrAsset Asset object has been cleared
                actorInterface.SetAssetInterface(null);
            }
        }
        
        bool IsVisible()
        {
            // Visibility
            if (enabled == false ||
                gameObject.activeInHierarchy == false ||
                (useOcclusionCulling && isOcclusionCulled))
            {
                return false;
            }

            return true;
        }

        void OcclusionStateChangedMethod(CullingGroupEvent evt)
        {
            if (evt.hasBecomeVisible)
                isOcclusionCulled = false;

            if (evt.hasBecomeInvisible)
                isOcclusionCulled = true;
        }

        void UpdateCullingGroup()
        {
            if (useOcclusionCulling)
            {
                if (cullingGroup == null || cullingSpheres == null)
                {
                    cullingGroup = new CullingGroup();

                    cullingSpheres = new BoundingSphere[10];

                    cullingGroup.SetBoundingSpheres(cullingSpheres);
                    cullingGroup.SetBoundingSphereCount(1);

                    cullingSpheres[0] = new BoundingSphere(Vector3.zero, 1f);

                    cullingGroup.onStateChanged = OcclusionStateChangedMethod;
                }

                if (cullingGroup.targetCamera == null && Camera.main != null)
                    cullingGroup.targetCamera = Camera.main;

                Bounds AABB = actorInterface.GetAABB();
                cullingSpheres[0].position = AABB.center;
                cullingSpheres[0].radius = Vector3.Distance(AABB.center, AABB.max) + occlusionRadiusOffset;
            }
            else
            {
                if (cullingGroup != null)
                    cullingGroup.Dispose();

                cullingGroup = null;
                cullingSpheres = null;

                isOcclusionCulled = false;
            }
        }
     
        void UpdateBoxCollider()
        {
            if (useBoxCollider)
            {
                if (GetComponent<BoxCollider>() == null)
                    gameObject.AddComponent<BoxCollider>();

                Bounds b = GetBounds();

                float scaleFactor = 1.0f;

                if (hvrAsset != null)
                    scaleFactor = hvrAsset.assetScaleFactor;

                GetComponent<BoxCollider>().size = b.size * scaleFactor;
                GetComponent<BoxCollider>().center = b.center * scaleFactor;
            }
            else
            {
                if (GetComponent<BoxCollider>() != null)
                {
                    if (Application.isEditor)
                    {
#if UNITY_EDITOR
                        EditorApplication.delayCall += RemoveBoxCollider;
#endif
                    }
                    else
                    {
                        DestroyImmediate(GetComponent<BoxCollider>());
                    }
                }
            }
        }
        void RemoveBoxCollider()
        {
            if (GetComponent<BoxCollider>())
                DestroyImmediate(GetComponent<BoxCollider>());
        }
    }

}
