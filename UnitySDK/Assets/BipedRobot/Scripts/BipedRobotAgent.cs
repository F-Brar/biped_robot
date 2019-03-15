﻿using System.Collections;
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
    public bool BothFeetDown;
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

    // Parms to set in subclass.AgentReset() 
    [Tooltip("Reward value to set on termination")]
    /**< \brief Reward value to set on termination*/
    protected float OnTerminateRewardValue = -1;

    //local vars
    RobotJointDriveController jdController;
    bool isNewDecisionStep;
    int currentDecisionStep;
    Dictionary<string, Quaternion> BodyPartsToFocalRotation = new Dictionary<string, Quaternion>();  //use this to determine the initial axis

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
            BodyPartsToFocalRotation[name] = focalPointRotation;                    //in initial condition these are all zero rotations
            //var EulerAngles = focalPointRotation.eulerAngles;
            //Debug.DrawRay(rigidbody.position, focalPoint - rigidbody.position, Color.red, 5);
            
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
        AddVectorObs(jdController.robotBodyPartsDict[hips].rb.velocity);
        AddVectorObs(hips.forward);
        AddVectorObs(hips.up);

        AddVectorObs(body.forward);
        AddVectorObs(body.up);

        

        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    bool TerminateRobot()
    {
        //if (TerminateOnNonFootHitTerrain())
        //    return true;
        var height = GetHeightPenality(.9f);
        var angle = GetForwardBonus(hips);
        bool endOnHeight = height > 0f;
        bool endOnAngle = (angle < .2f);
        return endOnHeight || endOnAngle;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (useTarget)
        {
            dirToTarget = target.position - jdController.robotBodyPartsDict[hips].rb.position;
        }

        if (!IsDone())
        {
            bool done = TerminateRobot();

            if (done)
            {
                Done();
                SetReward(OnTerminateRewardValue);
            }
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

        float _rotToTarget = 0f;
        float _moveToTarget = 0f;

        if (useTarget)
        {
            // Velocity alignment with goal direction.
            // Rotation alignment with goal direction.
            _moveToTarget = Vector3.Dot(dirToTarget.normalized, hipsRB.velocity);
            _rotToTarget = Vector3.Dot(dirToTarget.normalized, hips.forward);
        }
        #endregion

        _velocity = GetVelocity();
        _heightPenality = GetHeightPenality(1.3f);  //height of body
        // Encourage uprightness of hips and body.
        _uprightBonus =
            ( (GetUprightBonus(hips) / 4)//6
            + (GetUprightBonus(body) / 6));
        _forwardBonus =
            ( (GetForwardBonus(hips) / 4)//6
            + (GetForwardBonus(body) / 6));
        //_headHeightBonus = hipsRB.position.y - head.position.y;
        // penalize asynchron leg movement
        float leftThighPenality = Mathf.Abs(GetForwardBonus(thighL));
        float rightThighPenality = Mathf.Abs(GetBackwardsBonus(thighR));
        _limbPenalty = leftThighPenality + rightThighPenality;
        _limbPenalty = Mathf.Min(0.5f, _limbPenalty);   //penalty for moving both legs in the same direction
        _finalPhaseBonus = GetPhaseBonus();
        // Set reward for this step according to mixture of the following elements.

        
        // d. Discourage head movement.
        AddReward(_velocity
            + _uprightBonus
            + _forwardBonus
            + _finalPhaseBonus
            
            - _limbPenalty
            - _heightPenality
            + 0.3f * _rotToTarget   //if useTarget else 0
            + 0.5f * _moveToTarget  //Only if useTarget
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
        PhaseBonusInitalize();
        //recentVelocity = new List<float>();
    }

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
    public float _headHeightBonus;

    public float LeftMin;
    public float LeftMax;

    public float RightMin;
    public float RightMax;


    internal float GetVelocity(Transform bodyPart = null)
    {
        var dt = Time.fixedDeltaTime;
        float rawVelocity = 0f;
        if (bodyPart != null)
            rawVelocity = jdController.robotBodyPartsDict[bodyPart].rb.velocity.z;
        else
            rawVelocity = jdController.robotBodyPartsDict[hips].rb.velocity.z;  //velocity in meters per seconds

        var maxSpeed = 4f; // meters per second
        var velocity = rawVelocity / maxSpeed;
        /*
        if (ShowMonitor)
        {
            Monitor.Log("MaxDistance", FocalPointMaxDistanceTraveled.ToString());
            Monitor.Log("MPH: ", (rawVelocity * 2.236936f).ToString());
        }*/

        return velocity;
    }

    /// <summary>
    /// returns float penalty for falling below maxheight
    /// </summary>
    /// <param name="maxHeight"></param>
    /// <returns></returns>
    internal float GetHeightPenality(float maxHeight)
    {
        var height = GetHeight();   //body height
        var heightPenality = maxHeight - height;    //difference the set maxheight and the body height
        heightPenality = Mathf.Clamp(heightPenality, 0f, maxHeight);    //clamp between zero and maxheight
        return heightPenality;
    }

    /// <summary>
    /// get body height over lowest foot
    /// </summary>
    /// <returns></returns>
    internal float GetHeight()
    {
        var feetYpos = jdController.robotBodyPartsDict          //oredered list from lowest to highest foot
            .Where(x => x.Key.name.ToLowerInvariant().Contains("foot"))
            .Select(x => x.Key.position.y)
            .OrderBy(x => x)
            .ToList();
        float lowestFoot = 0f;
        if (feetYpos != null && feetYpos.Count != 0)
            lowestFoot = feetYpos[0];
        var height = body.transform.position.y - lowestFoot;
        return height;
    }

    /// <summary>
    /// returns upright reward between -1  and 1; positive if the angle between given bodypart and up axis is sharp
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetUprightBonus(Transform bodyPart)
    {
        var toFocalAngle = BodyPartsToFocalRotation[bodyPart.name] * bodyPart.up;       //bodypartstofocalrot == (0,0,0,1 [real part]) => 1*bodypart.up
        var angleFromUp = Vector3.Angle(toFocalAngle, Vector3.up);      //check angle between bodypart.up and up
        var qpos2 = (angleFromUp % 180) / 180;  //normalize the rotation angle
        var uprightBonus = 0.5f * (2 - (Mathf.Abs(qpos2) * 2) - 1);
        return uprightBonus;
    }

    /// <summary>
    /// /// returns upright reward between -maxbonus  and maxbonus; positive if the angle between given bodypart and direction axis is sharp
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <param name="direction"></param>
    /// <param name="maxBonus"></param>
    /// <returns></returns>
    internal float GetDirectionBonus(Transform bodyPart, Vector3 direction, float maxBonus = 0.5f)
    {
        var toFocalAngle = BodyPartsToFocalRotation[bodyPart.name] * bodyPart.forward;
        //Debug.DrawRay(bodyPart.position, toFocalAngle, Color.yellow, 1);
        var angle = Vector3.Angle(toFocalAngle, direction);
        var qpos2 = (angle % 180) / 180;
        var bonus = maxBonus * (2 - (Mathf.Abs(qpos2) * 2) - 1);
        //Debug.Log("direction: " + direction + " bonus: " + bonus + " qpos2: " + qpos2 + " angle: " + angle);
        return bonus;
    }

    /// <summary>
    /// leverages directionBonus
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetLeftBonus(Transform bodyPart)
    {
        var bonus = GetDirectionBonus(bodyPart, Vector3.left);
        return bonus;
    }
    /// <summary>
    /// leverages directionBonus
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetRightBonus(Transform bodyPart)
    {
        var bonus = GetDirectionBonus(bodyPart, Vector3.right);
        return bonus;
    }
    /// <summary>
    /// leverages directionBonus
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetForwardBonus(Transform bodyPart)
    {
        var bonus = GetDirectionBonus(bodyPart, Vector3.forward);
        return bonus;
    }
    /// <summary>
    /// leverages directionBonus to get backfacing bonus
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetBackwardsBonus(Transform bodyPart)
    {
        var bonus = GetDirectionBonus(bodyPart, -Vector3.forward);
        return bonus;
    }

    // implement phase bonus (reward for left then right)
    List<bool> _lastSenorState = new List<bool>();
    int NumSensors = 2; //left food, right food
    

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
        var inPhaseToFocalAngle = BodyPartsToFocalRotation["Thigh_L"] * -jdController.robotBodyPartsDict[thighL].rb.transform.forward;

        //Debug.DrawRay(jdController.robotBodyPartsDict[thighL].rb.position, inPhaseToFocalAngle, Color.blue, 1);
        //Debug.DrawRay(thighL.position, inPhaseToFocalAngle, Color.yellow, 1);

        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        //Debug.Log("inPhaseAngleFromUp: " + inPhaseAngleFromUp);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;

        //Debug.Log("bonus: " + bonus + " qpos2: " + qpos2 + " angle: " + angle);

        LeftMin = Mathf.Min(LeftMin, bonus);
        LeftMax = Mathf.Max(LeftMax, bonus);
    }

    void PhaseSetRight()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRotation["Thigh_R"] * -jdController.robotBodyPartsDict[thighL].rb.transform.forward;  //quaternion initial rot * local backwards vector  -of thigh
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);    //angle from up

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1; //if aligned bonus is high, else low - negative
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


