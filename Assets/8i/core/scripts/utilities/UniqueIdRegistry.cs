using System;
using System.Collections.Generic;

namespace HVR
{
	public static class UniqueIdRegistry
	{
		public static Dictionary<String, int> Mapping = new Dictionary<String, int>();

		public static string GetNewID()
		{
			string id = Guid.NewGuid().ToString();

			// Make sure the id is unique
			while (Contains(id))
				id = Guid.NewGuid().ToString();

			return id;
		}

		public static bool Contains(string id)
		{
			return Mapping.ContainsKey(id);
		}

		public static void Deregister(String id)
		{
			Mapping.Remove(id);
		}

		public static void Register(String id, int instanceID)
		{
			if(!Contains(id))
				Mapping.Add(id, instanceID);
		}

		public static int GetInstanceId(string id)
		{
			return Mapping[id];
		}
	}
}
