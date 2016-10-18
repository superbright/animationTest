using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using LitJson;

namespace HVR.Helpers
{
	public static class MD5Helper
	{
		public static string Get(string filename)
		{
			try
			{
				using (var md5 = System.Security.Cryptography.MD5.Create())
				{
					using (var stream = File.OpenRead(filename))
					{
						string hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
						return hash;
					}
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.Log(e);
				return "";
			}
		}
	}

	public static class JSONHelper
	{
		static public JsonData GetJSONNodeFromFile(string filePath)
		{
			try
			{
				StreamReader inp_stm = new StreamReader(filePath);
				string inp_ln = inp_stm.ReadToEnd();

				JsonData meta = JsonMapper.ToObject(inp_ln);

				inp_stm.Close();
				return meta;
			}
			catch
			{
				return null;
			}
		}

		static public bool JsonDataContainsKey(JsonData data, string key)
		{
			bool result = false;
			if (data == null)
				return result;
			if (!data.IsObject)
			{
				return result;
			}
			IDictionary tdictionary = data as IDictionary;
			if (tdictionary == null)
				return result;
			if (tdictionary.Contains(key))
			{
				result = true;
			}
			return result;
		}
	}

	public static class ObjectHelper
	{
		public static Rect ScreenRectFromBounds(GameObject go, bool forceSquare)
		{
			Quaternion quat = go.transform.rotation;

			go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

			Bounds bound = go.GetComponent<Collider>().bounds;
			Vector3 ext = go.GetComponent<Collider>().bounds.extents;

			go.transform.rotation = quat;

			Vector3 boundCenter = go.transform.position + quat * (bound.center - go.transform.position);

			Vector3 topFrontRight = boundCenter + quat * Vector3.Scale(ext, new Vector3(1, 1, 1));
			Vector3 topFrontLeft = boundCenter + quat * Vector3.Scale(ext, new Vector3(-1, 1, 1));
			Vector3 topBackLeft = boundCenter + quat * Vector3.Scale(ext, new Vector3(-1, 1, -1));
			Vector3 topBackRight = boundCenter + quat * Vector3.Scale(ext, new Vector3(1, 1, -1));
			Vector3 bottomFrontRight = boundCenter + quat * Vector3.Scale(ext, new Vector3(1, -1, 1));
			Vector3 bottomFrontLeft = boundCenter + quat * Vector3.Scale(ext, new Vector3(-1, -1, 1));
			Vector3 bottomBackLeft = boundCenter + quat * Vector3.Scale(ext, new Vector3(-1, -1, -1));
			Vector3 bottomBackRight = boundCenter + quat * Vector3.Scale(ext, new Vector3(1, -1, -1));
			Vector3[] corners = new Vector3[] { topFrontRight, topFrontLeft, topBackLeft, topBackRight, bottomFrontRight, bottomFrontLeft, bottomBackLeft, bottomBackRight };

			Vector2 min = new Vector2(99999999999, 99999999999);
			Vector2 max = new Vector2(-99999999999, -99999999999);

			for (int i = 0; i < corners.Length; i++)
			{
				Vector2 pos = WorldToGUIPoint(corners[i]);

				if (pos.x < min.x)
				{
					min.x = pos.x;
				}
				if (pos.y < min.y)
				{
					min.y = pos.y;
				}

				if (pos.x > max.x)
				{
					max.x = pos.x;
				}
				if (pos.y > max.y)
				{
					max.y = pos.y;
				}
			}

			float width = max.x - min.x;
			float height = max.y - min.y;

			Vector2 top = new Vector2();
			top.x = min.x;
			top.y = min.y;

			if (forceSquare)
			{
				if (width > height)
				{
					top.y -= (width - height) / 2;
					height = width;
				}
				else
				{
					top.x -= (height - width) / 2;
					width = height;
				}
			}

			return new Rect(top.x, top.y, width, height);
		}

		public static Vector2 WorldToGUIPoint(Vector3 world)
		{
			Vector2 screenPoint = Camera.main.WorldToScreenPoint(world);
			screenPoint.y = (float)Screen.height - screenPoint.y;
			return screenPoint;
		}

		public static void SetLayerRecursively(GameObject obj, LayerMask newLayer)
		{
			if (null == obj)
			{
				return;
			}

			obj.layer = newLayer;

			foreach (Transform child in obj.transform)
			{
				if (null == child)
				{
					continue;
				}
				SetLayerRecursively(child.gameObject, newLayer);
			}
		}

		public static void ParentUnder(Transform child, Transform parent)
		{
			child.SetParent(parent);
			child.transform.localPosition = Vector3.zero;
			child.transform.localEulerAngles = Vector3.zero;
		}

	}

	public static class ListHelper
	{
		public class NaturalComparer : IComparer<string>
		{
			private readonly CultureInfo _CultureInfo;

			public NaturalComparer(CultureInfo cultureInfo)
			{
				_CultureInfo = cultureInfo;
			}

