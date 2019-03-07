using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MLAgents
{

    /// <summary>
    /// Used to store relevant information for acting and learning for each body part in agent.
    /// </summary>
    [System.Serializable]
    public class RobotBodyPart
    {
        [Header("Body Part Info")] [Space(10)] public ConfigurableJoint joint;
        public Rigidbody rb;
        [HideInInspector] public Vector3 startingPos;
        [HideInInspector] public Quaternion startingRot;

        [Header("Ground & Target Contact")]
        [Space(10)]
        public GroundContact groundContact;
        public TargetContact targetContact;

        [HideInInspector] public RobotJointDriveController thisRobotJDController;

        [Header("Current Joint Settings")]
        [Space(10)]
        public Vector3 currentEularJointRotation;

        [HideInInspector] public float currentStrength;
        public float currentXNormalizedRot;
        public float currentYNormalizedRot;
        public float currentZNormalizedRot;

        [Header("Other Debug Info")]
        [Space(10)]
        public Vector3 currentJointForce;

        public float currentJointForceSqrMag;
        public Vector3 currentJointTorque;
        public float currentJointTorqueSqrMag;
        public AnimationCurve jointForceCurve = new AnimationCurve();
        public AnimationCurve jointTorqueCurve = new AnimationCurve();

        /// <summary>
        /// Reset body part to initial configuration.
        /// </summary>
        public void Reset(RobotBodyPart bp)
        {
            bp.rb.transform.position = bp.startingPos;
            bp.rb.transform.rotation = bp.startingRot;
            bp.rb.velocity = Vector3.zero;
            bp.rb.angularVelocity = Vector3.zero;
            if (bp.groundContact)
            {
                bp.groundContact.touchingGround = false;
            }

            if (bp.targetContact)
            {
                bp.targetContact.touchingTarget = false;
            }
        }

        /// <summary>
        /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
        /// </summary>
        public void SetJointTargetRotation(float x, float y, float z)
        {
            x = (x + 1f) * 0.5f;
            y = (y + 1f) * 0.5f;
            z = (z + 1f) * 0.5f;

            var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
            var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
            var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);

            currentXNormalizedRot = Mathf.InverseLerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, xRot);//float Percentage -
            currentYNormalizedRot = Mathf.InverseLerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, yRot);//of value between start and end.
            currentZNormalizedRot = Mathf.InverseLerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, zRot);

            joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
            currentEularJointRotation = new Vector3(xRot, yRot, zRot);
        }

        public void SetJointStrength(float strength)
        {
            var rawVal = (strength + 1f) * 0.5f * thisRobotJDController.maxJointForceLimit;
            var jd = new JointDrive
            {
                positionSpring = thisRobotJDController.maxJointSpring,
                positionDamper = thisRobotJDController.jointDampen,
                maximumForce = rawVal
            };
            joint.slerpDrive = jd;
            currentStrength = jd.maximumForce;
        }
    }

    public class RobotJointDriveController : MonoBehaviour
    {
        [Header("Joint Drive Settings")]
        [Space(10)]
        public float maxJointSpring;

        public float jointDampen;
        public float maxJointForceLimit;
        float facingDot;

        [HideInInspector] public Dictionary<Transform, RobotBodyPart> robotBodyPartsDict = new Dictionary<Transform, RobotBodyPart>();

        [HideInInspector] public List<RobotBodyPart> robotBodyPartsList = new List<RobotBodyPart>();

        /// <summary>
        /// Create BodyPart object and add it to dictionary.
        /// </summary>
        public void SetupBodyPart(Transform t)
        {
            RobotBodyPart bp = new RobotBodyPart
            {
                rb = t.GetComponent<Rigidbody>(),
                joint = t.GetComponent<ConfigurableJoint>(),
                startingPos = t.position,
                startingRot = t.rotation
            };
            bp.rb.maxAngularVelocity = 100;

            // Add & setup the ground contact script
            bp.groundContact = t.GetComponent<GroundContact>();
            if (!bp.groundContact)
            {
                bp.groundContact = t.gameObject.AddComponent<GroundContact>();
                bp.groundContact.agent = gameObject.GetComponent<Agent>();
            }
            else
            {
                bp.groundContact.agent = gameObject.GetComponent<Agent>();
            }

            // Add & setup the target contact script
            bp.targetContact = t.GetComponent<TargetContact>();
            if (!bp.targetContact)
            {
                bp.targetContact = t.gameObject.AddComponent<TargetContact>();
            }

            bp.thisRobotJDController = this;
            robotBodyPartsDict.Add(t, bp);
            robotBodyPartsList.Add(bp);
        }

        public void GetCurrentJointForces()
        {
            foreach (var bodyPart in robotBodyPartsDict.Values)
            {
                if (bodyPart.joint)
                {
                    bodyPart.currentJointForce = bodyPart.joint.currentForce;
                    bodyPart.currentJointForceSqrMag = bodyPart.joint.currentForce.magnitude;
                    bodyPart.currentJointTorque = bodyPart.joint.currentTorque;
                    bodyPart.currentJointTorqueSqrMag = bodyPart.joint.currentTorque.magnitude;
                    if (Application.isEditor)
                    {
                        if (bodyPart.jointForceCurve.length > 1000)
                        {
                            bodyPart.jointForceCurve = new AnimationCurve();
                        }

                        if (bodyPart.jointTorqueCurve.length > 1000)
                        {
                            bodyPart.jointTorqueCurve = new AnimationCurve();
                        }

                        bodyPart.jointForceCurve.AddKey(Time.time, bodyPart.currentJointForceSqrMag);
                        bodyPart.jointTorqueCurve.AddKey(Time.time, bodyPart.currentJointTorqueSqrMag);
                    }
                }
            }
        }
    }
}


