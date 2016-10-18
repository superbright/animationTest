using UnityEngine;
using System.Collections;

namespace HVR
{
    public class PlayHvrActor : MonoBehaviour
    {
        public HvrActor actor;
        public bool loop;
        void Start()
        {
            if (actor != null && actor.GetAsset() != null)
            {
                actor.GetAsset().Play();
                actor.GetAsset().SetLooping(loop);
            }
        }
    }
}
