using UnityEngine;

namespace HVR.Android
{
    public class AndroidUtils
    {
        internal static AndroidJavaObject GetCurrentActivity()
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            return currentActivity;
        }
    }
}

