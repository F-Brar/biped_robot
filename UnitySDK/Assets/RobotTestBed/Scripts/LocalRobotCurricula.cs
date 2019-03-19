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
    //initial forces updatet only on lesson iteration
    public float initPropForce = 25;
    public float initLatForce = 25;
    //internal forces updatet in each rollout
    public float propellingForce {
        get { return _propellingForce; }
        set {
            //update assistant
            _propellingForce = value;
            assistant.forwardStabilityForce = _propellingForce;
        }
    }

    private float _propellingForce;

    public float lateralBalanceForce {
        get { return _lateralBalanceForce; }
        set {
            //update assistant
            _lateralBalanceForce = value;
            assistant.sideWaysStabilityForce = _lateralBalanceForce;
        }
    }
    private float _lateralBalanceForce;

    public float reward {
        get { return _reward; }
        set {
            _reward = value;
            globalCurricula.expertReward = Mathf.Max(_reward, globalCurricula.expertReward);
        }
    }
    private float _reward;
    //reduction speed of forces
    public float reductionPercentage = .25f;

    //time alive milestone
    public float timeMileStone = 2;
    public int _lesson = 0;
    public int milestoneCounter;
    public float multiplier;
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        lateralBalanceForce = initLatForce;
        propellingForce = initPropForce;
    }
    /// <summary>
    /// reduce assistant force with each lesson
    /// </summary>
    public void UpdateLesson(int lesson)
    {
        _lesson = lesson;
        multiplier = 1 - (reductionPercentage * _lesson);
        propellingForce = initPropForce * multiplier;
        lateralBalanceForce = initLatForce * multiplier;
    }

    /// <summary>
    /// temporarily reduce forces in one rollout to avoid overfitting to one curricula
    /// </summary>
    public void ReachedMileStone()
    {
        milestoneCounter += 1;
        if (milestoneCounter >= 2)
        {
            if(propellingForce >= 0)
            {
                globalCurricula.UpdateAll(reward);
            }
        }
        multiplier = 1 - (reductionPercentage * (_lesson + milestoneCounter));
        propellingForce -= initPropForce * multiplier;
        lateralBalanceForce -= initLatForce * multiplier;
    }

    /// <summary>
    /// reset to initial values when agent is reset
    /// </summary>
    public void ResetRollout()
    {
        multiplier = 1 - (reductionPercentage * _lesson);
        milestoneCounter = 0;
        lateralBalanceForce = initPropForce * multiplier;
        propellingForce = initLatForce * multiplier;
    }
}
