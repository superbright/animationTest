using UnityEngine;
using System.Collections;
using HVR;

namespace HVR
{
    [AddComponentMenu("8i/HVR AudioSource Sync")]
    public class HvrAudioSourceSync : MonoBehaviour
    {
        public HvrActor hvrActor;
        public AudioSource audioSource;
        public float offset = 0;

        void Awake()
        {
            audioSource.Stop();
        }

        void Update()
        {
            if (hvrActor == null || hvrActor.GetAsset() == null || audioSource == null || audioSource.clip == null)
                return;

            if (!audioSource.isActiveAndEnabled)
                return;

            if (!hvrActor.GetAsset().IsPlaying())
            {
                audioSource.Stop();
                return;
            }

            float targetTime = hvrActor.GetAsset().GetCurrentTime() - offset;
            float delta = targetTime - audioSource.time;

            if (targetTime > audioSource.clip.length || targetTime < 0)
            {
                audioSource.Stop();
            }
            else
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();

                if (Mathf.Abs(delta) > 0.2f)
                {
                    audioSource.time = targetTime;
                }
                else
                {
                    audioSource.pitch = delta + 1.0f;
                }
            }
        }
    }
}