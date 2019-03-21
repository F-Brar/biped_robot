using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// iterate the curriulum if the policy reaches 70% of its predecessor and stays 2p seconds alive
/// </summary>
[System.Serializable]
public class LocalCurriculumController : MonoBehaviour
{
    public List<Curriculum> curriculumSkills;
    public int activeSkill;
    [HideInInspector]
    public GlobalCurriculumController globalCurricula;
    [HideInInspector]
    public VirtualAssistant assistant;
    [HideInInspector]
    public RobotMultiSkillAgent agent;

    #region AssistantForces
    //internal forces updatet in each rollout
    public float initPropForce;
    public float initLatForce;
    public float initBreakForce;

    public float propellingForce {
        get { return _propellingForce; }
        set {
            //update assistant
            _propellingForce = value;
            if (assistant == null)
            {
                assistant = GetComponent<VirtualAssistant>();
            }
            assistant.propellingForce = _propellingForce;
        }
    }

    private float _propellingForce;

    public float lateralBalanceForce {
        get { return _lateralBalanceForce; }
        set {
            //update assistant
            _lateralBalanceForce = value;
            assistant.lateralBalanceForce = _lateralBalanceForce;
        }
    }
    private float _lateralBalanceForce;


    public float breakForce {
        get { return _breakForce; }
        set {
            //update assistant
            _breakForce = value;
            assistant.breakForce = _breakForce;
        }
    }
    private float _breakForce;
    #endregion

    public float reward;
    //reduction speed of forces
    public float reductionPercentage;
    //time alive milestone
    public float mileStone;
    
    public int _lesson = 0;
    public int milestoneCounter = 0;
    [Range(0,1)]
    public float multiplier;

    private void Awake()
    {
        curriculumSkills = new List<Curriculum>();
        if (assistant == null)
        {
            assistant = GetComponent<VirtualAssistant>();
        }
        if (agent == null)
        {
            agent = GetComponent<RobotMultiSkillAgent>();
            agent.curriculumController = this;
        }
    }

    /// <summary>
    /// initialize local curricula
    /// </summary>
    public void Init(List<Curriculum> _curriculumSkills)
    {
        //initialize local curriculum list
        foreach(var curriculum in _curriculumSkills)
        {
            this.curriculumSkills.Add(curriculum);
        }
        activeSkill = agent.GetActiveSkill();
        //initialize with globale values
        UpdateCurriculumValues(activeSkill);
    }

    /// <summary>
    /// Adapt all values to new skill + reset rollout
    /// </summary>
    /// <param name="_activeSkill"></param>
    public void SetActiveCurriculum(int _activeSkill)
    {
        activeSkill = _activeSkill;
        _lesson = curriculumSkills[_activeSkill].lesson;
        UpdateCurriculumValues(_activeSkill);
        ResetRollout();
    }

    /// <summary>
    /// reduce assistant force with each lesson
    /// </summary>
    public void UpdateLesson(int lesson, float _newExpertReward )
    {
        _lesson = curriculumSkills[activeSkill].lesson = lesson;
        curriculumSkills[activeSkill].expertReward = _newExpertReward;
        //if last lesson done:
        if (_lesson * reductionPercentage >= 1)
        {
            //end curriculum learning
            agent.curriculumLearning = false;
        }
        //reset values
        ResetRollout();
    }

    /// <summary>
    /// gradually (timeAlive dependent) reduces forces in one rollout ; avoids overfitting to one curricula by reducing force on milestone reached
    /// </summary>
    public void ReachedMileStone()
    {
        //if curriculum not already done:
        if (_lesson * reductionPercentage <= 1)
        {
            milestoneCounter += 1;
            if (milestoneCounter >= 2)
            {
                globalCurricula.UpdateAll(reward, activeSkill);
                ResetRollout();
            }

            multiplier = GetMultiplier(milestoneCounter);
            propellingForce = initPropForce * multiplier;
            lateralBalanceForce = initLatForce * multiplier;
            breakForce = initBreakForce * multiplier;
        }

    }

    /// <summary>
    /// reset to initial values when agent is reset
    /// </summary>
    public void ResetRollout()
    {
       
        milestoneCounter = 0;
        multiplier = GetMultiplier();
        lateralBalanceForce = initLatForce * multiplier;
        propellingForce = initPropForce * multiplier;
        breakForce = initBreakForce * multiplier;

    }

    /// <summary>
    /// returns the lesson respective percentage mult to update assistant forces. range 0,1
    /// </summary>
    /// <returns></returns>
    float GetMultiplier(float mileStoneCounter = 0)
    {
        _lesson = curriculumSkills[activeSkill].lesson;
        
        float _multiplier = 1 - ((reductionPercentage * (_lesson + milestoneCounter)) <= 1 ? (reductionPercentage * (_lesson + milestoneCounter)) : 1);    //returns 1 if curriculum done
        return _multiplier;
    }

    void UpdateCurriculumValues(int _activeSkill)
    {
        mileStone = curriculumSkills[_activeSkill].mileStone;
        reductionPercentage = curriculumSkills[_activeSkill].reductionPercentage;
        initLatForce = curriculumSkills[_activeSkill].initLateralForce;
        initPropForce = curriculumSkills[_activeSkill].initPropellingForce;
        initBreakForce = curriculumSkills[_activeSkill].initBreakForce;
        lateralBalanceForce = curriculumSkills[_activeSkill].lateralForce;
        propellingForce = curriculumSkills[_activeSkill].propellingForce;
        breakForce = curriculumSkills[_activeSkill].breakForce;
    }
}
