using HVR;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("8i HVR", "Play HVR", CutsceneItemGenre.ActorItem)]
    public class PlayHVREvent : CinemaActorAction
    {
        private GameObject _Actor;

        private HvrActor m_hvrActor;

        public float startTime = 0;

        public HvrActor hvrActor
        {
            get
            {
                if (_Actor != null && m_hvrActor == null)
                {
                    if (_Actor.GetComponent<HvrActor>())
                        m_hvrActor = _Actor.GetComponent<HvrActor>();
                }

                return m_hvrActor;
            }
            set
            {
                m_hvrActor = value;
            }
        }

        /// <summary>
        /// Called when the running time of the cutscene hits the firetime of the action
        /// </summary>
        public override void Trigger(GameObject Actor)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset != null)
                return;

            hvrActor.hvrAsset.Seek(startTime);
            hvrActor.hvrAsset.Play();
        }

        /// <summary>
        /// Called at each update when the action is to be played.
        /// </summary>
        public override void UpdateTime(GameObject Actor, float time, float deltaTime)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.GetAsset() == null)
                return;

            // Check within the range of the Actor's duration time
            if (time < 0 && time > duration)
                return;

            float targetTime = time;
            float delta = targetTime - hvrActor.GetAsset().GetCurrentTime();

            if (targetTime >= 0 && targetTime < hvrActor.GetAsset().GetDuration())
            {
                if (Cutscene.State == CinemaDirector.Cutscene.CutsceneState.Playing || Cutscene.State == CinemaDirector.Cutscene.CutsceneState.PreviewPlaying)
                {
                    if (!hvrActor.GetAsset().IsPlaying())
                    {
                        hvrActor.GetAsset().Play();
                    }
                }

                if (Mathf.Abs(delta) > 0.2f)
                {
                    hvrActor.GetAsset().Seek(targetTime);
                }
            }
            else
            {
                hvrActor.GetAsset().Stop();
            }
        }

        /// <summary>
        /// Called when the running time of the cutscene exceeds the duration of the action
        /// </summary>
        public override void End(GameObject Actor)
        {
            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

            hvrActor.hvrAsset.Pause();
        }

        /// <summary>
        /// Called when the cutscene exists preview/play mode. Return properties to pre-cached state if necessary.
        /// </summary>
        public override void Stop(GameObject Actor)
        {
            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

            hvrActor.hvrAsset.Seek(0);
            hvrActor.hvrAsset.Pause();
        }

        /// <summary>
        /// Called when the cutscene time is set/skipped manually.
        /// </summary>
        /// <param name="Actor">The actor to target for this event.</param>
        /// <param name="time">The new running time.</param>
        /// <param name="deltaTime">The deltaTime since the last update call.</param>
        public override void SetTime(GameObject Actor, float time, float deltaTime)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

            if (time >= 0 && time < duration)
            {
                hvrActor.hvrAsset.Seek(startTime + time);
                hvrActor.hvrAsset.Pause();
            }
        }

        /// <summary>
        /// Reverse trigger. Called when scrubbing backwards.
        /// </summary>
        public override void ReverseTrigger(GameObject Actor)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

             hvrActor.hvrAsset.Pause();
        }

        /// <summary>
        /// Reverse End. Called when scrubbing backwards.
        /// </summary>
        public override void ReverseEnd(GameObject Actor)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

             hvrActor.hvrAsset.Pause();
        }


        /// <summary>
        /// Pause any action as necessary
        /// </summary>
        public override void Pause(GameObject Actor)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

            hvrActor.hvrAsset.Pause();
        }

        /// <summary>
        /// Resume from paused.
        /// </summary>
        public override void Resume(GameObject Actor)
        {
            _Actor = Actor;

            if (hvrActor == null || hvrActor.hvrAsset == null)
                return;

            hvrActor.hvrAsset.Play();
        }
    }
}