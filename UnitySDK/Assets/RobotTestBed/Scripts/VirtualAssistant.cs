using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// The virtual assistant is updated at each learning iteration such that
/// it provides assistive forces appropriate to the current skill level of
/// the learner.
/// </summary>
public class VirtualAssistant : MonoBehaviour
{
    [Tooltip("if this is active the agent will be stabilized on all sides")]
    public bool standing;
    public Rigidbody applyPowerBody;
    public Rigidbody hips;
    [Tooltip("adjusted through curriculum learning")]
    public float forwardStabilityForce;
    public float sideWaysStabilityForce;//lateral balance



    // Update is called once per frame
    void FixedUpdate()
    {
        var localVel = transform.InverseTransformDirection(hips.velocity);
        if (localVel.z <= -.0f)
        {
            hips.AddForce(hips.transform.forward * forwardStabilityForce, ForceMode.Acceleration);
        }
        //frontal stability at 70% for standing agent
        else if(localVel.z >= .0f && standing)
        {
            hips.AddForce(-hips.transform.forward * forwardStabilityForce * .7f, ForceMode.Acceleration);
        }


        if(localVel.x <= -.0f)
        {
            hips.AddForce(hips.transform.right * sideWaysStabilityForce, ForceMode.Acceleration);
        }
        else if(localVel.x >= .0f)
        {

            hips.AddForce( - hips.transform.right * sideWaysStabilityForce, ForceMode.Acceleration);
        }
        
    }
}
