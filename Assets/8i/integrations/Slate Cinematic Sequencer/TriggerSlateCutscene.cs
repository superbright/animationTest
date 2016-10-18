using UnityEngine;
using System.Collections;
using Slate;
public class TriggerSlateCutscene : MonoBehaviour
{
    public Cutscene targetCutscene;
    
    void Start()
    {
        if(targetCutscene)
        {
            targetCutscene.Play();
        }
    }
}
