using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;


/// <summary>
/// iterate the curriulum if the policy reaches 70% of its predecessor and stays 2 p (= 3) seconds alive
/// </summary>
[System.Serializable]
public class RobotCurricula : MonoBehaviour
{
    //private List<BipedRobotAgent> agents;
    private List<LocalRobotCurricula> curricula;
    [Tooltip("time milestone global")]
    public float _mileStone = 3;
    [Tooltip("percantage reduction on milestone reach")]
    public float _reductionPercentage = .1f;
    [Tooltip("update the curriculum when this value of initial policy is reached")]
    public float updateOnPercentage = .7f;
    [Tooltip("initial propelling force")]
    public float _initPropForce = 25;
    [Tooltip("initial lateral balance force")]
    public float _initLatForce = 20;
    [HideInInspector]
    public LocoAcadamy academy;

    /// <summary>
    /// the cumulative reward from the initial trained policy with full assistance
    /// </summary>
    public float expertReward;
    public int lesson = 0;
    
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        curricula = new List<LocalRobotCurricula>();

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("agent"))
        {
            var curr = obj.GetComponent<LocalRobotCurricula>();
            curr.initPropForce = _initPropForce;
            curr.initLatForce = _initLatForce;
            curr.globalCurricula = this;
            curr.Init();
            curricula.Add(curr);

            if(_reductionPercentage != 0)
            {
                curr.reductionPercentage = _reductionPercentage;
            }
            if(_mileStone != 0)
            {
                curr.mileStone = _mileStone;
            }
        }
    }

    /// <summary>
    /// update all if 70% of expertReward is satisfied
    /// </summary>
    /// <param name="reward"></param>
    public void UpdateAll(float reward)
    {
        //check if the sent reward is bigger than percentage of the stored expert
        if(reward >= expertReward * updateOnPercentage)
        {
            lesson++;

            foreach (LocalRobotCurricula curr in curricula)
            {
                curr.agent.AgentReset();
                curr.UpdateLesson(lesson);
            }

            expertReward = Mathf.Max(reward, expertReward);
        }
    }

}
