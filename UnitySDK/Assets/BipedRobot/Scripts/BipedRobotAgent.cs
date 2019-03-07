using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class BipedRobotAgent : Agent
{

    [Tooltip("Last set of Actions")]
    /**< \brief Last set of Actions*/
    public List<float> Actions;

    //[Header("Specific to Walker")]
    [Header("Target To Walk Towards")]
    //[Space(10)]
    public Transform target;
    

    Vector3 dirToTarget;
    /*
   
    public Transform chest;
    public Transform spine;
    public Transform head;
    
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;
    */
    public Transform hips;
    public Transform body;
    //public Transform body;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;

    RobotJointDriveController jdController;
    //JointDriveController jdController;
    bool isNewDecisionStep;
    int currentDecisionStep;

    public override void InitializeAgent()
    {
        jdController = GetComponent<RobotJointDriveController>();
        //Debug.Log("body: " + body.name);
        jdController.SetupBodyPart(body);
        //jdController.SetupBodyPart(chest);
        //jdController.SetupBodyPart(spine);
        //jdController.SetupBodyPart(head);
        jdController.SetupBodyPart(hips);
        jdController.SetupBodyPart(thighL);
        jdController.SetupBodyPart(shinL);
        jdController.SetupBodyPart(footL);
        jdController.SetupBodyPart(thighR);
        jdController.SetupBodyPart(shinR);
        jdController.SetupBodyPart(footR);
        /*jdController.SetupBodyPart(armL);
        jdController.SetupBodyPart(forearmL);
        jdController.SetupBodyPart(handL);
        jdController.SetupBodyPart(armR);
        jdController.SetupBodyPart(forearmR);
        jdController.SetupBodyPart(handR);*/
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(RobotBodyPart bp)
    {
        var rb = bp.rb;
        AddVectorObs(bp.groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground
        AddVectorObs(rb.velocity);
        AddVectorObs(rb.angularVelocity);
        Vector3 localPosRelToHips = body.InverseTransformPoint(rb.position);
        AddVectorObs(localPosRelToHips);

        if (bp.rb.transform != footL && bp.rb.transform != footR)
        {
            AddVectorObs(bp.currentXNormalizedRot);
            AddVectorObs(bp.currentYNormalizedRot);
            AddVectorObs(bp.currentZNormalizedRot);
            AddVectorObs(bp.currentStrength / jdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations()
    {
        jdController.GetCurrentJointForces();

        AddVectorObs(dirToTarget.normalized);
        AddVectorObs(jdController.robotBodyPartsDict[body].rb.position);
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
        dirToTarget = target.position - jdController.robotBodyPartsDict[hips].rb.position;

        // Apply action to all relevant body parts. 
        if (isNewDecisionStep)
        {
            var bpDict = jdController.robotBodyPartsDict;
            int i = -1;

            //bpDict[chest].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            //bpDict[spine].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            bpDict[body].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[thighL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[thighR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[shinL].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[shinR].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[footR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            bpDict[footL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);


            //bpDict[armL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            //bpDict[armR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            //bpDict[forearmL].SetJointTargetRotation(vectorAction[++i], 0, 0);
            //bpDict[forearmR].SetJointTargetRotation(vectorAction[++i], 0, 0);
            //bpDict[head].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);

            //update joint strength settings
            //bpDict[chest].SetJointStrength(vectorAction[++i]);
            //bpDict[spine].SetJointStrength(vectorAction[++i]);
            //bpDict[head].SetJointStrength(vectorAction[++i]);
            bpDict[body].SetJointStrength(vectorAction[++i]);
            bpDict[thighL].SetJointStrength(vectorAction[++i]);
            bpDict[shinL].SetJointStrength(vectorAction[++i]);
            bpDict[footL].SetJointStrength(vectorAction[++i]);
            bpDict[thighR].SetJointStrength(vectorAction[++i]);
            bpDict[shinR].SetJointStrength(vectorAction[++i]);
            bpDict[footR].SetJointStrength(vectorAction[++i]);
            //bpDict[armL].SetJointStrength(vectorAction[++i]);
            //bpDict[forearmL].SetJointStrength(vectorAction[++i]);
            //bpDict[armR].SetJointStrength(vectorAction[++i]);
            //bpDict[forearmR].SetJointStrength(vectorAction[++i]);

        }

        IncrementDecisionTimer();

        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        // b. Rotation alignment with goal direction.
        // c. Encourage head height.
        // d. Discourage head movement.
        AddReward(
            +0.03f * Vector3.Dot(dirToTarget.normalized, jdController.robotBodyPartsDict[body].rb.velocity)
            + 0.01f * Vector3.Dot(dirToTarget.normalized, body.forward)
            + 0.02f * (jdController.robotBodyPartsDict[body].rb.centerOfMass.y - 1)//(body.position.y - body.root.position.y)
            //- 0.0001f// * GetEffort()
            //- 0.01f * Vector3.Distance(jdController.bodyPartsDict[head].rb.velocity,jdController.bodyPartsDict[body].rb.velocity)
        );
    }

    internal float GetEffort(string[] ignorJoints = null)
    {
        double effort = 0;
        for (int i = 0; i < Actions.Count; i++)
        {
            /*
            if (i >= MarathonJoints.Count)
                continue; // handle case when to many actions
            var name = MarathonJoints[i].JointName;
            if (ignorJoints != null && ignorJoints.Contains(name))
                continue;
            */
            var jointEffort = Mathf.Pow(Mathf.Abs(Actions[i]), 2);
            effort += jointEffort;
        }

        return (float)effort;
    }
    /*
    internal float GetUprightBonus(string bodyPart)
    {
        var toFocalAngle = BodyPartsToFocalRoation[bodyPart] * -BodyParts[bodyPart].transform.forward;
        var angleFromUp = Vector3.Angle(toFocalAngle, Vector3.up);
        var qpos2 = (angleFromUp % 180) / 180;
        var uprightBonus = 0.5f * (2 - (Mathf.Abs(qpos2) * 2) - 1);
        return uprightBonus;
    }*/


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
        if (dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }
        
        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        isNewDecisionStep = true;
        currentDecisionStep = 1;
    }
}


