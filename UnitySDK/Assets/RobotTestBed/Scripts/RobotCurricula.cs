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
    public List<BipedRobotAgent> agents;
    public List<LocalRobotCurricula> curricula;
    [Tooltip("time milestone global")]
    public float p = 3;
    [Tooltip("percantage reduction on milestone reach")]
    public float k = .25f;
    [Tooltip("update the curriculum when this value of initial policy is reached")]
    public float updateOnPercentage;

    private LocoAcadamy academy;

    /// <summary>
    /// the cumulative reward from the initial trained policy with full assistance
    /// </summary>
    public float expertReward;
    public int lesson = 1;

    private void Awake()
    {
        Init();
    }
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("agent"))
        {
            var agent = obj.GetComponent<BipedRobotAgent>();
            var curr = obj.GetComponent<LocalRobotCurricula>();
            curr.globalCurricula = this;
            curricula.Add(curr);
            agents.Add(agent);

            if(k != 0)
            {
                curr.reductionPercentage = k;
            }
            if(p != 0)
            {
                curr.timeMileStone = p;
            }

            
        }
    }

    /// <summary>
    /// update all if 70% of expertReward is satisfied
    /// </summary>
    /// <param name="reward"></param>
    public void UpdateAll(float reward)
    {
        if(reward >= expertReward * .7f)
        {
            lesson++;
            foreach (LocalRobotCurricula curr in curricula)
            {
                curr.UpdateLesson(lesson);
            }
            academy.AcademyReset();
        }

    }

}
