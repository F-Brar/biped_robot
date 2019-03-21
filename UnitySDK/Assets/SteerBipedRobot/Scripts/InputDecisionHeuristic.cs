using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class InputDecisionHeuristic : Decision
{
    public int Action;
    float[] ActionList;
    
    /// <summary>
    /// decision heuristic wich skill to execute
    /// </summary>
    /// <param name="vectorObs"></param>
    /// <param name="visualObs"></param>
    /// <param name="reward"></param>
    /// <param name="done"></param>
    /// <param name="memory"></param>
    /// <returns></returns>
    public override float[] Decide(
        List<float> vectorObs,
        List<Texture2D> visualObs,
        float reward,
        bool done,
        List<float> memory)
    {
        if (memory.Count == 0)
        {
            memory.Add(0f);
            memory.Add(0f);
        }
        Action = (int)memory[1];
        
        memory[0]--;
        if (memory[0] <= 0)
        {
            var rnd = UnityEngine.Random.value;
            bool repeateAction = false;
            if (Action != 0 && rnd > .6f)
                repeateAction = true;
            if (!repeateAction)
            {
                rnd = UnityEngine.Random.value;
                if (rnd <= .4f)
                    Action = 1; //walk
                else
                    Action = 0; // stand
            }
            memory[0] = 40 + (int)(UnityEngine.Random.value * 200); //statt 40 / 200
            memory[1] = (float)Action;
        }
        float[] ActionList = new float[1] { Action };
        return ActionList;
    }


    public override List<float> MakeMemory(
        List<float> vectorObs,
        List<Texture2D> visualObs,
        float reward,
        bool done,
        List<float> memory)
    {
        return memory;
    }
}
