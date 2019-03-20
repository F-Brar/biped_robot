using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// iterate the curriulum if the policy reaches 70% of its predecessor and stays 2p seconds alive
/// </summary>
[System.Serializable]
public class LocalRobotCurricula : MonoBehaviour
{
    public RobotCurricula globalCurricula;
    public VirtualAssistant assistant;
    public RobotMultiSkillAgent agent;

    #region AssistantForces
    public float initPropForce = 25;
    public float initLatForce = 25;
    //internal forces updatet in each rollout
    public float propellingForce {
        get { return _propellingForce; }
        set {
            //update assistant
            _propellingForce = value;
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
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        agent = GetComponent<RobotMultiSkillAgent>();
        lateralBalanceForce = initLatForce;
        propellingForce = initPropForce;
    }
    /// <summary>
    /// reduce assistant force with each lesson
    /// </summary>
    public void UpdateLesson(int lesson)
    {
        _lesson = lesson;
        //if last lesson done:
        if (_lesson * reductionPercentage >= 1)
        {
            //end curriculum learning
            agent.curriculumLearning = false;
            //for convenience
            agent.shouldCurriculumLearning = false;
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
                globalCurricula.UpdateAll(reward);
                ResetRollout();
            }

            multiplier = GetMultiplier(milestoneCounter);
            propellingForce = initPropForce * multiplier;
            lateralBalanceForce = initLatForce * multiplier;
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
        
    }

    /// <summary>
    /// returns the lesson respective percentage mult to update assistant forces. range 0,1
    /// </summary>
    /// <returns></returns>
    float GetMultiplier(float mileStoneCounter = 0)
    {
        float _multiplier = 1 - ((reductionPercentage * (_lesson + milestoneCounter)) <= 1 ? (reductionPercentage * (_lesson + milestoneCounter)) : 1);    //returns 1 if curriculum done
        return _multiplier;
    }
}
