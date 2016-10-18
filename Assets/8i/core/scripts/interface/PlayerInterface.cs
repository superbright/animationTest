using UnityEngine;
using System.Text;
using System.Collections.Generic;
using HVR.Interface;

namespace HVR.Core
{
	public class PlayerInterface
	{
		public static bool Initialise()
		{
			if (!SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && !SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11"))
			{
				return false;
			}

            return HVRPlayerInterfaceAPI.Player_Initialise();
		}

		public static void ResetFrameBuffersAndMeshes()
		{
			HVRPlayerInterfaceAPI.Player_ResetFrameBuffersAndMeshes();
		}

		public static void UpdateTime(float time)
		{
			HVRPlayerInterfaceAPI.Player_Update(time);
		}

		public static void PreRender(HVRSceneInterface scene)
		{
			if (scene != null)
			{
				int eventID = HVRPlayerInterfaceAPI.Unity_Player_PrepareRender(scene.handle);
				GL.IssuePluginEvent(HVRPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
			}
		}

		public static void Render(HVRSceneInterface scene, HVRViewportInterface viewport)
		{
			if (scene != null && viewport != null)
			{
                HVRFrameBufferInterface frameBuffer = viewport.frameBuffer;
                if (frameBuffer != null)
                {
                    int clearEventID = HVRPlayerInterfaceAPI.Unity_FrameBuffer_Clear(frameBuffer.handle, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
                    GL.IssuePluginEvent(HVRPlayerInterfaceAPI.UnityRenderEventFunc(), clearEventID);
                }

				int eventID = HVRPlayerInterfaceAPI.Unity_Player_Render(scene.handle, viewport.handle);
				GL.IssuePluginEvent(HVRPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
			}
		}

		public struct StatValues
		{
			public float max, min, avg;
		}

		public static Dictionary<string, StatValues> GetStats()
		{
			Dictionary<string, StatValues> stats = new Dictionary<string, StatValues>();

			int count = HVRPlayerInterfaceAPI.Statistics_GetTrackedValueCount();

			for (int i = 0; i < count; ++i)
			{
				StringBuilder key = new StringBuilder(256);
				StatValues val = new StatValues();

				if (HVRPlayerInterfaceAPI.Statistics_GetTrackedValueName(i, key))
				{
					val.max = HVRPlayerInterfaceAPI.Statistics_GetPerFrame(key.ToString(), HVRPlayerInterfaceAPI.STAT_MAX);
					val.min = HVRPlayerInterfaceAPI.Statistics_GetPerFrame(key.ToString(), HVRPlayerInterfaceAPI.STAT_MIN);
					val.avg = HVRPlayerInterfaceAPI.Statistics_GetPerFrame(key.ToString(), HVRPlayerInterfaceAPI.STAT_AVG);
				}

				if (!stats.ContainsKey(key.ToString()))
				{
					stats.Add(key.ToString(), val);
				}
			}

			return stats;
		}

		public static void LogStats()
		{
			Dictionary<string, StatValues> stats = GetStats();

			Debug.Log("Statistics: " + stats.Keys.Count + "\n");

			int count = 0;

			foreach (KeyValuePair<string, StatValues> pair in stats)
			{
				string log = count + " - ";

				float max = pair.Value.max;
				float min = pair.Value.min;
				float avg = pair.Value.avg;

				log += pair.Key.ToString() + " = " + avg + ", " + min + ", " + max;

				Debug.Log(log + "\n");

				count++;
			}
		}
	}
}
