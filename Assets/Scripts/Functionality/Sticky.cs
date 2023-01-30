using System;
using Player_States;
using UnityEngine;

namespace Functionality {
    // carries player relative to this object's location, doesn't apply any relative speed to player
    public class Sticky : MonoBehaviour {
        // for cling functionality
        public static Action OnClungStickSurface;
        private bool _wasClinging;

        protected PlayerMovement Movement;

        // object's prev loc. not player's
        protected Vector3 PrevLocation;

        // if true deactivationTimer starts else deactivationTimer gets a reset
        protected bool NoCollision;

        protected Transform OtherTf;

        // needed since rapid changes in velocity may disconnect the player from the object
        // so we have to wait a little to make sure player really left
        protected float DeactivationTimer;
        protected bool Active;

        private void Start() {
            // we should get player movement class from scene manager but right now, we don't have one
            // TODO replace
            Movement = FindObjectOfType<PlayerMovement>();
            if (Movement == null)
                throw new Exception("Couldn't find a player movement instance");
            NoCollision = true;
            DeactivationTimer = -1f;
            Active = false;
        }

        private void OnTriggerEnter2D(Collider2D col) {
            if (col.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            PrevLocation = transform.position;
            OtherTf = col.gameObject.transform;
            Vector3 relativePosition = OtherTf.position - PrevLocation;
            // to prevent sticking from side
            NoCollision = false;
            if (Math.Abs(relativePosition.normalized.x) < 0.6f)
                Init();
            else if (Movement.Conditions.IsClingingWall)
                ClingInit();
            else
                OnClungStickSurface += ClingInit;
        }

        private void Init() {
            PrevLocation = transform.position;
            Active = true;
            // In fixed update, delta time is always 0.02 regardless of the fps.
            // So lowering this variable won't affect anything
            DeactivationTimer = 0.01f;
            Movement.SetGravity(0f);
        }

        private void ClingInit() {
            _wasClinging = true;
            Init();
        }

        private void Exit() {
            NoCollision = true;
            OnClungStickSurface -= ClingInit;
        }

        protected virtual void FixedUpdate() {
            if (DeactivationTimer > 0f) {
                if (NoCollision) {
                    DeactivationTimer -= Time.deltaTime;
                }

                if (_wasClinging && !Movement.Conditions.IsClingingWall) {
                    Exit();
                    return;
                }

                Vector3 position = transform.position;
                Vector3 direction = position - PrevLocation;
                OtherTf.position += direction;
                PrevLocation = position;
            }
            else if (Active) {
                Active = false;
                Movement.SetGravity();
            }
        }


        protected virtual void OnTriggerExit2D(Collider2D other) {
            if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            Exit();
            _wasClinging = false;
        }
    }
}