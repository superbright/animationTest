using UnityEngine;
using System.Collections;

namespace HVR
{

    // Attach this component to a gameobject that has a box collider set to 'trigger'

    public class TriggerHvrActor : MonoBehaviour
    {
        public HvrActor actor;

        void OnTriggerEnter(Collider other)
        {
            if (actor != null && actor.GetAsset() != null)
                actor.GetAsset().Play();
        }
    }
}
