using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace HVR.Utils
{
    public class FileCopier
    {
        CopyProjectData copyJob;

        public bool stopCopy;

        public bool copyComplete = false;

        public void Start(string[][] fileArray, bool overwriteData)
        {
            if (fileArray.Length != 0)
            {
                copyJob = new CopyProjectData(fileArray, overwriteData, CompleteCallback);
                copyJob.Run();
            }
            else
            {
                CompleteCallback();
            }
        }
        public void Stop()
        {
            stopCopy = true;
            copyJob.Stop();
            copyJob = null;
        }
        void CompleteCallback()
        {
            if (stopCopy)
                return;

            Debug.Log("[HVR] Successfully exported HvrAsset Data");

            copyComplete = true;
        }
        public float GetProgress()
        {
            if (copyJob == null)
                return 0.0f;

            return copyJob.GetProgress();
        }
        public string GetCopyOutput()
        {
            if (copyJob == null)
                return "";

            return copyJob.GetCopyOutput();
        }

        class CopyProjectData
        {
            CopyFilesJob copyJob;

            public CopyProjectData(string[][] fileArray, bool overwriteData, Action completeCallback)
            {
                copyJob = new CopyFilesJob(fileArray, overwriteData, completeCallback);
            }

            public void Run()
            {
                if (copyJob.useThreading)
                {
                    ThreadPool.QueueUserWorkItem((_) =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        try
                        {
                            copyJob.Start();
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    });
                }
                else
                {
                    try
                    {
                        copyJob.Start();
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }
            }

            public void Stop()
            {
                copyJob.Stop();
                copyJob = null;
            }

            public float GetProgress()
            {
                return copyJob.progress;
            }

            public string GetCopyOutput()
            {
                return copyJob.workingOutput;
            }
        }

        class CopyFilesJob
        {
            string[][] filesArray;

            bool overwrite = true;
            public bool useThreading = false;

            bool stop = false;

            public float progress;
            public string workingOutput = "";

            Action completeCallback;

            public CopyFilesJob(string[][] _filesArray, bool _overwrite, Action _completeCallback)
            {
                filesArray = _filesArray;
                overwrite = _overwrite;
                completeCallback = _completeCallback;
            }

            public void Start()
            {
                string src = "";
                string dst = "";

                for (int i = 0; i < filesArray.Length; i++)
                {
                    if (stop)
                        break;

                    src = filesArray[i][0];
                    dst = filesArray[i][1];

                    FileInfo srcFileInfo = new FileInfo(src);

                    FileInfo dstFileInfo = new FileInfo(dst);

                    dstFileInfo.Directory.Create();

                    progress = ((float)i + 1) / (float)filesArray.Length;

                    if (!overwrite)
                    {
                        if (dstFileInfo.Exists)
                        {
                            workingOutput = "Skipping " + src;
                            continue;
                        }
                    }

                    srcFileInfo.CopyTo(dst, overwrite);
                    workingOutput = "Copying " + src + " to " + dst;
                }

                if (completeCallback != null)
                {
                    completeCallback();
                }

                progress = 1.0f;
            }

            public void Stop()
            {
                stop = true;
            }
        }
    }
}
