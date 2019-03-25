using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;



public abstract class RobotAgent : Agent
{
    [Tooltip("wether the agent should be reset")]
    public bool terminateNever;

    public GameObject CameraTarget;

    [Header("Sensors")]
    public bool bothFeetDown;
    public bool isLeftFootDown;
    public bool isRightFootDown;

    [Header("The robots bodyparts")]
    public Transform head;  //rigid bp - for measuring height
    public Transform hips;
    public Transform body;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;

    

    [Tooltip("Reward value to set on termination")]
    /**< \brief Reward value to set on termination*/
    protected float OnTerminateRewardValue = -1;

    //local vars
    protected List<float> Actions;            //last action values
    protected RobotJointDriveController jdController;
    protected bool isNewDecisionStep;
    protected int currentDecisionStep;
    protected Dictionary<string, Quaternion> BodyPartsToFocalRotation = new Dictionary<string, Quaternion>();  //use this to determine the initial axis
    protected List<float> recentVelocity;
    protected Vector3 _initialPosition;
    protected bool phaseChange;

    public override void InitializeAgent()
    {
        jdController = GetComponent<RobotJointDriveController>();

        //setup camera target
        if (CameraTarget != null)
        {
            var smoothFollow = CameraTarget.GetComponent<SmoothFollow>();
            if (smoothFollow != null)
                smoothFollow.target = hips.transform;
        }

        jdController.SetupBodyPart(body);
        jdController.SetupBodyPart(hips);
        jdController.SetupBodyPart(thighL);
        jdController.SetupBodyPart(shinL);
        jdController.SetupBodyPart(footL);
        jdController.SetupBodyPart(thighR);
        jdController.SetupBodyPart(shinR);
        jdController.SetupBodyPart(footR);

        _initialPosition = hips.position;
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
            BodyPartsToFocalRotation[name] = focalPointRotation;                    //in initial condition these are all zero rotations --> rotation that is needed to point forward (z-axis)

        }

        recentVelocity = new List<float>();
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
        //Vector3 localPosRelToHips = hips.InverseTransformPoint(rb.position);
        //AddVectorObs(localPosRelToHips);
        if (bp.rb.transform != footL && bp.rb.transform != footR && bp.rb.transform != hips)
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
        
        _maxDistanceTravelled = Mathf.Max(_maxDistanceTravelled, hips.position.z);

        jdController.GetCurrentJointForces();
        AddVectorObs(hips.forward);
        AddVectorObs(hips.up);

        AddVectorObs(body.forward);
        AddVectorObs(body.up);

        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    /// <summary>
    /// used to determine if the rollout should be aborted/reset
    /// </summary>
    /// <returns></returns>
    public virtual bool TerminateRobot()
    {
        if (terminateNever)
        {
            return false;
        }
        return GetTerminationOnAngle() || GetTerminationOnHeight();
    }

    public virtual bool GetTerminationOnHeight()
    {
        var height = GetHeightPenalty(_terminationHeight);
        bool endOnHeight = height > 0f;
        return endOnHeight;
    }

    public virtual bool GetTerminationOnAngle()
    {
        var angle = GetForwardBonus(hips);
        bool endOnAngle = angle < _terminationAngle;
        return endOnAngle;
    }

    /// <summary>
    /// Apply the action vector sent by the communicator to the joints --> send back the skill reward
    /// </summary>
    /// <param name="vectorAction"></param>
    /// <param name="textAction"></param>
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        
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
            Actions = vectorAction
            .Select(x => x)
            .ToList();

            var bpDict = jdController.robotBodyPartsDict;
            int i = -1;
            bpDict[body].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
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

