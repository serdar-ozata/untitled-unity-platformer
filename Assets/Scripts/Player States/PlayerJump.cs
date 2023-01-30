using System;
using Level_Component;
using UnityEngine;

namespace Player_States {
    public class PlayerJump : PlayerStates {
        public float jumpForce;
        public float fallJumpTimer;
        public float earlyJumpTimer = 0.04f;
        public float fallGravityMultiplier = 0.2f;
        public float jumpHoldTime;
        public float airJumpMult = 14f;
        [Header("Wall Jump")] public float wallJumpAngle;
        public float wallJumpForce;

        [Header("Jump Pad Movement Multiplier")]
        public float padSpeedLimiter = 0.8f;

        public float padOppositeSpeedLimiter = 0.8f;
        [Header("Sword Jump")] public float swordJumpForceMultiplier = 1.2f;

        private bool _jumpButtonReleased;
        private float _countdownFall;
        private float _jumpTimer;
        private float _earlyJumpCountdown;
        private bool _prevClung;
        private FallTimerOrigin _fallTimerOrigin;

        private PlayerMovement _playerMovement;
        private enum FallTimerOrigin {
            WALL_JUMP,
            EDGE_JUMP,
            NONE
        }

        protected override void InitState() {
            base.InitState();
            _jumpButtonReleased = true;
            _countdownFall = 0f;
            _jumpTimer = 0f;
            _prevClung = false;
            _fallTimerOrigin = FallTimerOrigin.NONE;
        }

        public override void ExecuteState() {
            // jump functionality
            if (_earlyJumpCountdown > Mathf.Epsilon && !PlayerController.Conditions.IsJumping) {
                // normal jump - on ground or near edge or swinging
                if (PlayerController.Conditions.OnGround || _fallTimerOrigin == FallTimerOrigin.EDGE_JUMP ||
                    PlayerController.Conditions.IsSwinging) {
                    PlayerController.Conditions.IsSwinging = false;
                    PlayerController.Jump(jumpForce);
                    PlayerController.Conditions.IsJumping = true;
                    _jumpButtonReleased = false;
                    _jumpTimer = Time.deltaTime;
                } // wall jump - clinging wall or just left the wall
                else if (PlayerController.Conditions.IsClingingWall || _fallTimerOrigin == FallTimerOrigin.WALL_JUMP) {
                    PlayerController.Conditions.IsClingingWall = false;
                    PlayerController.Conditions.IsJumping = true;
                    PlayerController.RevertGravity();
                    float xCord = Mathf.Tan(wallJumpAngle * Mathf.PI / 180f);
                    if (PlayerController.Conditions.IsFacingRight)
                        xCord *= -1f;
                    if (_fallTimerOrigin == FallTimerOrigin.WALL_JUMP)
                        xCord *= -1f;
                    Vector2 ray = new Vector2(xCord, 1f).normalized * wallJumpForce;
                    PlayerController.VectorJump(ray);
                } // Sword jump - on sword
                else if (PlayerController.Conditions.OnStuckSword) {
                    PlayerController.Jump(jumpForce * swordJumpForceMultiplier);
                    _jumpButtonReleased = false;
                    // we should reset _jumpTimer if we want to enable jump hold in sword jump
                    //_jumpTimer = Time.deltaTime;
                    PlayerController.Conditions.IsJumping = true;
                }
            }

            _earlyJumpCountdown -= Time.deltaTime;
            _prevClung = PlayerController.Conditions.IsClingingWall;
        }

        protected override void GetInput() {
            bool prevOnGround = PlayerController.Conditions.OnGround;
            PlayerController.Conditions.OnGround = PlayerController.isOnGround();
            if (PlayerController.Conditions.OnGround) {
                PlayerController.Conditions.IsDashed = false;
            }

            // late jump is possible from from a wall or an edge
            if (prevOnGround && !PlayerController.Conditions.OnGround) {
                _fallTimerOrigin = FallTimerOrigin.EDGE_JUMP;
                _countdownFall = fallJumpTimer;
            }
            else if (_prevClung && !PlayerController.Conditions.IsClingingWall) {
                _fallTimerOrigin = FallTimerOrigin.WALL_JUMP;
                _countdownFall = fallJumpTimer;
            }

            if (_fallTimerOrigin != FallTimerOrigin.NONE) {
                _countdownFall -= Time.deltaTime;
                if (_countdownFall < 0) {
                    _countdownFall = 0;
                    _fallTimerOrigin = FallTimerOrigin.NONE;
                }
            }

            // jumps more if you hold
            if (Input.GetButtonUp("Jump")) {
                _jumpButtonReleased = true;
            }

            if (!_jumpButtonReleased) {
                _jumpTimer += Time.deltaTime;
                if (_jumpTimer < jumpHoldTime)
                    PlayerController.JumpHold(airJumpMult * Mathf.Pow((1f - _jumpTimer / jumpHoldTime), 2f));
            }
            else if (PlayerController.Conditions.OnGround || PlayerController.Conditions.IsSwinging
                                                          || PlayerController.Conditions.IsClingingWall
                                                          || PlayerController.Conditions.OnStuckSword) {
                PlayerController.Conditions.IsJumping = false;
            }

            if (Input.GetButtonDown("Jump"))
                _earlyJumpCountdown = earlyJumpTimer;
        }

        private void JumpPadResponse(Vector2 padForce) {
            Vector2 forceNormalized = padForce.normalized;
            _jumpButtonReleased = true;
            PlayerController.MultiplyMovement(padOppositeSpeedLimiter, padSpeedLimiter, forceNormalized);
            PlayerController.VectorJump(padForce);
            PlayerController.Conditions.IsJumping = true;
            PlayerController.Conditions.IsDashed = false;
        }

        private void OnEnable() {
            // OnJump action will invoke the function above 
            JumpPad.OnJump += JumpPadResponse;
        }

        private void OnDisable() {
            // desubscribe to not get errors when disabling this class
            JumpPad.OnJump -= JumpPadResponse;
        }
    }
}