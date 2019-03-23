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

    private LocoAcadamy academy;
    
    
    public bool useMultiSkill;

    public int stepsToTrainWalk;
    public int stepsToTrainStand;

    private bool resetCurriculumLearning;

    [Tooltip("if checked the player controls the agent")]
    public bool playerControl;
    public int Action;

    private int _stepCountTillSkill;
    private RobotMultiSkillAgent agent;
    private int lastAction;
    private int lastActionFailed;

    public override void InitializeAgent()
    {
        academy = GameObject.FindGameObjectWithTag("academy").GetComponent<LocoAcadamy>();
        /*
        agents = new List<RobotMultiSkillAgent>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("agent"))
        {
            agents.Add( obj.GetComponent<RobotMultiSkillAgent>() );
        }*/
        agent = GetComponent<RobotMultiSkillAgent>();
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
        lastActionFailed = 1;

        if (Action == 0)
        {
            lastActionFailed = 0;
            Action = 1;
        }
        
        //targetVelocity = 0f;
    }
    
    public override void CollectObservations()
    {
        AddVectorObs(lastActionFailed);
        //AddVectorObs(Action);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {

        

        if (useMultiSkill)
        {
            Action = (int)vectorAction[0];

        }
        else
        {
            _stepCountTillSkill = academy.stepCount;
            //train walkSkill for as long as required
            if (_stepCountTillSkill <= stepsToTrainWalk)
            {
                Action = 1;
            }
            //train standSkill for as long as required
            else if(_stepCountTillSkill <= (stepsToTrainWalk + stepsToTrainStand))
            {
                Action = 0;
            }
            //train both together for the rest of training + reset curriculum learning
            else if(_stepCountTillSkill >= (stepsToTrainWalk + stepsToTrainStand))
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
        //foreach(RobotMultiSkillAgent agent in agents)
        //{
        agent.SetupSkill(Action, resetCurriculumLearning);
            
        if(resetCurriculumLearning == true)
        {
            resetCurriculumLearning = false;
        }

        //}
    }


}
