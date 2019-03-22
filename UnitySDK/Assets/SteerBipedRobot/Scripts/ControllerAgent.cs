using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MLAgents;

/// <summary>
/// The controller either controlled by internal logic (heuristic) or player input
/// </summary>
public class ControllerAgent : Agent
{
    [Header("either heuristic logic input or Player input")]
    public Brain playerBrain;
    public Brain heuristicBrain;

    public GameObject academy;

    [Tooltip("if checked the player controls the agent")]
    public bool playerControl;
    public int Action;
    //public float targetVelocity;
    private List<RobotMultiSkillAgent> agents;
    //private RobotMultiSkillAgent agent;
    private int lastAction;

    public override void InitializeAgent()
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("agent"))
        {
            agents.Add( obj.GetComponent<RobotMultiSkillAgent>() );
        }
        base.InitializeAgent();
        if (playerControl)
        {
            GiveBrain(playerBrain);
        }
        else
        {
            GiveBrain(heuristicBrain);
        }
    }
    
    public override void AgentReset()
    {
        Action = 0;
        //targetVelocity = 0f;
    }
    
    public override void CollectObservations()
    {
        AddVectorObs(Action);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Action = (int)vectorAction[0];

        /*
        switch (Action)
        {
            case 0:
                targetVelocity = 0f;
                break;
            case 1:
                targetVelocity = 1f;
                break;

            default:
                throw new NotImplementedException();
        }*/
        if(Action != lastAction)
        {
            ApplyActionToAgents(Action);
            /*
            if (agent.curriculumLearning)
            {
                currController.SetActiveCurriculum(Action);
            }*/
        }
        lastAction = Action;
    }

    void ApplyActionToAgents(int Action)
    {
        foreach(RobotMultiSkillAgent agent in agents)
        {
            agent.SetupSkill(Action);
        }
    }
}
