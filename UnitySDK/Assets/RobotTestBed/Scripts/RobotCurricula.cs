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
    private List<BipedRobotAgent> agents;
    private List<LocalRobotCurricula> curricula;
    [Tooltip("meters milestone global")]
    public float p = 3;
    [Tooltip("percantage reduction on milestone reach")]
    public float k = .1f;
    [Tooltip("update the curriculum when this value of initial policy is reached")]
    public float updateOnPercentage;

    [HideInInspector]
    public LocoAcadamy academy;

    /// <summary>
    /// the cumulative reward from the initial trained policy with full assistance
    /// </summary>
    public float expertReward;
    public int lesson = 1;

    public bool init;
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        agents = new List<BipedRobotAgent>();
        curricula = new List<LocalRobotCurricula>();

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
                curr.mileStone = p;
            }

            
        }
        init = true;
    }

    /// <summary>
    /// update all if 70% of expertReward is satisfied
    /// </summary>
    /// <param name="reward"></param>
    public void UpdateAll(float reward)
    {
        if (!init)
        {
            return;
        }
        //check if the sent reward is bigger than percentage of the stored expert 
        if(reward >= expertReward * .7f)
        {

            lesson++;
            foreach (LocalRobotCurricula curr in curricula)
            {
                curr.ResetRollout();
                curr.UpdateLesson(lesson);
            }
            academy.AcademyReset();

            //if last lesson done:
            if (lesson * k == 1)
            {
                //end curriculum learning
                foreach (BipedRobotAgent agent in agents)
                {
                    agent.curriculumLearning = false;
                }
            }


        }
        
       

    }

}
