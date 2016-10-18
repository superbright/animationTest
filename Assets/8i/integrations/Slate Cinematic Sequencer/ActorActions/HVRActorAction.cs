using UnityEngine;
using System.Collections;
using HVR;

namespace Slate.ActionClips
{
    [Category("HVR")]
    [Name("HVR Actor Clip")]
    [Description("Triggers playback of the HVR Actor")]
    public class HVRActorAction : ActorActionClip
    {
        [SerializeField]
        [HideInInspector]
        private float _length = 1f;

        public HvrAsset hvrAsset;

        public override float length
        {
            get { return _length; }
            set { _length = value; }
        }

        public override bool isValid
        {
            get { return hvrAsset != null; }
        }

        public override string info
        {
            get { return isValid ? hvrAsset.name : base.info; }
        }

        private HvrActor hvrActor
        {
            get { return actor.GetComponent<HvrActor>(); }
        }


        protected override void OnEnter()
        {
            hvrAsset.Seek(0);
            hvrActor.hvrAsset= hvrAsset;
        }

        protected override void OnUpdate(float deltaTime, float previousTime)
        {
            if (deltaTime != previousTime)
            {
                float assetTime = hvrAsset.GetCurrentTime();
                float curTime = deltaTime;
                float timeOffset = 0.2f;

                if (assetTime < curTime - timeOffset || assetTime > curTime + timeOffset)
                {
                    hvrAsset.Seek(deltaTime);
                }

                if (!hvrAsset.IsPlaying())
                {
                    hvrAsset.Play();
                }
            }
            else
            {
                hvrAsset.Seek(deltaTime);
                hvrAsset.Pause();
            }
        }

        protected override void OnExit()
        {
            hvrAsset.Seek(0);
            hvrAsset.Pause();
            hvrActor.hvrAsset = null;
        }

        protected override void OnReverse()
        {
            hvrAsset.Seek(0);
            hvrAsset.Pause();
            hvrActor.hvrAsset = null;
        }
    }
}