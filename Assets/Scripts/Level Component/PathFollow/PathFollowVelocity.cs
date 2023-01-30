using System;
using UnityEditor;
using UnityEngine;

namespace Level_Component.PathFollow {
    // This implementation is not used for now
    public class PathFollowVelocity : PathFollow {
        // requires rigidbody2D
        protected Rigidbody2D Rb;

        protected override void Start() {
            base.Start();
            Rb = GetComponent<Rigidbody2D>();
            if (Rb == null)
                throw new Exception("Add a rigidbody2D to me!");
        }
#if UNITY_EDITOR
        [CustomEditor(typeof(PathFollowVelocity))]
        public class PathFollowVelocityEditor : PathFollowEditor {
        }
#endif
        protected override float MoveAngular(float speed) {
            Vector3 pos = transform.position;
            if (debugRays) {
                Debug.DrawRay(pos, Vector2.Perpendicular(pos - CurvePoint));
            }

            Vector2 direction = Vector2.Perpendicular(pos - CurvePoint).normalized;
            Rb.velocity = direction * speed;
            return Vector3.Distance(pos, CurveDestination);
        }

        protected override float MoveLinear(float speed) {
            Vector3 pos = transform.position;
            Vector2 direction = ((ShiftedPosition + points[Index]) - pos).normalized;
            Rb.velocity = direction * speed;
            return Vector3.Distance(pos, ShiftedPosition + points[Index]);
        }
    }
}