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
    
    [Header("Target To Walk Towards")]
    public Transform target;
    public bool useTarget;
    public bool feetTouchingGround;

    Vector3 dirToTarget;
    
    public Transform hips;
    public Transform body;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;

    RobotJointDriveController jdController;
    bool isNewDecisionStep;
    int currentDecisionStep;

    public override void InitializeAgent()
    {
        jdController = GetComponent<RobotJointDriveController>();
        //Debug.Log("body: " + body.name);
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
    public void CollectObservationBodyPart(RobotBodyPart bp)
    {
        var rb = bp.rb;
        AddVectorObs(bp.groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground
        AddVectorObs(rb.velocity);
        AddVectorObs(rb.angularVelocity);
        Vector3 localPosRelToHips = hips.InverseTransformPoint(rb.position);
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
        
        if (useTarget)
        {
            AddVectorObs(dirToTarget.normalized);
        }

        jdController.GetCurrentJointForces();
        AddVectorObs(jdController.robotBodyPartsDict[body].rb.position);
        AddVectorObs(hips.forward);
        AddVectorObs(hips.up);

        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (useTarget)
        {
            dirToTarget = target.position - jdController.robotBodyPartsDict[hips].rb.position;
        }
        
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


            bpDict[body].SetJointStrength(vectorAction[++i]);
            bpDict[thighL].SetJointStrength(vectorAction[++i]);
            bpDict[shinL].SetJointStrength(vectorAction[++i]);
            bpDict[footL].SetJointStrength(vectorAction[++i]);
            bpDict[thighR].SetJointStrength(vectorAction[++i]);
            bpDict[shinR].SetJointStrength(vectorAction[++i]);
            bpDict[footR].SetJointStrength(vectorAction[++i]);

        }

        IncrementDecisionTimer();
        var hipsRB = jdController.robotBodyPartsDict[hips].rb;
        //var hipsVelocity = transform.InverseTransformDirection(hipsRB.velocity);

        #region OnlyIfTarget

        var rotToTarget = 0f;
        float moveToTarget = 0f;

        if (useTarget)
        {
            moveToTarget = 0.03f * Vector3.Dot(dirToTarget.normalized, hipsRB.velocity);
            rotToTarget = 0.01f * Vector3.Dot(dirToTarget.normalized, hips.forward);
        }
        #endregion

        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        // b. Rotation alignment with goal direction.
        // c. Encourage head height.
        // d. Discourage head movement.
        AddReward(
            + rotToTarget   //Only if useTarget
            + moveToTarget  //Only if useTarget
            //+ 0.02f * (hipsRB.centerOfMass.y - 1)
            //- 0.01f * Vector3.Distance(jdController.bodyPartsDict[head].rb.velocity,jdController.bodyPartsDict[body].rb.velocity)
        );
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
        if (dirToTarget != Vector3.zero && useTarget)
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