        AddReward(
            _reward = GetSkillReward()
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

    public virtual float GetSkillReward()
    {
        return _reward;
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void AgentReset()
    {

        foreach (var bodyPart in jdController.robotBodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        isNewDecisionStep = true;
        currentDecisionStep = 1;
    }


    #region Utilities
    [Header("Relevant learning values")]
    /// <summary>
    /// the overall reward
    /// </summary>
    public float _reward;
    /// <summary>
    /// the cumulative velocity reward since agent reset
    /// </summary>
    public float _cumulativeVelocityReward;
    /// <summary>
    /// the time alive since last reset
    /// </summary>
    public float _timeAlive;

    /// <summary>
    /// the bonus for phase regularity 
    /// </summary>
    public float _finalPhaseBonus;
    /// <summary>
    /// leverages phase bonus as penalty for both feet on ground
    /// </summary>
    public float _finalPhasePenalty;
    /// <summary>
    /// the forward velocity of given bodypart
    /// </summary>
    public float _velocity;
    /// <summary>
    /// the velocity the agent should match
    /// </summary>
    public float _targetVelocityForward;
    /// <summary>
    /// returns penalty for deviating from forward axis
    /// </summary>
    public float _deviationPenalty;
    /// <summary>
    /// the avarage velocity forward
    /// </summary>
    public float _currentVelocityForward;
    /// <summary>
    /// the forward velocity of given bodypart
    /// </summary>
    public float _velocityPenalty;
    /// <summary>
    /// the final reward for matching the targetVel
    /// </summary>
    public float _velocityReward;
    /// <summary>
    /// the bonus for holding given bodyparts upright
    /// </summary>
    public float _uprightBonus;
    /// <summary>
    /// the bonus for holding given bodypart aligned with forward axis
    /// </summary>
    public float _forwardBonus;
    /// <summary>
    /// penalty for falling below height over feet
    /// </summary>
    public float _heightPenality;
    /// <summary>
    /// penalty for moving legs irregular - max 0.5
    /// </summary>
    public float _limbPenalty;
    /// <summary>
    /// penalize joint actions at limit
    /// </summary>
    public float _jointsAtLimitPenality;
    /// <summary>
    /// penalize excessive joint actions
    /// </summary>
    public float _effortPenality;
    /// <summary>
    /// the maximum reached distance since training began
    /// </summary>
    public float _maxDistanceTravelled;
    /// <summary>
    /// the height used to terminate the agent for given skill
    /// </summary>
    public float _terminationHeight;
    /// <summary>
    /// the angle used to terminate the agent for given skill
    /// </summary>
    public float _terminationAngle;
    
    
    


    /// <summary>
    /// returns penalty for joints hitting rotation limits
    /// </summary>
    /// <param name="ignorJoints"></param>
    /// <returns></returns>
    internal float GetJointsAtLimitPenality(string[] ignorJoints = null)
    {
        int atLimitCount = 0;
        for (int i = 0; i < Actions.Count; i++)
        {
            if (i >= jdController.robotBodyPartsList.Count)
                continue; // handle case when to many actions
            var name = jdController.robotBodyPartsList[i].jointName;//MarathonJoints[i].JointName;
            if (ignorJoints != null && ignorJoints.Contains(name))
                continue;
            bool atLimit = Mathf.Abs(Actions[i]) >= 1f;
            if (atLimit)
                atLimitCount++;
        }
        float penality = atLimitCount * 0.2f;
        return (float)penality;
    }

    /// <summary>
    /// returns penalty for excessive joint actions
    /// </summary>
    /// <param name="ignorJoints"></param>
    /// <returns></returns>
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
    /// returns the forward velocity (meters per second) of a given bodypart. Divided by maxspeed = 4
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetVelocity(Transform bodyPart = null)
    {
        var dt = Time.fixedDeltaTime;
        float rawVelocity = 0f;
        if (bodyPart != null)
            rawVelocity = jdController.robotBodyPartsDict[bodyPart].rb.velocity.z;
        else
            rawVelocity = jdController.robotBodyPartsDict[hips].rb.velocity.z;  //velocity in meters per seconds
        var velocity = rawVelocity;

        return velocity;
    }

    /// <summary>
    /// Get Average velocity of last 10 steps
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <returns></returns>
    internal float GetAverageVelocity(Transform bodyPart = null)
    {
        var v = GetVelocity(bodyPart);
        recentVelocity.Add(v);
        if (recentVelocity.Count >= 10)
            recentVelocity.RemoveAt(0);
        return recentVelocity.Average();
    }

    /// <summary>
    /// if deviation >= threshold return deviation from forward axis
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    internal float GetAxisDeviation(Vector3 position, float threshold = 0f)
    {
        _deviationPenalty = 0;
        //initial position == hips.position
        _deviationPenalty = Mathf.Abs(_initialPosition.x - position.x);
        _deviationPenalty = _deviationPenalty >= threshold || _deviationPenalty <= -threshold ? _deviationPenalty : 0;
        return _deviationPenalty;
    }


    /// <summary>
    /// returns float penalty for body falling below maxheight over feet
    /// </summary>
    /// <param name="maxHeight"></param>
    /// <returns></returns>
    internal float GetHeightPenalty(float maxHeight)
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
        var feetYpos = jdController.robotBodyPartsDict          //ordered list from lowest to highest foot
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
        var angleFromUp = Vector3.Angle(toFocalAngle, Vector3.up);      //check angle between bodypart local up and world up
        var qpos2 = (angleFromUp % 180) / 180;  //normalize the rotation angle
        var uprightBonus = 0.5f * (2 - (Mathf.Abs(qpos2) * 2) - 1);
        return uprightBonus;
    }

    /// <summary>
    /// /// returns direction reward between -.5 and .5; positive if the angle between given bodypart and direction axis is sharp
    /// </summary>
    /// <param name="bodyPart"></param>
    /// <param name="direction"></param>
    /// <param name="maxBonus"></param>
    /// <returns></returns>
    internal float GetDirectionBonus(Transform bodyPart, Vector3 direction, float maxBonus = 0.5f)
    {
        var toFocalAngle = BodyPartsToFocalRotation[bodyPart.name] * bodyPart.forward;

        var angle = Vector3.Angle(toFocalAngle, direction);
        var qpos2 = (angle % 180) / 180;

        var bonus = maxBonus * (2 - (Mathf.Abs(qpos2) * 2) - 1);
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

    #region PhaseBonus calculation

    List<bool> _lastSensorState = new List<bool>();
    int NumSensors = 2; //left food, right food

    /// <summary>
    /// the bonus for one phase cicle
    /// </summary>
    public float _phaseBonus;
    /// <summary>
    /// the actual phase - left=1 right=2
    /// </summary>
    public int _phase;
    public float LeftMin;
    public float LeftMax;

    public float RightMin;
    public float RightMax;


    /// <summary>
    /// initialize on agent reset/ Done
    /// </summary>
    internal void PhaseBonusInitalize()
    {
        _lastSensorState = Enumerable.Repeat<bool>(false, NumSensors).ToList();
        _phase = 0;
        _phaseBonus = 0f;
        PhaseResetLeft();
        PhaseResetRight();
    }

    internal void PhaseResetLeft()
    {
        LeftMin = float.MaxValue;
        LeftMax = float.MinValue;
        PhaseSetLeft();
    }

    internal void PhaseResetRight()
    {
        RightMin = float.MaxValue;
        RightMax = float.MinValue;
        PhaseSetRight();
    }

    /// <summary>
    /// set bonus for leg angle in phase/ if aligned with upAxis bonus is high, else low - negative
    /// </summary>
    internal void PhaseSetLeft()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRotation["Thigh_L"] * -jdController.robotBodyPartsDict[thighL].rb.transform.forward;
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1;

        LeftMin = Mathf.Min(LeftMin, bonus);
        LeftMax = Mathf.Max(LeftMax, bonus);
    }

    /// <summary>
    /// set bonus for leg angle in phase/ if leg is in phase (aligned with upAxis) bonus is high, else low - negative
    /// </summary>
    internal void PhaseSetRight()
    {
        var inPhaseToFocalAngle = BodyPartsToFocalRotation["Thigh_R"] * -jdController.robotBodyPartsDict[thighL].rb.transform.forward;  //quaternion initial rot * local backwards vector  -of thigh
        var inPhaseAngleFromUp = Vector3.Angle(inPhaseToFocalAngle, Vector3.up);    //angle from up

        var angle = 180 - inPhaseAngleFromUp;
        var qpos2 = (angle % 180) / 180;
        var bonus = 2 - (Mathf.Abs(qpos2) * 2) - 1; 

        RightMin = Mathf.Min(RightMin, bonus);
        RightMax = Mathf.Max(RightMax, bonus);
    }

    /// <summary>
    /// return the phase bonus
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal float CalcPhaseBonus(float min, float max)
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
    
    internal float GetPhaseBonus()
    {
        bool noPhaseChange = true;
        bool wasLeftFootDown = _lastSensorState[0];
        bool wasRightFootDown = _lastSensorState[1];
        noPhaseChange = noPhaseChange && isLeftFootDown == wasLeftFootDown;
        noPhaseChange = noPhaseChange && isRightFootDown == wasRightFootDown;
        phaseChange = !noPhaseChange;
        bothFeetDown = isLeftFootDown && isRightFootDown;
        _lastSensorState[0] = isLeftFootDown;
        _lastSensorState[1] = isRightFootDown;
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
            _phaseBonus *= 0.9f;    //lower reward if phase doesnt change
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
    #endregion
    #endregion
}


