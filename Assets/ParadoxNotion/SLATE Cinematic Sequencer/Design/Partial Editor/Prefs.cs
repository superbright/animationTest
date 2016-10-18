﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Slate{

	///SLATE editor preferences
	public static class Prefs {

		[System.Serializable]
		class SerializedData{
			public bool showTransforms                 = false;
			public bool compactMode                    = false;
			public TimeStepMode timeStepMode           = TimeStepMode.Seconds;
			public float snapInterval                  = 0.1f;
			public int frameRate                       = 30;
			public bool autoFirstKey                   = false;
			public bool doPairedKeying                 = false;
			public bool showDopesheetKeyValues         = true;
			public TangentMode defaultTangentMode      = TangentMode.Editable;
			public KeyframesStyle keyframesStyle       = KeyframesStyle.PerTangentMode; 
			public bool showShotThumbnails             = true;
			public int thumbnailsRefreshInterval       = 30;
			public bool showRuleOfThirds               = true;
			public bool scrollWheelZooms               = true;
			public bool showDescriptions               = true;
			public float gizmosLightness               = 0f;
			public Color trajectoryColor               = Color.black;
			public Prefs.RenderSettings renderSettings = new Prefs.RenderSettings();
		}

		[System.Serializable]
		public enum KeyframesStyle{
			PerTangentMode,
			AlwaysDiamond
		}

		[System.Serializable]
		public enum TimeStepMode{
			Seconds,
			Frames
		}

		[System.Serializable]
		public class RenderSettings{

#if !NO_UTJ
			public enum OutputType{
				PNGImageSequence,
				EXRImageSequence,
				MP4Video,
				GIFAnimation
			}

			public OutputType outputType    = OutputType.PNGImageSequence;
			public DataPath.Root rootFolder = DataPath.Root.ProjectDirectory;
			public string subFolder         = "SlateRenders";
			public string fileName          = "MyRender";
			public int resolutionWidth      = 640;
			public int framerate            = 30;
			public bool splitRenderBuffer   = false;
			public int videoBitrate         = 8192000;
#endif

		}

		private static SerializedData _data;
		private static SerializedData data{
			get
			{
				if (_data == null){
					_data = JsonUtility.FromJson<SerializedData>( EditorPrefs.GetString("Slate.EditorPreferences") );
					if (_data == null){
						_data = new SerializedData();
					}
				}
				return _data;
			}
		}

		public static float[] snapIntervals = new float[]{ 0.001f, 0.01f, 0.1f };
		public static int[] frameRates = new int[]{ 30, 60 };

		public static bool showTransforms{
			get {return data.showTransforms;}
			set {if (data.showTransforms != value){ data.showTransforms = value; Save(); } }
		}

		public static bool compactMode{
			get {return data.compactMode;}
			set {if (data.compactMode != value){ data.compactMode = value; Save(); } }
		}

		public static float gizmosLightness{
			get {return data.gizmosLightness;}
			set {if (data.gizmosLightness != value){ data.gizmosLightness = value; Save(); } }
		}

		public static Color gizmosColor{
			get {return new Color(data.gizmosLightness, data.gizmosLightness, data.gizmosLightness);}
		}

		public static bool showShotThumbnails{
			get {return data.showShotThumbnails;}
			set {if (data.showShotThumbnails != value){ data.showShotThumbnails = value; Save(); } }
		}

		public static bool showDopesheetKeyValues{
			get {return data.showDopesheetKeyValues;}
			set {if (data.showDopesheetKeyValues != value){ data.showDopesheetKeyValues = value; Save(); } }
		}

		public static KeyframesStyle keyframesStyle{
			get {return data.keyframesStyle;}
			set {if (data.keyframesStyle != value){ data.keyframesStyle = value; Save(); } }
		}

		public static bool scrollWheelZooms{
			get {return data.scrollWheelZooms;}
			set {if (data.scrollWheelZooms != value){ data.scrollWheelZooms = value; Save(); } }
		}

		public static bool showDescriptions{
			get {return data.showDescriptions;}
			set {if (data.showDescriptions != value){ data.showDescriptions = value; Save(); } }
		}

		public static Color trajectoryColor{
			get {return data.trajectoryColor;}
			set {if (data.trajectoryColor != value){ data.trajectoryColor = value; Save(); } }
		}

		public static int thumbnailsRefreshInterval{
			get {return data.thumbnailsRefreshInterval;}
			set {if (data.thumbnailsRefreshInterval != value){ data.thumbnailsRefreshInterval = value; Save(); } }
		}

		public static bool autoFirstKey{
			get {return data.autoFirstKey;}
			set {if (data.autoFirstKey != value){ data.autoFirstKey = value; Save(); } }
		}

		public static bool doPairedKeying{
			get {return data.doPairedKeying;}
			set {if (data.doPairedKeying != value){ data.doPairedKeying = value; Save(); } }			
		}

		public static TangentMode defaultTangentMode{
			get {return data.defaultTangentMode;}
			set {if (data.defaultTangentMode != value){ data.defaultTangentMode = value; Save(); } }			
		}

		public static bool showRuleOfThirds{
			get {return data.showRuleOfThirds;}
			set {if (data.showRuleOfThirds != value){ data.showRuleOfThirds = value; Save(); } }			
		}

		public static Prefs.RenderSettings renderSettings{
			get {return data.renderSettings;}
			set {data.renderSettings = value; Save(); }			
		}


		public static TimeStepMode timeStepMode{
			get {return data.timeStepMode;}
			set
			{
				if (data.timeStepMode != value){
					data.timeStepMode = value;
					frameRate = value == TimeStepMode.Frames? 30 : 10;
					Save();
				}
			}
		}

		public static int frameRate{
			get {return data.frameRate;}
			set {if (data.frameRate != value){ data.frameRate = value; snapInterval = 1f/value; Save(); } }
		}

		public static float snapInterval{
			get {return Mathf.Max(data.snapInterval, 0.001f);}
			set	{if (data.snapInterval != value){ data.snapInterval = Mathf.Max(value, 0.001f); Save();	} }
		}

		static void Save(){
			EditorPrefs.SetString("Slate.EditorPreferences", JsonUtility.ToJson(data));
		}
	}
}

#endif