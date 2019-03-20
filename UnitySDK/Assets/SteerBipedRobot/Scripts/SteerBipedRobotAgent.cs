using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class SteerBipedRobotAgent : Agent
{
    public string currentVelocityField;
    public string velocityRewardField;

    [Tooltip("Last set of Actions")]
    /**< \brief Last set of Actions*/
    public List<float> Actions;

    public float TargetVelocityZ;


    public bool ShouldJump;
    public float CurrentVelocityZ;
    public int StepsUntilNextTarget;
    public Transform ground;

    public Transform hips;
    public Transform body;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;

    public bool feetTouchingGround;

    SteerRobotJointDriveController jdController;
    bool isNewDecisionStep;
    int currentDecisionStep;
    ControllerAgent controllerAgent;
    List<float> recentVelocity;



    public override void InitializeAgent()
    {
        if (!ground)
        {
            ground = GameObject.FindGameObjectWithTag("ground").transform;
        }
        jdController = GetComponent<SteerRobotJointDriveController>();
        jdController.SetupBodyPart(body);
        jdController.SetupBodyPart(hips);
        jdController.SetupBodyPart(thighL);
        jdController.SetupBodyPart(shinL);
        jdController.SetupBodyPart(footL);
        jdController.SetupBodyPart(thighR);
        jdController.SetupBodyPart(shinR);
        jdController.SetupBodyPart(footR);
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(SteerRobotBodyPart bp)
    {
        var rb = bp.rb;
        AddVectorObs(bp.groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground
        AddVectorObs(rb.velocity);
        AddVectorObs(rb.angularVelocity);
        Vector3 localPosRelToHips = body.InverseTransformPoint(rb.position);    //world to local space
        AddVectorObs(localPosRelToHips);

        if (bp.rb.transform != footL && bp.rb.transform != footR)
        {
            AddVectorObs(bp.currentXNormalizedRot);
            AddVectorObs(bp.currentYNormalizedRot);
            AddVectorObs(bp.currentZNormalizedRot);
            AddVectorObs(bp.currentStrength / jdController.maxJointForceLimit);
        }
        else
        {
            AddVectorObs(footL.position.y);
            AddVectorObs(footR.position.y);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations()
    {
        //Observe input
        TargetVelocityZ = controllerAgent.AxisX;
        ShouldJump = controllerAgent.Jump;

        AddVectorObs(TargetVelocityZ);
        AddVectorObs(CurrentVelocityZ);
        AddVectorObs(ShouldJump);

        jdController.GetCurrentJointForces();


        //AddVectorObs(jdController.robotBodyPartsDict[body].rb.position);
        AddVectorObs(body.forward);
        AddVectorObs(body.up);



        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart);

        }
    }


    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Actions = vectorAction
                .Select(x => x)
                .ToList();

        // Apply action to all relevant body parts. 
        if (isNewDecisionStep)
        {
            var bpDict = jdController.robotBodyPartsDict;
            int i = -1;
            
            bpDict[body].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[thighL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[thighR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[shinL].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[shinR].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[footR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            bpDict[footL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);

            //update joint strength settings
            bpDict[body].SetJointStrength(vectorAction[++i]);
            bpDict[thighL].SetJointStrength(vectorAction[++i]);
            bpDict[shinL].SetJointStrength(vectorAction[++i]);
            bpDict[footL].SetJointStrength(vectorAction[++i]);
            bpDict[thighR].SetJointStrength(vectorAction[++i]);
            bpDict[shinR].SetJointStrength(vectorAction[++i]);
            bpDict[footR].SetJointStrength(vectorAction[++i]);

        }

        IncrementDecisionTimer();

        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        // b. Rotation alignment with goal direction.
        // c. Encourage head height.
        // d. Discourage head movement.

        //  var jumpBonus = ShouldJump ? GetRewardJump() : 0f;
        float speed = 1;
        TargetVelocityZ = TargetVelocityZ * speed;
        CurrentVelocityZ = GetAverageVelocity(hips);
        //float velocityReward = 1f - (Mathf.Abs(TargetVelocityZ - CurrentVelocityZ) * 1.2f);
        float velocityDiff = Mathf.Abs(CurrentVelocityZ - TargetVelocityZ);

        currentVelocityField = CurrentVelocityZ.ToString();
        velocityRewardField = velocityDiff.ToString();
        //velocityReward = Mathf.Clamp(velocityReward, -1f, 1f);


        float effort = GetEffort();
         //var effortPenality = 1e-2f * (float)effort;
        //var effortPenality = (float)effort;
        //Vector3 direction = new Vector3(0, 0, TargetVelocityZ);
        //float aliveBonus = 0.001f;

        AddReward(
            //- 0.03f * velocityDiff   //encourage agent to match velocity
            //+ 0.02f * (jdController.robotBodyPartsDict[body].rb.centerOfMass.y - ground.position.y)       //encourage body height
            + 0.03f * Vector3.Dot(Vector3.forward, jdController.robotBodyPartsDict[body].rb.velocity)
            + 0.01f * Vector3.Dot(Vector3.forward, body.forward)
            //+ 0.01f * Vector3.Dot(Vector3.up, body.up);   //encourage body to stay upward
            //- 0.01f * Vector3.Distance(jdController.robotBodyPartsDict[body].rb.velocity,
            //    jdController.robotBodyPartsDict[hips].rb.velocity)      //discourage head movement
                                                                        //+0.03f * Vector3.Dot(dirToTarget.normalized, jdController.robotBodyPartsDict[body].rb.velocity)
                                                                        //+ 0.01f * Vector3.Dot(dirToTarget.normalized, body.forward)     //dotproduct ist positiv wenn der Winkel spitz ist, also in dieselbe Richtung zeigt.
                                                                        //+ jumpBonus
            //- 0.01f * effort
            //+ aliveBonus
            //+ 0.02f * (jdController.robotBodyPartsDict[body].rb.centerOfMass.y - 1)//(body.position.y - body.root.position.y)
            //- 1f / agentParameters.maxStep  //// Penalty given each step to encourage agent to finish task quickly
            //- 0.01f * Vector3.Distance(jdController.bodyPartsDict[head].rb.velocity,jdController.bodyPartsDict[body].rb.velocity)
            );
        //controllerAgent.LowerStepReward(CurrentVelocityZ);
    }

    internal float GetAverageVelocity(Transform bodyPart = null)
    {
        var v = GetVelocity(bodyPart);
        recentVelocity.Add(v);
        if (recentVelocity.Count >= 20)
            recentVelocity.RemoveAt(0);
        return recentVelocity.Average();
    }

    internal float GetVelocity(Transform bodyPart = null)
    {
        float rawVelocity = 0f;
        if (bodyPart != null)
            rawVelocity = jdController.robotBodyPartsDict[bodyPart].rb.velocity.z;
        else
            rawVelocity = jdController.robotBodyPartsDict[hips].rb.velocity.z;

        //var maxSpeed = 4f; // velocity = meters per second : 4
        var velocity = rawVelocity; // /maxSpeed;
        return velocity;
    }


    internal float GetEffort(string[] ignorJoints = null)
    {
        double effort = 0;
        for (int i = 0; i < Actions.Count; i++)
        {
            
            if (i >= jdController.robotBodyPartsList.Count)
                continue; // handle case when to many actions
            var name = jdController.robotBodyPartsList[i].jointName;
            if (ignorJoints != null && ignorJoints.Contains(name))
                continue;
            
            var jointEffort = Mathf.Pow(Mathf.Abs(Actions[i]), 2);
            effort += jointEffort;
        }

        return (float)effort;
    }

    /// <summary>
    /// Only change the joint settings based on decision frequency.
    /// </summary>
    public void IncrementDecisionTimer()
    {
        if (currentDecisionStep == agentParameters.numberOfActionsBetweenDecisions ||
            agentParameters.numberOfActionsBetweenDecisions == 1)
        {
            currentDecisionStep = 1;
            isNewDecisionStep = true;
        }
        else
        {
            currentDecisionStep++;
            isNewDecisionStep = false;
        }
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void AgentReset()
    {

        if (controllerAgent == null)
            controllerAgent = GetComponent<ControllerAgent>();
        //else
            //controllerAgent.LowerEpisodeEnd(this);
        
        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        isNewDecisionStep = true;
        currentDecisionStep = 1;

        recentVelocity = new List<float>();
    }

    /// <summary>
    /// returns reward if both feet off ground + jump height
    /// </summary>
    /// <returns></returns>
    float GetRewardJump()
    {
        var jumpReward = 0f;
        var footHeight = Mathf.Min(this.footR.position.y, this.footL.position.y);

        if (feetTouchingGround == false)
        {
            jumpReward += 1f;
            jumpReward += footHeight;
        }
        return jumpReward;
    }



}


