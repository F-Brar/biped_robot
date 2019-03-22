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

    public LocoAcadamy academy;
    public int stepCount;
    public bool useMultiSkill;

    public int stepsToTrainWalk;
    public int stepsToTrainStand;

    private bool resetCurriculumLearning;

    [Tooltip("if checked the player controls the agent")]
    public bool playerControl;
    public int Action;
    private List<RobotMultiSkillAgent> agents;
    private int lastAction;

    public override void InitializeAgent()
    {
        agents = new List<RobotMultiSkillAgent>();
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

        

        if (useMultiSkill)
        {
            Action = (int)vectorAction[0];

        }
        else
        {
            stepCount = academy.stepCount;
            //train walkSkill for as long as required
            if (stepCount <= stepsToTrainWalk)
            {
                Action = 1;
            }
            //train standSkill for as long as required
            else if(stepCount <= (stepsToTrainWalk + stepsToTrainStand))
            {
                Action = 0;
            }
            //train both together for the rest of training + reset curriculum learning
            else if(stepCount >= (stepsToTrainWalk + stepsToTrainStand))
            {
                if (useMultiSkill == false)
                {
                    resetCurriculumLearning = true;
                }
                useMultiSkill = true;
            }
        }
        if(Action != lastAction)
        {
            ApplyActionToAgents(Action);

        }
        lastAction = Action;
    }

    void ApplyActionToAgents(int Action)
    {
        foreach(RobotMultiSkillAgent agent in agents)
        {
            agent.SetupSkill(Action, resetCurriculumLearning);
            
            if(resetCurriculumLearning == true)
            {
                resetCurriculumLearning = false;
            }

        }
    }
}
