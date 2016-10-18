///Adapted from:
///http://wiki.unity3d.com/index.php/EditorAnimationCurveExtension

using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
 
namespace Slate{

	public enum TangentMode
	{
		Editable = 0,
		Smooth = 1,
		Linear = 2,
		Constant = Linear | Smooth,
	}
 
	public enum TangentDirection
	{
		Left,
		Right
	}
 
 	public static class CurveExtension {
 
		public static void UpdateTangentsFromMode(this AnimationCurve curve){
			for (int i = 0; i < curve.keys.Length; i++) {
				curve.UpdateTangentsFromMode(i);
			}
		}
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static void UpdateTangentsFromMode(this AnimationCurve curve, int index)
		{
			if (index < 0 || index >= curve.length){
				return;
			}

			Keyframe key = curve[index];

			if (KeyframeUtility.GetKeyTangentMode(key, 0) == TangentMode.Linear && index >= 1)
			{
				key.inTangent = CalculateLinearTangent(curve, index, index - 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtility.GetKeyTangentMode(key, 1) == TangentMode.Linear && index + 1 < curve.length)
			{
				key.outTangent = CalculateLinearTangent(curve, index, index + 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtility.GetKeyTangentMode(key, 0) == TangentMode.Smooth || KeyframeUtility.GetKeyTangentMode(key, 1) == TangentMode.Smooth){
				curve.SmoothTangents(index, 0.0f);
				if (index == 0 || index + 1 == curve.keys.Length){
					key.inTangent = 0;
					key.outTangent = 0;
					curve.MoveKey(index, key);
				}
			}
		}
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		private static float CalculateLinearTangent(AnimationCurve curve, int index, int toIndex){
			return (float) (((double) curve[index].value - (double) curve[toIndex].value) / ((double) curve[index].time - (double) curve[toIndex].time));
		}
 
	}

 
	public class KeyframeUtility {
 
	 	public static Keyframe GetNewModeFromExistingKey(Keyframe key, TangentMode mode){
			if (mode == TangentMode.Editable){
				if (GetKeyTangentMode(key, 0) == TangentMode.Smooth && GetKeyTangentMode(key, 1) == TangentMode.Smooth){
					key = SetKeyTangentMode(key, 0, mode);
					key = SetKeyTangentMode(key, 1, mode);
					return key;
				}
			}
			
			return GetNew(key.time, key.value, mode);
	 	}

		public static Keyframe GetNew( float time, float value, TangentMode leftAndRight){
			return GetNew(time, value, leftAndRight, leftAndRight);
		}
 
		public static Keyframe GetNew(float time, float value, TangentMode left, TangentMode right){

			var keyframe = new Keyframe(time, value);
 
			keyframe = SetKeyBroken(keyframe, true);
			keyframe = SetKeyTangentMode(keyframe, 0, left);
			keyframe = SetKeyTangentMode(keyframe, 1, right);
 
			if (left == TangentMode.Constant ){
				keyframe.inTangent = float.PositiveInfinity;
			}
			if (right == TangentMode.Constant ){
				keyframe.outTangent = float.PositiveInfinity;
			}
 
			return keyframe;
		}
 
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static Keyframe SetKeyTangentMode(Keyframe keyframe, int leftRight, TangentMode mode)
		{

			int tangentMode = keyframe.tangentMode;
 
			if (leftRight == 0)
			{
				tangentMode &= -7;
				tangentMode |= (int) mode << 1;
			}
			else
			{
				tangentMode &= -25;
				tangentMode |= (int) mode << 3;
			}
 
 			keyframe.tangentMode = tangentMode;
 			keyframe = SetKeyBroken(keyframe, false);
			if (GetKeyTangentMode(tangentMode, leftRight) != mode){
				Debug.LogError("Bug occured. This should not happen, but it did."); 
			}

			return keyframe;
		}
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static TangentMode GetKeyTangentMode(int tangentMode, int leftRight)
		{
			if (leftRight == 0){
				return (TangentMode) ((tangentMode & 6) >> 1);
			} else {
				return (TangentMode) ((tangentMode & 24) >> 3);
			}
		}
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static TangentMode GetKeyTangentMode(Keyframe keyframe, int leftRight)
		{
			int tangentMode = keyframe.tangentMode;
			if (leftRight == 0){
				return (TangentMode) ((tangentMode & 6) >> 1);
			} else {
				return (TangentMode) ((tangentMode & 24) >> 3);
			}
		}
 
 
		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static Keyframe SetKeyBroken(Keyframe keyframe, bool broken)
		{
			int tangentMode =  keyframe.tangentMode;
 
			if (broken){
				tangentMode |= 1;
			} else {
				tangentMode &= -2;
			}
			keyframe.tangentMode = tangentMode;
			return keyframe;
		}

		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static bool GetIsKeyBroken(Keyframe keyframe)
		{
			int tangentMode = keyframe.tangentMode;
			tangentMode |= 1;
 			return keyframe.tangentMode == tangentMode;
		}
	}
}