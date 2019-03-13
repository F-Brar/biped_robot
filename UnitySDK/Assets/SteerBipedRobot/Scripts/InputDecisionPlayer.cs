using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputDecisionPlayer : MonoBehaviour
{
    public float X;
    //public float Y;
    public bool Jump;
    public int Action;
    float[] ActionList;


    float[] DecidePlayer(
    List<float> vectorObs,
    List<Texture2D> visualObs,
    float reward,
    bool done,
    List<float> memory)
    {
        X = Input.GetAxis("Horizontal") * Time.deltaTime;
        //Y = Input.GetAxis("Vertical") * Time.deltaTime;
        Jump = Input.GetButton("Fire1");
        Action = 0;
        if (X > 0f)
            Action = 1;
        else if (X < 0f)
            Action = 2;
        if (Jump)
            Action += 3;

        return new float[1] { Action };
    }


}
