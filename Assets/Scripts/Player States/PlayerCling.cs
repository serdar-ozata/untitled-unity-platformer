using Functionality;
using UnityEngine;
using UnityEngine.UI;

namespace Player_States {
    public class PlayerCling : PlayerStates {
        public float clingGravity = 0f;
        public float slideDownSpeed = 3f;
        public float slideUpSpeed = 4f;
        public Vector2 relativeClimbDestination = new(0, 0);
        private bool _clungRight;

        protected override void InitState() {
            base.InitState();
            _clungRight = true;
        }

        public override void ExecuteState() {
           
        }

        protected override void GetInput() {
            if ( PlayerController.Conditions.OnGround) {
                Drop();
            }
            if (PlayerController.Conditions.OnGround || PlayerController.Conditions.IsSwinging
                // || PlayerController.Conditions.IsClimbing
                ) return;

            bool clinging = PlayerController.isClinging();

            if (PlayerController.Conditions.IsClingingWall) {
                // != => xor
                bool facingMismatch = PlayerController.Conditions.IsFacingRight != _clungRight;

                if (facingMismatch) {
                    PlayerController.Conditions.IsSliding = false;
                    PlayerController.Conditions.IsClimbing = false;
                    Drop();
                }
                else if (!clinging) {
                    PlayerController.Conditions.IsSliding = false;
                    PlayerController.Conditions.IsClimbing = false;
                    Drop();
                    // waiting for climb animation
                    if (VerticalInput > 0.1f)
                        Climb();
                }
                else if (VerticalInput < -0.01f) {
                    PlayerController.Conditions.IsClimbing = false;
                    PlayerController.Conditions.IsSliding = true;
                    PlayerController.SlideWall(VerticalInput * slideDownSpeed);
                }
                else if ( VerticalInput > 0.1f) {
                    //Climb();
                    PlayerController.Conditions.IsSliding = true;
                    PlayerController.SlideWall(VerticalInput * slideUpSpeed);
                }
                else {
                    // it does!
                    //Debug.Log("check if this else gets triggered");
                    PlayerController.Conditions.IsSliding = false;
                    PlayerController.Conditions.IsClimbing = false;
                    PlayerController.SlideWall(VerticalInput * slideUpSpeed);
                }
            }
            else if (clinging) {
                if ((HorizontalInput > 0.1f && PlayerController.Conditions.IsFacingRight) ||
                    (HorizontalInput < -0.1f && !PlayerController.Conditions.IsFacingRight)) {
                    PlayerController.Conditions.IsClingingWall = true;
                    _clungRight = PlayerController.Conditions.IsFacingRight;
                    PlayerController.SetGravity(clingGravity);
                    PlayerController.StopMovement();
                    Sticky.OnClungStickSurface?.Invoke();
                }
                PlayerController.Conditions.IsSliding = false;
                PlayerController.Conditions.IsClimbing = false;
                // change the animation
            }
        }

        private const float ClimbHorizontalDisplacement = 0.8f;
        private const float ClimbVerticalDisplacement = 0.9f;

        private void Climb() {
            PlayerController.Conditions.IsClimbing = true;
            float deltaX;
            if (PlayerController.Conditions.IsFacingRight)
                deltaX = ClimbHorizontalDisplacement;
            else
                deltaX = -ClimbHorizontalDisplacement;
            
            PlayerController.MoveByTranslation(new(deltaX, ClimbVerticalDisplacement));
        }

        private void Drop() {
            PlayerController.Conditions.IsClingingWall = false;
            PlayerController.RevertGravity();
            // also change the animation
        }
    }
}