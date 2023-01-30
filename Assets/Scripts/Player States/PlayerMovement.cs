using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Level_Component;
using UnityEngine;

namespace Player_States {
    public class PlayerMovement : PlayerStates {
        [Header("Horizontal Movement")] public float moveSpeed;
        public float velocityPower;
        public float acceleration;
        public float deceleration;
        public float airAcceleration;
        public float frictionAmount;
        public float maxSpeedAccelerationMultiplier = 0.1f;

        [Header("Dash")] public float dashForce;
        public float maxDashDuration;
        public float dashForceDirect;
        public float dashAfterDuration;
        public float dashMovementReduceMultiplier = 0.4f;

        [Tooltip("Velocity is used to calculate the dash force. You can increase that velocity from here")]
        public float extraVelocity;

        public float airFriction = 0.9f;

        [Header("Fan Control")] [SerializeField]
        private float terminalVelocity = 20f;

        [Header("Sword Sticking")] public float swordFriction = 1.4f;
        public float swordMoveSpeed = 4f;
        [SerializeField] private float terminalMultiplier = 0.3f;
        private float _dashDuration;
        private float _dashTimerWait;
        private const float DashForcePower = 0.2f;
        private Vector2 _relativeSpeed;
        private bool _dashDelay;
        private float _movementLimit = 2f;
        private float _moveLimitDuration = Mathf.Epsilon;
        public PlayerConditions Conditions => PlayerController.Conditions;

        public static Action<float, float> OnMovementLimit;
        protected override void InitState() {
            base.InitState();
            _dashDuration = 0f;
            _dashTimerWait = 0f;
            _dashDelay = true;
            _relativeSpeed = Vector2.zero;
            PlayerController.maxSpeed = moveSpeed;
        }

        public override void ExecuteState() {
            if (!_dashDelay) {
                _dashTimerWait -= Time.deltaTime;
                if (_dashTimerWait < 0) {
                    _dashDelay = true;
                }
            }

            if (Conditions.IsDashing) {
                if (_dashDelay) {
                    _dashDelay = false;
                }

                _dashDuration -= Time.deltaTime;
                if (_dashDuration < 0) {
                    Conditions.IsDashing = false;
                    if (!PlayerController.Conditions.IsClingingWall) {
                        PlayerController.RevertGravity();
                        PlayerController.ReduceMovement(dashMovementReduceMultiplier);
                    }
                }
            }
            else if (Conditions.OnStuckSword) {
                // MovePlayer(HorizontalInput * swordMoveSpeed);
                if (Mathf.Abs(HorizontalInput) < 0.01f) {
                    PlayerController.GroundFriction(swordFriction, _relativeSpeed.x);
                }
            }
            else if (!Conditions.IsSwinging) {
                MovePlayer(HorizontalInput * moveSpeed);
                if (Mathf.Abs(HorizontalInput) < 0.01f && Conditions.OnGround) {
                    PlayerController.GroundFriction(frictionAmount, _relativeSpeed.x);
                }

                if (_relativeSpeed.magnitude > 0.01f)
                    ApplyRelativeSpeed();
            }

            PlayerController.ChangeDirection(_relativeSpeed.x);
            PlayerController.SetSpeedAnimation(_relativeSpeed);
        }

        protected override void GetInput() {
            if (Conditions.OnInteraction) return;
            // press dash button while not (dashed , clinging wall, swinging or on ground)
            if (Input.GetButtonDown("Dash") && _dashDelay &&
                !(Conditions.IsDashed || Conditions.IsClingingWall ||
                  Conditions.IsSwinging || Conditions.OnGround ||
                  Conditions.OnStuckSword)) {
                Conditions.IsDashed = true;
                Conditions.IsDashing = true;
                _dashDuration = maxDashDuration;
                _dashTimerWait = dashAfterDuration;
                //float velocity = Mathf.Abs(PlayerController.GetVelocity().x) + extraVelocity;
                PlayerController.SetGravity(0f);
                PlayerController.StopMovement();
                //float force = dashForce * Mathf.Pow(velocity, DashForcePower);
                float force = dashForceDirect;
                if (!Conditions.IsFacingRight)
                    force *= -1f;
                PlayerController.ForceHorizontal(force);
            } //go down from the platform
            else if (VerticalInput < -0.1f && !Conditions.IsClingingWall) {
                LevelManager.OnPlatformDown?.Invoke();
            }
        }

        // Moves by velocity
        private void MovePlayer(float targetSpeed) {
            if (_moveLimitDuration > 0.01f && _movementLimit < 1f) {
                targetSpeed *= 1f - _movementLimit;
                float newDuration = _moveLimitDuration - Time.deltaTime;
                _movementLimit *= newDuration / _moveLimitDuration;
                _moveLimitDuration = newDuration;
            }

            PlayerController.MoveHorizontal(targetSpeed, velocityPower,
                acceleration, deceleration, airAcceleration, maxSpeedAccelerationMultiplier, _relativeSpeed.x);
        }

        private void ApplyRelativeSpeed() {
            PlayerController.MoveByVelocity(_relativeSpeed * Time.deltaTime);
        }

        /** <summary>
         * Used for cases which the velocity addition applied continuously
         * </summary>
         */
        public void MoveByVelocity(Vector2 velocity) {
            if (Conditions.IsClingingWall)
                velocity.x = 0f;
            PlayerController.MoveByVelocity(velocity * Time.deltaTime);
        }

        /**
        * <summary>
        * Used for cases that the velocity addition is applied once
        * </summary>
        */
        public void ApplyVelocity(Vector2 velocity) {
            if (Conditions.IsClimbing || Conditions.IsDashing) return;
            PlayerController.AddVelocity(velocity);
        }

        /**
         * <summary>
         * don't include delta time in force
         * </summary>
         */
        //TODO test
        private void ApplyForce(Vector2 force) {
            Vector2 vel = PlayerController.rb.velocity;
            if (vel.magnitude > terminalVelocity) {
                force *= terminalMultiplier;
            }

            PlayerController.ApplyForce(force * Time.deltaTime);
            PlayerController.AirFriction(airFriction);
        }

        public void AddRelativeSpeed(Vector2 velocity) {
            _relativeSpeed += velocity;
            if (Mathf.Abs(_relativeSpeed.magnitude) < 0.01f)
                _relativeSpeed = Vector2.zero;
        }

        /**
         *<summary>
         * sets gravity
         * </summary>
         */
        public void SetGravity(float gravity) {
            PlayerController.SetGravity(gravity);
        }

        /**
         * <summary>
         *Reverts gravity
         * </summary>
         */
        public void SetGravity() {
            PlayerController.RevertGravity();
        }

        /**
         * <summary>
         * Set how much button presses effects player's velocity
         * </summary>
         */
        private void LimitPlayerEffectOnMovement(float multiplier, float duration) {
            _movementLimit = multiplier;
            _moveLimitDuration = duration;
        }

        private void OnEnable() {
            Fan.OnFanExposure += ApplyForce;
            OnMovementLimit += LimitPlayerEffectOnMovement;
        }

        private void OnDisable() {
            Fan.OnFanExposure -= ApplyForce;
            OnMovementLimit -= LimitPlayerEffectOnMovement;
        }
    }
}