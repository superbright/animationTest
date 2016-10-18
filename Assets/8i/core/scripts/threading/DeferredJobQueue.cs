using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class DeferredJobQueue
{
    public static DeferredJobQueue instance;
    public DeferredJobQueue()
    {
        instance = this;
    }

    Mutex queueLock = new Mutex();
    public List<Job> jobQueue = new List<Job>();

    public void AddJob(Job job)
    {
        queueLock.WaitOne();
        jobQueue.Add(job);
        queueLock.ReleaseMutex();
    }

    public void Update()
    {
        // Copy list to temp, clear old
        queueLock.WaitOne();
        List<Job> processQueue = jobQueue;
        jobQueue = new List<Job>();
        queueLock.ReleaseMutex();

        // Process Jobs in order
        foreach (Job job in processQueue)
        {
            job.Run();
        }
    }
}

