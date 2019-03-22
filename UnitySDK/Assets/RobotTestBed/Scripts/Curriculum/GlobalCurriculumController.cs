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
    [Tooltip ("needs to match 1/updateOnPercentage : example 1/0,25 = 4")]
    private int minimumStepsToReach;
    private bool isMinimumStepCountReached;
    public int stepCount;
    private int stepCountAtLessonStart;


    public bool IsStepCountReached(int AcademyStepCount)
    {
        this.stepCount = AcademyStepCount - this.stepCountAtLessonStart;
        this.isMinimumStepCountReached = this.stepCount >= this.minimumStepsToReach;
        return isMinimumStepCountReached;
    }

    public bool IsRewardReached(float _reward)
    {
        return _reward >= this.expertReward * this.updateOnPercentage;
    }

    public void LessonReset(int AcademyStepCount)
    {
        this.stepCountAtLessonStart = AcademyStepCount;
    }

}

/// <summary>
/// iterate the curriulum if the policy reaches 70% of its predecessor and stays 2 p (= 3) seconds alive
/// </summary>
[System.Serializable]
public class GlobalCurriculumController : MonoBehaviour
{
    public List<Curriculum> curriculumList;
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
            currController.Init(curriculumList, shouldCurriculumLearning);
            currController.globalCurriculumController = this;
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
        var activeCurriculum = curriculumList[activeSkill];
        
        //locked if the lesson has just been updatet && check if the minimumStepCount is reached
        if (!locked && activeCurriculum.IsStepCountReached(academy.stepCount))
        {
            //check if the sent reward is bigger than percentage of the stored expert 
            if (activeCurriculum.IsRewardReached(reward))
            {
                StartCoroutine(LockLessons());
                //update lesson
                activeCurriculum.lesson++;
                //update stepCount
                
                //update experReward
                float _newExpertReward = activeCurriculum.expertReward = Mathf.Max(reward, activeCurriculum.expertReward);
                foreach (LocalCurriculumController curr in curriculumControllerList)
                {
                    curr.SetLesson(activeCurriculum.lesson, _newExpertReward);
                }
            }
        }
    }

    /// <summary>
    /// used to restart curriculum learning for multiSkill learning
    /// </summary>
    public void ResetCurriculumLearning(int skillToBeReset)
    {
        var curriculumToBeReset = curriculumList[skillToBeReset];
        curriculumToBeReset.lesson = 0;
        curriculumToBeReset.LessonReset(academy.stepCount);
        foreach (LocalCurriculumController curr in curriculumControllerList)
        {
            curr.SetLesson(curriculumToBeReset.lesson, 0);
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