			public int Compare(string x, string y)
			{
				// simple cases
				if (x == y) // also handles null
					return 0;
				if (x == null)
					return -1;
				if (y == null)
					return +1;

				int ix = 0;
				int iy = 0;
				while (ix < x.Length && iy < y.Length)
				{
					if (Char.IsDigit(x[ix]) && Char.IsDigit(y[iy]))
					{
						// We found numbers, so grab both numbers
						int ix1 = ix++;
						int iy1 = iy++;
						while (ix < x.Length && Char.IsDigit(x[ix]))
							ix++;
						while (iy < y.Length && Char.IsDigit(y[iy]))
							iy++;
						string numberFromX = x.Substring(ix1, ix - ix1);
						string numberFromY = y.Substring(iy1, iy - iy1);

						// Pad them with 0's to have the same length
						int maxLength = Math.Max(
							numberFromX.Length,
							numberFromY.Length);
						numberFromX = numberFromX.PadLeft(maxLength, '0');
						numberFromY = numberFromY.PadLeft(maxLength, '0');

						int comparison = _CultureInfo
							.CompareInfo.Compare(numberFromX, numberFromY);
						if (comparison != 0)
							return comparison;
					}
					else
					{
						int comparison = _CultureInfo
							.CompareInfo.Compare(x, ix, 1, y, iy, 1);
						if (comparison != 0)
							return comparison;
						ix++;
						iy++;
					}
				}

				// we should not be here with no parts left, they're equal
				UnityEngine.Debug.Log(ix < x.Length || iy < y.Length);

				// we still got parts of x left, y comes first
				if (ix < x.Length)
					return +1;

				// we still got parts of y left, x comes first
				return -1;
			}
		}

		public static IEnumerable<string> NaturalSort(
			this IEnumerable<string> collection)
		{
			return NaturalSort(collection, CultureInfo.CurrentCulture);
		}

		public static IEnumerable<string> NaturalSort(
			this IEnumerable<string> collection, CultureInfo cultureInfo)
		{
			return collection.OrderBy(s => s, new NaturalComparer(cultureInfo));
		}


		public static T GetNext<T>(IEnumerable<T> list, T current)
		{
			try
			{
				return list.SkipWhile(x => !x.Equals(current)).Skip(1).First();
			}
			catch
			{
				return default(T);
			}
		}

		public static T GetPrevious<T>(IEnumerable<T> list, T current)
		{
			try
			{
				return list.TakeWhile(x => !x.Equals(current)).Last();
			}
			catch
			{
				return default(T);
			}
		}
	}

	static public class Parser
	{
		static public Matrix4x4 asMatrix4x4(string text)
		{
			Matrix4x4 mat4 = new Matrix4x4();

			try
			{
				string[] outStringSplit = text.Split(","[0]);
				for (int x = 0; x < 4; ++x)
				{
					for (int y = 0; y < 4; ++y)
					{
						int i = y + (x * 4);
						mat4[y, x] = float.Parse(outStringSplit[i]);
					}
				}

			}
			catch
			{
				UnityEngine.Debug.Log("[Parser] Cannot parse (" + text + ") to a matrix4x4");
			}

			return mat4;
		}

		static public Vector3 asVector3(string text)
		{
			Vector3 vec = new Vector3();

			try
			{
				string[] outStringSplit = text.Split(","[0]);
				vec = new Vector3(float.Parse(outStringSplit[0]), float.Parse(outStringSplit[1]), float.Parse(outStringSplit[2]));
			}
			catch
			{
				UnityEngine.Debug.Log("[Parser] Cannot parse (" + text + ") to a vector3");
			}

			return vec;
		}

		static public bool asBool(string text)
		{
			bool check = false;

			if (text == null)
			{
				UnityEngine.Debug.Log("[Parser] Unable to parse text to bool from null string");
				return check;
			}

			text = text.ToLower();

			if (text == "true")
			{
				check = true;
			}
			else if (text == "false")
			{
				check = false;
			}
			else
			{
				check = false;
				UnityEngine.Debug.Log("[Parser] Unable to parse text to bool from string: " + text);
			}

			return check;
		}
	}

	public static class IO
	{
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
		  out ulong lpFreeBytesAvailable,
		  out ulong lpTotalNumberOfBytes,
		  out ulong lpTotalNumberOfFreeBytes);

		public static long GetTotalFreeSpaceOnDisk(string path)
		{
			ulong FreeBytesAvailable;
			ulong TotalNumberOfBytes;
			ulong TotalNumberOfFreeBytes;

			bool success = GetDiskFreeSpaceEx("C:\\", out FreeBytesAvailable, out TotalNumberOfBytes,
							   out TotalNumberOfFreeBytes);

			return (long)TotalNumberOfFreeBytes;
		}

		public static bool IsEnoughFreeSpace(string directory, long datasize)
		{
			//Check space on target hard drive
			long freeDiskSpace = GetTotalFreeSpaceOnDisk(directory);

			if (datasize < freeDiskSpace)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static long GetDirectorySize(string path)
		{
			// 1.
			// Get array of all file names.
			string[] a = Directory.GetFiles(path, "*.*");

			// 2.
			// Calculate total bytes of all files in a loop.
			long b = 0;
			foreach (string name in a)
			{
				// 3.
				// Use FileInfo to get length of each file.
				FileInfo info = new FileInfo(name);
				b += info.Length;
			}
			// 4.
			// Return total size
			return b;
		}

		public static bool IsFileLocked(FileInfo file)
		{
			if (!file.Exists)
			{
				return false;
			}

			FileStream stream = null;

			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			//file is not locked
			return false;
		}

		public static bool IsAbsolutePath(string url)
		{
			Uri result;
			return Uri.TryCreate(url, UriKind.Absolute, out result);
		}


	}
}
