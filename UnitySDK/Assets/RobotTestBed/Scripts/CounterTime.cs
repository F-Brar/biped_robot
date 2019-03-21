using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterTime : MonoBehaviour
{
    LocoAcadamy acadamy;
    private void Awake()
    {
        acadamy = GetComponent<LocoAcadamy>();
    }
    public float time;
    public float timeReached;
    private void FixedUpdate()
    {
        time = Time.realtimeSinceStartup;
        if(acadamy.stepCount == 3000)
        {
            timeReached = time;
        }
        

    }
}
