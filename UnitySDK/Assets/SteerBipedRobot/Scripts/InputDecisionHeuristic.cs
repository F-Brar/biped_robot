using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class InputDecisionHeuristic : Decision
{

    public float X;
    public int Action;

    float[] ActionList;
    
    
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
            if (rnd > .6f)           // || JumpAction != 0 && rnd > .75f ?
                repeateAction = true;
            if (!repeateAction)
            {
                rnd = UnityEngine.Random.value;
                if (rnd <= .3f)
                    Action = 1;
                else
                    Action = 0; // stand
            }
            memory[0] = 80 + (int)(UnityEngine.Random.value * 300); //statt 40 / 200
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
        // memory.Add(0);
        return memory;
    }
}
