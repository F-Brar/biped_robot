﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public enum Skills
{
    Stand,
    Walk
};

[System.Serializable]
public class Skill
{
    
    [Tooltip("the skill to be learned")]
    public Skills skill;
    [Tooltip("the respective brain")]
    public Brain skillBrain;
    [Tooltip("wether this skill is active or not")]
    public bool active;
}

public class RobotMultiSkillAgent : RobotAgent
{

    [Header("Skill Setup Area")]
    [Tooltip("setup wich skills to learn")]
    public List<Skill> skillList;
    [Header("active skill => 0 : stand; 1 : walk;")]
    public int activeSkill;
    

    public bool curriculumLearning {
        get { return _curriculumLearning; }
        set {
            _curriculumLearning = value;
            if (_curriculumLearning == false)
            {
                _timeAlive = 0;
                _cumulativeVelocityReward = 0f;
            }
        }
    }
    [SerializeField]
    private bool _curriculumLearning;
    private ControllerAgent inputController;
    [HideInInspector]
    public LocalCurriculumController curriculumController;  //the curriculum learner


    public override void InitializeAgent()
    {
        inputController = GetComponent<ControllerAgent>();
        //Initialize the curriculum learning
        curriculumController = GetComponent<LocalCurriculumController>();
        base.InitializeAgent();

        //SetupSkill(activeSkill);
    }

    public override void CollectObservations()
    {
        
        //if walking brain
        if (activeSkill == 1)
        {
            AddVectorObs(_targetVelocityForward);
            AddVectorObs(_currentVelocityForward);
        }
        base.CollectObservations();
    }

    /// <summary>
    /// switch active skill; either from controllerAgent or this agent; use reset curriculum to restart curriculum within training
    /// </summary>
    /// <param name="_activeSkill"></param>
    public void SetupSkill(int _activeSkill, bool resetCurriculum)
    {
        if(this.activeSkill != _activeSkill)
        {
            this.activeSkill = _activeSkill;
        }
        else
        {
            return;
        }
        if (resetCurriculum)
        {
            curriculumController.globalCurriculumController.ResetCurriculumLearning(activeSkill);
        }
        //check for convenience
        foreach(var skill in skillList)
        {
            skill.active = false;
        }
        Skill _skill = skillList[_activeSkill];
        _skill.active = true;

        switch (_skill.skill){
            case Skills.Stand:
                _targetVelocityForward = 0f;
                GiveBrain(_skill.skillBrain);
                _terminationHeight = 1f;
                _terminationAngle = .2f;
                break;
            case Skills.Walk:
                _targetVelocityForward = 1.5f;
                GiveBrain(_skill.skillBrain);
                _terminationHeight = 1f;
                _terminationAngle = .2f;
                break;
        }
        if (curriculumLearning)
        {
            ResetCurriculumRollout();
            curriculumController.SetActiveCurriculum(activeSkill);
        }
        
    }


    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // update curriculum
        if (curriculumLearning)
        {
            _timeAlive += Time.fixedDeltaTime;

            if (curriculumController.IsMileStoneReached(_timeAlive))
            {
                curriculumController.reward = GetCumulativeReward();
                curriculumController.ReachedMileStone();
                _timeAlive = 0;
            }
        }
        // action base
        base.AgentAction(vectorAction, textAction);
    }

    /// <summary>
    /// calculates the reward in respect to active skill
    /// </summary>
    /// <returns></returns>
    public override float GetSkillReward()
    {
        if (activeSkill == 0)
        {
            _reward = GetStandingReward();
        }
        else if (activeSkill == 1)
        {
            _reward = GetWalkerReward();
        }
        return _reward;
    }

    /// <summary>
    /// encourage low velocity, uprightness of all bodyparts; penalize feet movement; penalize falling below height threshold; penalize overall joint effort
    /// </summary>
    /// <returns></returns>
    float GetStandingReward()
    {
        float __reward = 0;
        //penalize forward axis movement
        _velocityPenalty = 4 * Mathf.Abs(GetVelocity());
        _uprightBonus =
            ((GetUprightBonus(hips) / 4)//6
            + (GetUprightBonus(body) / 8)
            + (GetUprightBonus(thighL) / 6)
            + (GetUprightBonus(thighR) / 6)
            + (GetUprightBonus(shinL) / 8)
            + (GetUprightBonus(shinR) / 8)
            + (GetUprightBonus(footL) / 6)
            + (GetUprightBonus(footR) / 6));
        _forwardBonus =
            ((GetForwardBonus(hips) / 4)//6
            + (GetForwardBonus(body) / 8)
            + (GetForwardBonus(thighL)/ 6)
            + (GetForwardBonus(thighR)/ 6)
            + (GetForwardBonus(shinL) / 8)
            + (GetForwardBonus(shinR) / 8)
            + (GetForwardBonus(footL) / 6)
            + (GetForwardBonus(footR) / 6));
        //float effort = GetEffort();
        //_finalPhasePenalty = GetPhaseBonus();
        //_effortPenality = 1e-2f * (float)effort;
        _heightPenality = 2 * GetHeightPenalty(1.4f);  //height of body

        __reward = (
            +_uprightBonus
            + _forwardBonus
            //- _finalPhasePenalty
            - _velocityPenalty
            //- _effortPenality
            - _heightPenality
            );

        return __reward;
        
    }

    float GetWalkerReward()
    {
        float __reward = 0;

        //_velocity = GetVelocity();
        //_velocity = _velocity / 2;
        _currentVelocityForward = GetAverageVelocity();
        _velocityReward = 1f - Mathf.Abs(_targetVelocityForward - _currentVelocityForward) * 1.3f;
        
        // Encourage uprightness of hips and body.
        _uprightBonus =
            ((GetUprightBonus(hips) / 4)//6
            + (GetUprightBonus(body) / 4));
        _forwardBonus =
            ((GetForwardBonus(hips) / 6)//6
            + (GetForwardBonus(body) / 6));
        //bonus for async phase:
        _finalPhaseBonus = GetPhaseBonus();

        // penalize synchron leg movement
        float leftThighPenality = Mathf.Abs(GetForwardBonus(thighL));
        float rightThighPenality = Mathf.Abs(GetBackwardsBonus(thighR));
        _limbPenalty = leftThighPenality + rightThighPenality;
        _limbPenalty = Mathf.Min(0.5f, _limbPenalty);   //penalty for moving both legs in the same direction

        float effort = GetEffort(new string[] { shinL.name, shinR.name });
        _effortPenality = 2e-2f * (float)effort;
        _jointsAtLimitPenality = GetJointsAtLimitPenality();
        _heightPenality = 1.2f * GetHeightPenalty(1.3f);  //height of body

        __reward = (
              _velocityReward
            + _uprightBonus
            + _forwardBonus
            + _finalPhaseBonus

            - _limbPenalty
            - _effortPenality
            - _jointsAtLimitPenality
            - _heightPenality
            );

        return __reward;
    }


    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void AgentReset()
    {
        inputController.AgentReset();

        base.AgentReset();

        PhaseBonusInitalize();

        if (curriculumLearning)
        {
            ResetCurriculumRollout();
        }

        recentVelocity = new List<float>();
    }

    public void ResetCurriculumRollout()
    {
        _timeAlive = 0;
        _cumulativeVelocityReward = 0f;
        curriculumController.ResetRollout();
    }

    public int GetActiveSkill()
    {
        return activeSkill;
    }

}


