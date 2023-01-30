using System.ComponentModel;
using UnityEngine;

namespace Player_States {
    public class PlayerSwing : PlayerStates {
        // swing
        [Header("Swing Anchor Check")]
        [Tooltip("Sets how many vectors there should be in one half to check the swing collision")]
        public int halfSwingVecCount = 12;

        [Tooltip("Max swing distance")] public float swingVecLength = 10f;
        [Tooltip("Min swing distance")] public float minSwingVecLength = 1f;

        [Space] [Tooltip("Making it higher than 30 is not advised")]
        public float maxSwingAngle = 30;

        // Scarf Length
        [Header("Length Settings")] [Range(0, 89)]
        public float minStiffnessAngle;

        [Range(0f, 0.1f)] public float stiffnessMultiplier;
        [Range(0, 89)] public float minLoosenessAngle;
        [Range(0f, 0.1f)] public float loosenessMultiplier;

        // swing
        [Header("Swing")] [Tooltip("k of the scarf in F=kx")]
        public float scarfForceMultiplier;

        [Tooltip("To start the pulling force of the scarf early")]
        public float earlyForceLength;


        // movement
        public float moveForce;
        public float forceVelocityLimit;

        // Swing debug
        [Header("Debug")] public bool showSwingCheckRays = true;

        public float checkRayDuration = 4f;
        public bool showSwingRay = false;

        [Header("Do not touch")]
        [SerializeField]
        [ReadOnly(true)]
        // private variables
        // ReSharper disable once InconsistentNaming
        private float _scarfLength;

        private Vector2 _swingPoint;

        protected override void InitState() {
            base.InitState();
            _swingPoint = Vector2.zero;
            _scarfLength = 0f;
        }

        public override void ExecuteState() {
            
        }

        protected override void GetInput() {
            if (PlayerController.Conditions.IsSwinging) {
                // if scarf length is not calculated calculate else swing 
                if (_scarfLength < 0.001f) 
                    _scarfLength = PlayerController.CalculateScarfLength(_swingPoint, minStiffnessAngle
                        , stiffnessMultiplier, minLoosenessAngle, loosenessMultiplier, minSwingVecLength);
                else
                    PlayerController.Swing(_swingPoint, _scarfLength, scarfForceMultiplier, showSwingRay,
                        earlyForceLength, HorizontalInput * moveForce
                        , forceVelocityLimit);
                // temp
                if (Input.GetButtonDown("Interact")) {
                    PlayerController.Conditions.IsSwinging = false;
                }
                // end temp
            }
            else {
                if (Input.GetButtonDown("Interact")) {
                    if (PlayerController.Conditions.OnGround || PlayerController.Conditions.IsThereInteractable) return;
                    _swingPoint = PlayerController.FindSwingPoint(maxSwingAngle, halfSwingVecCount, swingVecLength,
                        checkRayDuration, showSwingCheckRays);
                    if (_swingPoint.magnitude > 0.001f) {
                        // disable dashing and restore gravity
                        if (PlayerController.Conditions.IsDashing) {
                            PlayerController.Conditions.IsDashing = false;
                            PlayerController.RevertGravity();
                        }
                        PlayerController.Conditions.IsDashed = false;
                        PlayerController.Conditions.IsSwinging = true;
                        _scarfLength = 0f;
                        // We should probably wait here for a while for an animation
                        // Then we make the real connection
                    }
                }
            }
        }
    }
}