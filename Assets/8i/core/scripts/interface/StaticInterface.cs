using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HVR.Interface;

namespace HVR.Core
{
	public class StaticInterface
	{
		static private StaticInterface instance;
		static public StaticInterface Self()
		{
			if (instance == null)
				instance = new StaticInterface();

			return instance;
		}

		static public DeferredJobQueue deferredJobQueue = new DeferredJobQueue();
		static public Dictionary<string, PlayerInterface.StatValues> stats = new Dictionary<string, PlayerInterface.StatValues>();

		public StaticInterface()
		{
			Init();
		}

		void Init()
		{
			PlayerInterface.Initialise();
		}

		public void ResetFrameBuffersAndMeshes()
		{
			PlayerInterface.ResetFrameBuffersAndMeshes();
		}

		public void UnityEditorUpdate(float editorTime)
		{
			Update(editorTime);
			LateUpdate();
		}

		public void Update(float time)
		{
			PlayerInterface.UpdateTime(time);

			SceneInterface.Self().Reset();
			stats = PlayerInterface.GetStats();
		}

		public void LateUpdate()
		{
			deferredJobQueue.Update();
		}
	}
}
