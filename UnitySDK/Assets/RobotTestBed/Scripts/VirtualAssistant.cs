﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualAssistant : MonoBehaviour
{

    public Rigidbody hips;
    [Tooltip("adjusted through curriculum learning")]
    public float forwardStabilityForce = 100;
    public float sideWaysStabilityForce = 100;



    // Update is called once per frame
    void Update()
    {
        //Get the local hips direction
        var localVel = transform.InverseTransformDirection(hips.velocity);
        if (localVel.z <= -.1f)
        {
            Debug.Log("force forward");
            hips.AddForce(hips.transform.forward * forwardStabilityForce, ForceMode.Force);
        }
        if(localVel.x <= -.1f)
        {
            hips.AddForce(hips.transform.right * sideWaysStabilityForce, ForceMode.Force);
        }
        else if(localVel.x >= .1f)
        {

            hips.AddForce( - hips.transform.right * sideWaysStabilityForce, ForceMode.Force);
        }
    }



}
