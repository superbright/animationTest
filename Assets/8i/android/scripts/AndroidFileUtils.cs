using UnityEngine;

namespace HVR.Android
{
    public class AndroidFileUtils
    {
        internal static void UnpackAllAssets(string assetDirectory, string outputDirectory) {
            AndroidJavaObject currentActivity = AndroidUtils.GetCurrentActivity();

            AndroidJavaClass fileUtils = new AndroidJavaClass("com.eighti.unity.androidutils.FileUtils");
            fileUtils.CallStatic("unpackAssets", assetDirectory, outputDirectory, currentActivity);
        }

        internal static void Unpack8iAssets(string outputDirectory) {
            UnpackAllAssets("8i", outputDirectory);
        }

		public static void Unpack8iAssets(){
			string outputDirectory = AndroidFileUtils.GetExternalPublicDirectory("8i");
			Unpack8iAssets (outputDirectory);
		}

        public static string GetExternalPublicDirectory(string externalPublicName) {
            AndroidJavaClass environmentClass = new AndroidJavaClass("android.os.Environment");
            AndroidJavaObject externalStorageFile = environmentClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", externalPublicName);
            string externalStoragePath = externalStorageFile.Call<string>("getAbsolutePath");
            return externalStoragePath;
        }

        public static string GetExternalPublicDirectory() {
            return GetExternalPublicDirectory("");
        }

        public static string GetInternalStorageDirectory() {
            AndroidJavaObject currentActivity = AndroidUtils.GetCurrentActivity();
            AndroidJavaObject internalStorageFile = currentActivity.Call<AndroidJavaObject>("getFilesDir");
            string internalStoragePath = internalStorageFile.Call<string>("getAbsolutePath");
            return internalStoragePath;
        }
    }
}