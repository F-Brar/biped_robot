using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class InputDecisionHeuristic : Decision
{

    public float X;
    //public float Y;
    public bool Jump;
    public int Action;
    public int JumpAction;

    float[] ActionList;
    

    public override float[] Decide(
        List<float> vectorObs,
        List<Texture2D> visualObs,
        float reward,
        bool done,
        List<float> memory)
    {
        ActionList = DecideHeuristic(vectorObs, visualObs, reward, done, memory);
        return ActionList;
    }

    float[] DecideHeuristic(
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
            memory.Add(0f);
        }
        Action = (int)memory[1];
        JumpAction = (int)memory[2];

        memory[0]--;
        if (memory[0] <= 0)
        {
            var rnd = UnityEngine.Random.value;
            bool repeateAction = false;
            if (Action != 0 && rnd > .6f)           // || JumpAction != 0 && rnd > .75f ?
                repeateAction = true;
            if (!repeateAction)
            {
                rnd = UnityEngine.Random.value;
                if (rnd <= .4f)
                    Action = 1; // right
                //else if (rnd <= .8f)
                //    Action = 2; // left
                else
                    Action = 0; // stand

                rnd = UnityEngine.Random.value;
                if (rnd >= .75)
                    JumpAction = 3; // add jump
                else
                {
                    JumpAction = 0;
                }
            }
            memory[0] = 80 + (int)(UnityEngine.Random.value * 300); //statt 40 / 200
            memory[1] = (float)Action;
            memory[2] = (float)JumpAction;
        }
        float[] ActionList = new float[2] { Action, JumpAction };
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
