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
    public Dictionary <int, Skill> skillDict;
    public int activeSkill;
    public GameObject CameraTarget;

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

    private LocalRobotCurricula curricula;  //the curriculum learner
    private VirtualAssistant assistant;


    public override void InitializeAgent()
    {
        activeSkill = 0;
        skillDict = new Dictionary<int, Skill>();
        for(int i = 0; i < skillList.Count; i++)
        {
            skillDict[i] = skillList[i];
        }

        //Initialize the curriculum learning
        curricula = GetComponent<LocalRobotCurricula>();

        //setup camera target
        if (CameraTarget != null)
        {
            var smoothFollow = CameraTarget.GetComponent<SmoothFollow>();
            if (smoothFollow != null)
                smoothFollow.target = hips.transform;
        }

        base.InitializeAgent();
    }
    

    public void SetupSkill(int _activeSkill)
    {
        foreach(var skill in skillDict)
        {
            skill.Value.active = false;
        }
        Skill _skill = skillDict[_activeSkill];
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
    }


    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (curriculumLearning)
        {
            _timeAlive += Time.deltaTime;

            if (_timeAlive >= curricula.mileStone)
            {
                //testing purpose: for standing
                if (standSkill)
                {
                    curricula.reward = _reward;
                }
                else if (walkSkill)
                {
                    curricula.reward = _cumulativeVelocityReward;
                }
                curricula.ReachedMileStone();
                _timeAlive = 0;
            }
        }

        base.AgentAction(vectorAction, textAction);
    }

    public override float CalcSkillReward()
    {
        if (standSkill)
        {
            _reward = GetStandingReward();
        }
        else if (walkSkill)
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
            + (GetUprightBonus(body) / 6)
            + (GetUprightBonus(thighL) / 8)
            + (GetUprightBonus(thighR) / 8)
            + (GetUprightBonus(shinL) / 8)
            + (GetUprightBonus(shinR) / 8)
            + (GetUprightBonus(footL) / 6)
            + (GetUprightBonus(footR) / 6));
        _forwardBonus =
            ((GetForwardBonus(hips) / 4)//6
            + (GetForwardBonus(body) / 6)
            + (GetForwardBonus(thighL)/8)
            + (GetForwardBonus(thighR)/8)
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
            _timeAlive = 0;
            _cumulativeVelocityReward = 0f;
            curricula.ResetRollout();
        }

        //recentVelocity = new List<float>();
    }


}


