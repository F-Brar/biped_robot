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

    [HideInInspector]
    public LocalCurriculumController curriculumController;  //the curriculum learner


    public override void InitializeAgent()
    {
        //Initialize the curriculum learning
        curriculumController = GetComponent<LocalCurriculumController>();
        base.InitializeAgent();

        //SetupSkill(activeSkill);
    }
    
    /// <summary>
    /// switch active skill; either from controllerAgent or this agent
    /// </summary>
    /// <param name="_activeSkill"></param>
    public void SetupSkill(int _activeSkill)
    {
        if(this.activeSkill != _activeSkill)
        {
            this.activeSkill = _activeSkill;
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
                GiveBrain(_skill.skillBrain);
                _terminationHeight = 1.1f;
                break;
            case Skills.Walk:
                GiveBrain(_skill.skillBrain);
                _terminationHeight = 1f;
                break;
        }
        if (curriculumLearning)
        {
            
            ResetCurriculumRollout();
            //curriculumController.activeSkill = activeSkill;
            curriculumController.SetActiveCurriculum(activeSkill);
        }
        
    }


    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (curriculumLearning)
        {
            _timeAlive += Time.fixedDeltaTime;

            if (_timeAlive >= curriculumController.mileStone)
            {
                
                if (activeSkill == 0)
                {
                    curriculumController.reward = _reward;
                }
                else if (activeSkill == 1)
                {
                    curriculumController.reward = _cumulativeVelocityReward;
                }
                curriculumController.ReachedMileStone();
                _timeAlive = 0;
            }
        }

        base.AgentAction(vectorAction, textAction);
    }

    public override float CalcSkillReward()
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
        float effort = GetEffort();
        _finalPhasePenalty = GetPhaseBonus();
        _effortPenality = 1e-2f * (float)effort;
        _heightPenality = 2 * GetHeightPenalty(1.3f);  //height of body
        __reward = (
            +_uprightBonus
            + _forwardBonus
            - _finalPhasePenalty
            - _velocityPenalty
            - _effortPenality
            - _heightPenality
            );

        return __reward;
        
    }

    float GetWalkerReward()
    {
        float __reward = 0;

        _velocity = GetVelocity();
        _velocity = _velocity / 2;
        //update the curriculum
        if (curriculumLearning)
        {
            _cumulativeVelocityReward += _velocity;
        }

        // Encourage uprightness of hips and body.
        _uprightBonus =
            ((GetUprightBonus(hips) / 6)//6
            + (GetUprightBonus(body) / 6));
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
        _effortPenality = 1e-2f * (float)effort;
        _jointsAtLimitPenality = GetJointsAtLimitPenality();
        _heightPenality = GetHeightPenalty(1.3f);  //height of body

        __reward = (
              _velocity
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

        base.AgentReset();

        PhaseBonusInitalize();

        if (curriculumLearning)
        {
            ResetCurriculumRollout();
        }

        //recentVelocity = new List<float>();
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


