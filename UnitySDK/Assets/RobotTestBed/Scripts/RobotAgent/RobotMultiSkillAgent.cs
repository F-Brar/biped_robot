using System.Collections;
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
    public bool ShowMonitor = true;
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
    }

    public override void CollectObservations()
    {
        AddVectorObs(_timeAliveBonus);
        AddVectorObs(hips.position.z);
        AddVectorObs(_targetVelocityForward);
        AddVectorObs(_currentVelocityForward);
        
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
            //decelerate here
            //yield return new WaitWhile(() => _targetVelocityForward - 0.001f == 0);
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

        _timeAliveBonus = 0;

        switch (_skill.skill){
            case Skills.Stand:
                _targetVelocityForward = 0f;
                recentVelocity = new List<float>();
                GiveBrain(_skill.skillBrain);
                _terminationHeight = .7f;
                _terminationAngle = .25f;
                break;
            case Skills.Walk:
                _targetVelocityForward = .55f;
                recentVelocity = new List<float>();
                GiveBrain(_skill.skillBrain);
                _terminationHeight = .7f;
                _terminationAngle = .25f;
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
        //Monitor.Log()
        //Monitor.Log("reward: ", _reward);
        //Monitor.Log("lesson: ", curriculumController._lesson);
        return _reward;

        
    }

    public float _timeAliveBonus;

    /// <summary>
    /// encourage low velocity, uprightness of all bodyparts; penalize feet movement; penalize falling below height threshold; penalize overall joint effort
    /// </summary>
    /// <returns></returns>
    float GetStandingReward()
    {
        float __reward = 0;
        //penalize forward axis movement
        //_velocityPenalty = 2 * Mathf.Abs(GetVelocity());
        _currentVelocityForward = GetAverageVelocity();
        _velocityReward = Mathf.Abs(1f - Mathf.Abs(_targetVelocityForward - _currentVelocityForward) * 1.3f);
        _velocityPenalty *= 1.5f;
        _uprightBonus =
            ((GetUprightBonus(hips) / 4)//6
            + (GetUprightBonus(body) / 4)
            + (GetUprightBonus(footL) / 6)
            + (GetUprightBonus(footR) / 6));
        _forwardBonus =
            ((GetForwardBonus(hips) / 4)//6
            + (GetForwardBonus(body) / 4)
            + (GetForwardBonus(footL) / 6)
            + (GetForwardBonus(footR) / 6));
        float effort = GetEffort();
        //_finalPhasePenalty = GetPhaseBonus();
        _timeAliveBonus += 0.0001f;
        _effortPenalty = 1e-2f * (float)effort;
        _heightPenalty = GetHeightPenalty(1.3f)/2;  //height of body
        //_deviationPenalty = GetAxisDeviation(hips.position, 0.1f);
        //float leftThighPenalty = Mathf.Abs(GetForwardBonus(thighL));
        //float rightThighPenalty = Mathf.Abs(GetForwardBonus(thighR));
        //_limbPenalty = leftThighPenalty + rightThighPenalty;
        //_limbPenalty = Mathf.Min(0.5f, _limbPenalty);   //penalty for moving both legs in the same direction

        __reward = (
            + _uprightBonus
            + _forwardBonus
            //+ _finalPhasePenalty
            + _timeAliveBonus
            + _velocityReward
            //- _limbPenalty
            - _effortPenalty
            - _heightPenalty
            //- _deviationPenalty
            );
        
        return __reward;
        
    }

    /// <summary>
    /// Calculates the final reward with the following objectives
    ///  Encourage targetvelocity
    ///  Encourage uprightness of hips and body.
    ///  Encourage natural gait and phase
    ///  Encourage staying alive
    ///  penalize synchron leg movement
    ///  penalize high actuation
    ///  penalize joint values at limit
    ///  penalize falling below height treshold
    /// </summary>
    /// <returns></returns>
    float GetWalkerReward()
    {
        float __reward = 0;
        //
        _currentVelocityForward = GetAverageVelocity();
        _velocityReward = 2 * ( 1f - Mathf.Abs(_targetVelocityForward - _currentVelocityForward));
        //
        _uprightBonus =
            ((GetUprightBonus(hips) / 4)//6
            + (GetUprightBonus(body) / 4));
        _forwardBonus =
            ((GetForwardBonus(hips) / 4)//6
            + (GetForwardBonus(body) / 4));
        //
        _finalPhaseBonus = GetPhaseBonus();
        //
        _timeAliveBonus += 0.0001f;
        //
        float leftThighPenalty = Mathf.Abs(GetForwardBonus(thighL));
        float rightThighPenalty = Mathf.Abs(GetBackwardsBonus(thighR));
        _limbPenalty = leftThighPenalty + rightThighPenalty;
        _limbPenalty = Mathf.Min(0.5f, _limbPenalty);   //penalty for moving both legs in the same direction
        //
        float effort = GetEffort(new string[] {shinL.name,shinR.name});
        _effortPenalty = 1e-2f * (float)effort;
        //
        _jointsAtLimitPenalty = GetJointsAtLimitPenalty();
        //
        _heightPenalty = GetHeightPenalty(1.3f);  //height of body
        //_deviationPenalty = GetAxisDeviation(hips.position, 0.1f);

        //Add everything to final stepreward
        __reward = (
             _velocityReward
            + _uprightBonus
            + _forwardBonus
            + _finalPhaseBonus
            + _timeAliveBonus
            //- _deviationPenalty
            - _limbPenalty
            - _effortPenalty
            - _jointsAtLimitPenalty
            - _heightPenalty
            );

        return __reward;
    }


    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void AgentReset()
    {
        inputController.AgentReset();
        _timeAliveBonus = 0;
        base.AgentReset();

        PhaseBonusInitalize();

        if (curriculumLearning)
        {
            ResetCurriculumRollout();
        }

        recentVelocity = new List<float>();
    }

    /// <summary>
    /// Reset the relevant curriculum values for a new rollout
    /// </summary>
    public void ResetCurriculumRollout()
    {
        _timeAlive = 0;
        _cumulativeVelocityReward = 0f;
        curriculumController.ResetRollout();
    }

    /// <summary>
    /// returns the agents active skill
    /// </summary>
    /// <returns></returns>
    public int GetActiveSkill()
    {
        return activeSkill;
    }

}


