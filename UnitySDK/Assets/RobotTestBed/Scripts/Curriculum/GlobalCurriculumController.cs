using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

[System.Serializable]
public class Curriculum
{
    [Tooltip("name of the respective skill")]
    public string CurriculumName;

    [Tooltip("time milestone global")]
    public float mileStone = .5f;
    [Tooltip("percantage reduction on milestone reach")]
    public float reductionPercentage = .1f;
    [Tooltip("update the curriculum when this value of initial policy is reached")]
    public float updateOnPercentage = .7f;
    [Header("Initial skill dependent values")]
    [Tooltip("initial propelling force for this skill")]
    public float initPropellingForce = 25;
    [Tooltip("initial lateral balance force for this skill")]
    public float initLateralForce = 20;
    [Tooltip("initial lateral balance force for this skill")]
    public float initBreakForce = 20;
    [Header("Actual curriculum values")]
    [Tooltip("initial propelling force for this skill")]
    public float propellingForce = 0;
    [Tooltip("initial lateral balance force for this skill")]
    public float lateralForce = 0;
    [Tooltip("initial lateral balance force for this skill")]
    public float breakForce = 0;
    /// <summary>
    /// the cumulative reward from the initial trained policy with full assistance
    /// </summary>
    public float expertReward;
    public float reward;
    public int lesson = 0;

}

/// <summary>
/// iterate the curriulum if the policy reaches 70% of its predecessor and stays 2 p (= 3) seconds alive
/// </summary>
[System.Serializable]
public class GlobalCurriculumController : MonoBehaviour
{
    public List<Curriculum> curriculumSkills;
    public bool shouldCurriculumLearning;
    private List<LocalCurriculumController> curriculumControllerList;

    [HideInInspector]
    public LocoAcadamy academy;
    //locks lesson updates
    private bool locked;
    /// <summary>
    /// initialize
    /// </summary>
    public void Init()
    {
        curriculumControllerList = new List<LocalCurriculumController>();

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("agent"))
        {
            obj.AddComponent<VirtualAssistant>();
            var currController = obj.AddComponent<LocalCurriculumController>();
            currController.Init(curriculumSkills);
            currController.globalCurricula = this;
            currController.agent.curriculumLearning = shouldCurriculumLearning;
            curriculumControllerList.Add(currController);

        }
    }

    /// <summary>
    /// update all if 70% of expertReward is satisfied
    /// </summary>
    /// <param name="reward"></param>
    public void UpdateAll(float reward, int activeSkill)
    {
        var activeCurriculum = curriculumSkills[activeSkill];
        //check if the sent reward is bigger than percentage of the stored expert
        if(reward >= activeCurriculum.expertReward * activeCurriculum.updateOnPercentage && !locked)
        {
            StartCoroutine(LockLessons());
            activeCurriculum.lesson++;
            var _newExpertReward = activeCurriculum.expertReward = Mathf.Max(reward, activeCurriculum.expertReward);
            foreach (LocalCurriculumController curr in curriculumControllerList)
            {
                curr.UpdateLesson(activeCurriculum.lesson, _newExpertReward);
            }
        }
    }

    IEnumerator LockLessons()
    {
        locked = true;
        yield return new WaitForSeconds(2);
        locked = false;
        yield break;
    }

}
