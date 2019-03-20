using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MLAgents;

public class ControllerAgent : Agent
{
    public Brain standingBrain;
    public Brain walkingBrain;

    public float AxisX;

    public override void AgentReset()
    {
        AxisX = 0f;
    }

    public override void CollectObservations()
    {
        AddVectorObs(AxisX);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        int walkAction = (int)vectorAction[0];
        switch (walkAction)
        {
            case 0:
                
                AxisX = 0f;
                break;
            case 1:
                AxisX = 1f;
                break;

            default:
                throw new NotImplementedException();
        }

    }
}
