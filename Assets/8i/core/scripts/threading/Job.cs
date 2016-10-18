using UnityEngine;
using System;

public class Job
{
    public enum JobState
    {
        Ready,
        Active,
        Complete,
        Error
    }

    JobState state;
    public JobState State { get { return state; } }

    public virtual bool OnStart() { return true; }
    public virtual bool OnRun() { return true; }
    public virtual bool OnComplete() { return true; }
    public virtual void OnError() { }

    public void Run()
    {
        if (state != JobState.Ready)
            return;
        state = JobState.Active;

        bool ok = OnStart();
        if (ok) ok = OnRun();
        if (ok) ok = OnComplete();
        if (!ok)
        {
            state = JobState.Error;
            OnError();
            return;
        }
        state = JobState.Complete;
    }
}