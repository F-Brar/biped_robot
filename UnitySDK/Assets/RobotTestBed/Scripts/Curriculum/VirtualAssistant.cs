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
    public Rigidbody hips;
    [Tooltip("adjusted through curriculum learning")]
    public float propellingForce;       //propels forward velocity
    public float lateralBalanceForce;   //lateral balance
    public float breakForce;            //breaks forward velocity

    private void Awake()
    {
        hips = transform.Find("hips").gameObject.GetComponent<Rigidbody>();
    }

    // fixedUpdate is called every physics step
    void FixedUpdate()
    {
        var localVel = transform.InverseTransformDirection(hips.velocity);
        if (localVel.z <= -.1f)
        {
            hips.AddForce(hips.transform.forward * propellingForce, ForceMode.Acceleration);
        }
        //frontal stability at 70% for standing agent
        else if(localVel.z >= .1f)
        {
            hips.AddForce(-hips.transform.forward * breakForce * .7f, ForceMode.Acceleration);
        }


        if(localVel.x <= -.0f)
        {
            hips.AddForce(hips.transform.right * lateralBalanceForce, ForceMode.Acceleration);
        }
        else if(localVel.x >= .0f)
        {

            hips.AddForce( - hips.transform.right * lateralBalanceForce, ForceMode.Acceleration);
        }
        
    }
}
