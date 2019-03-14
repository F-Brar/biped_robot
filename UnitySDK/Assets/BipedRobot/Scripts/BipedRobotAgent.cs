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
    //target
    [Header("Target To Walk Towards")]
    public Transform target;
    public bool useTarget;
    Vector3 dirToTarget;
    //Sensors
    public bool feetTouchingGround;
    public bool isLeftFootDown;
    public bool isRightFootDown;

    
    //bodyparts
    public Transform head;  //rigid bp - for measuring height
    public Transform hips;
    public Transform body;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;

    //local vars
    RobotJointDriveController jdController;
    bool isNewDecisionStep;
    int currentDecisionStep;
    Dictionary<string, Quaternion> BodyPartsToFocalRoation = new Dictionary<string, Quaternion>();  //use this to determine the initial axis

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

        // set body part directions
        foreach (var bodyPart in jdController.robotBodyPartsDict)
        {
            var name = bodyPart.Key.name;   //transform name
            var rigidbody = bodyPart.Value.rb;

            // find up
            var focalPoint = rigidbody.position;
            focalPoint.z += 10; //forward is z
            var focalPointRotation = rigidbody.rotation;    //copy initial rotation
            focalPointRotation.SetLookRotation(focalPoint - rigidbody.position);    //save forward rotation to determine angle diff
            BodyPartsToFocalRoation[name] = focalPointRotation;
            Debug.DrawRay(rigidbody.position, focalPoint - rigidbody.position, Color.red, 5);
        }
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
        //AddVectorObs(jdController.robotBodyPartsDict[body].rb.position);
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
            moveToTarget = Vector3.Dot(dirToTarget.normalized, hipsRB.velocity);
            rotToTarget = Vector3.Dot(dirToTarget.normalized, hips.forward);
        }
        #endregion

        float headHeight = hipsRB.position.y - head.position.y;

        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        // b. Rotation alignment with goal direction.
        // c. Encourage head height.
        // d. Discourage head movement.
        AddReward(
            + 0.01f * rotToTarget   //Only if useTarget
            + 0.03f * moveToTarget  //Only if useTarget
            + 0.02f * (hipsRB.centerOfMass.y - 1)
            + 0.02f * headHeight
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

    // implement phase bonus (reward for left then right)
    List<bool> _lastSenorState = new List<bool>();
    int NumSensors = 2; //left food, right food
    
    public float _phaseBonus;
    public int _phase;
    public float _reward;
    public float _velocity;
    public float _uprightBonus;
    public float _forwardBonus;
    public float _finalPhaseBonus;
    public float _heightPenality;
    public float _limbPenalty;
    public float _jointsAtLimitPenality;
    public float _effortPenality;


    public float LeftMin;
    public float LeftMax;

    public float RightMin;
    public float RightMax;

    void PhaseBonusInitalize()
    {
        _lastSenorState = Enumerable.Repeat<bool>(false, NumSensors).ToList();
        _phase = 0;
        _phaseBonus = 0f;
        PhaseResetLeft();
        PhaseResetRight();
    }

    void PhaseResetLeft()
    {
        LeftMin = float.MaxValue;
        LeftMax = float.MinValue;
        PhaseSetLeft();
    }

    void PhaseResetRight()
    {
        RightMin = float.MaxValue;
        RightMax = float.MinValue;
        PhaseSetRight();
    }

    void PhaseSetLeft()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRoation["thighL"] * jdController.robotBodyPartsDict[thighL].rb.transform.right;
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;
        LeftMin = Mathf.Min(LeftMin, bonus);
        LeftMax = Mathf.Max(LeftMax, bonus);
    }

    void PhaseSetRight()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRoation["thighR"] * jdController.robotBodyPartsDict[thighL].rb.transform.right;
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;
        RightMin = Mathf.Min(RightMin, bonus);
        RightMax = Mathf.Max(RightMax, bonus);
    }

    float CalcPhaseBonus(float min, float max)
    {
        float bonus = 0f;
        if (min < 0f && max < 0f)
        {
            min = Mathf.Abs(min);
            max = Mathf.Abs(max);
        }
        else if (min < 0f)
        {
            bonus = Mathf.Abs(min);
            min = 0f;
        }

        bonus += max - min;
        return bonus;
    }

    float GetPhaseBonus()
    {
        bool noPhaseChange = true;
        //bool isLeftFootDown = SensorIsInTouch[0] > 0f || SensorIsInTouch[1] > 0f;
        //bool isRightFootDown = SensorIsInTouch[2] > 0f || SensorIsInTouch[3] > 0f;
        bool wasLeftFootDown = _lastSenorState[0];
        bool wasRightFootDown = _lastSenorState[1];
        noPhaseChange = noPhaseChange && isLeftFootDown == wasLeftFootDown;
        noPhaseChange = noPhaseChange && isRightFootDown == wasRightFootDown;
        _lastSenorState[0] = isLeftFootDown;
        _lastSenorState[1] = isRightFootDown;
        if (isLeftFootDown && isRightFootDown)
        {
            _phase = 0;
            _phaseBonus = 0f;
            PhaseResetLeft();
            PhaseResetRight();
            return _phaseBonus;
        }

        PhaseSetLeft();
        PhaseSetRight();

        if (noPhaseChange)
        {
            var bonus = _phaseBonus;
            _phaseBonus *= 0.9f;
            return bonus;
        }

        // new phase
        _phaseBonus = 0;
        if (isLeftFootDown)
        {
            if (_phase == 1)
            {
                _phaseBonus = 0f;
            }
            else
            {
                _phaseBonus = CalcPhaseBonus(LeftMin, LeftMax);
                _phaseBonus += 0.1f;
            }
            _phase = 1;
            PhaseResetLeft();
        }
        else if (isRightFootDown)
        {
            if (_phase == 2)
            {
                _phaseBonus = 0f;
            }
            else
            {
                _phaseBonus = CalcPhaseBonus(RightMin, RightMax);
                _phaseBonus += 0.1f;
            }
            _phase = 2;
            PhaseResetRight();
        }

        return _phaseBonus;
    }

}


