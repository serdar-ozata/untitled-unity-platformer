using System;
using System.Collections.Generic;
using Functionality;
using Level_Component.Hittable;
using UnityEngine;

namespace Player_States {
    public class PlayerAttack : PlayerStates {
        [Header("Sword sticking")] public GameObject swordStuckPrefab;
        [Header("Attack Points")] public Transform sidePoint;
        public Transform topPoint;
        public Transform bottomPoint;
        [Header("Attack Check Lengths")] public float sideLength;
        public float topLength;
        public float bottomLength;
        private const float CheckWidth = 0.2f;

        // for optimization purposes, this value is hard coded
        // TODO - change this value once we make the stuck sword sprite
        private const float SwordExtentX = 1f;
        private float _footToBodyDistance;
        private GameObject _sword;

        protected override void InitState() {
            base.InitState();
            _sword = null;
            Collider2D playerCol = GetComponent<Collider2D>();
            _footToBodyDistance = playerCol.bounds.extents.y;
        }

        public override void ExecuteState() {
        }

        protected override void GetInput() {
            if (PlayerController.Conditions.OnInteraction) return;
            if (_sword is null) {
                if (Input.GetButtonDown("Attack") &&
                    !(PlayerController.Conditions.IsSwinging || PlayerController.Conditions.IsDashing)) {
                    // clinging
                    if (PlayerController.Conditions.IsClingingWall) {
                        switch (VerticalInput) {
                            case > 0.1f:
                                ProcessTop();
                                break;
                            case < -0.1f:
                                ProcessBottom();
                                break;
                            default:
                                ProcessNonFacedSide();
                                break;
                        }
                    } // on ground
                    else if (PlayerController.Conditions.OnGround) {
                        switch (VerticalInput) {
                            case > 0.1f:
                                ProcessTop();
                                break;
                            default:
                                ProcessSide();
                                break;
                        }
                    }
                    else { // on air
                        switch (VerticalInput) {
                            case > 0.1f:
                                ProcessTop();
                                break;
                            case < -0.1f:
                                ProcessBottom();
                                break;
                            default:
                                ProcessAirSide();
                                break;
                        }
                    }
                }
            }
            else {
                PlayerController.Conditions.OnStuckSword = PlayerController.IsOnSword();
                if (Input.GetButtonDown("Interact") && !PlayerController.Conditions.IsThereInteractable) {
                    PlayerController.Conditions.OnStuckSword = false;
                    CallSword();
                }
            }
        }

        private void CallSword() {
            // start sword calling animation
            Destroy(_sword);
            _sword = null;
        }

        private void HitAll(List<Collider2D> cols, bool onAir = false) {
            foreach (Collider2D col in cols) {
                IHittable entity = col.GetComponent<IHittable>();
                if (entity is null) return;
                switch (col.gameObject.tag, onAir) {
                    // Add canStick tag to objects that we want to stick into
                    case ("CanStick", true):
                        StickWithSword(col);
                        break;
                    case ("DashRecharger", _):
                        PlayerController.Conditions.IsDashed = false;
                        break;
                    case ("Enemy", _):
                        break;
                }

                entity.Hit(col.ClosestPoint(transform.position));
            }
        }

        private void StickWithSword(Collider2D col) {
            Vector3 colPos = col.transform.position;
            Vector3 pos = transform.position;
            Vector3 direction = colPos - pos;
            bool isColInRight = direction.x > Mathf.Epsilon;
            // set variables according to direction
            if (isColInRight != PlayerController.Conditions.IsFacingRight) return;
            float edgeX = isColInRight ? col.bounds.min.x : col.bounds.max.x;
            Quaternion rotation = isColInRight ? Quaternion.identity : Quaternion.AngleAxis(180f, Vector3.forward);
            Vector3 prefabPos = new Vector3(edgeX + (isColInRight ? -SwordExtentX : SwordExtentX),
                pos.y - _footToBodyDistance);

            // check if the position is valid
            // we may either discard the action or relocate prefabPos
            Bounds colBounds = col.bounds;
            float colMinY = colBounds.min.y;
            float colMaxY = colBounds.max.y;
            // return (discard)
            if (colMaxY < prefabPos.y || prefabPos.y < colMinY) return;

            _sword = Instantiate(swordStuckPrefab, prefabPos, rotation);
            Collider2D swordCol = _sword.GetComponent<Collider2D>();
            var swordBounds = swordCol.bounds;
            float swordCenterDistY = swordBounds.extents.y;
            // TODO - start some animations
            // user should be teleported above the prefab eventually
            // I'm doing it right after the instantiation but it should be done after the animations
            transform.position = new Vector3(prefabPos.x, prefabPos.y + swordCenterDistY + _footToBodyDistance);
            PlayerController.StopMovement();
        }


        private void ProcessNonFacedSide() {
            var position = sidePoint.position;
            Vector2 oppositeSide = new Vector2(2 * transform.position.x - position.x, position.y);
            var col = PlayerController.CheckBoxCollision(oppositeSide, new Vector2(CheckWidth, sideLength));
            HitAll(col);
        }

        private void ProcessSide() {
            var col = PlayerController.CheckBoxCollision(sidePoint.position, new Vector2(CheckWidth, sideLength));
            HitAll(col);
        }

        private void ProcessAirSide() {
            var col = PlayerController.CheckBoxCollision(sidePoint.position, new Vector2(CheckWidth, sideLength));
            HitAll(col, true);
        }

        private void ProcessTop() {
            var col = PlayerController.CheckBoxCollision(topPoint.position, new Vector2(topLength, CheckWidth));
            HitAll(col);
        }

        private void ProcessBottom() {
            var col = PlayerController.CheckBoxCollision(bottomPoint.position, new Vector2(bottomLength, CheckWidth));
            HitAll(col);
        }
    }
}