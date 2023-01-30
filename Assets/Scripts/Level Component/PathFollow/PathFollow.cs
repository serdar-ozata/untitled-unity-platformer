using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Level_Component.PathFollow {
    public class PathFollow : MonoBehaviour {
        public float moveSpeed = 6f;
        public List<Vector3> points = new List<Vector3>();

        [HideInInspector, SerializeField] protected bool debugRays = false;
        [HideInInspector, SerializeField] protected bool directionArrows;

        [HideInInspector, SerializeField] protected bool curvedTransition = false;
        [HideInInspector, SerializeField] protected bool destroyAtEnd;
        [HideInInspector, SerializeField] protected bool teleportToFirst;
        [HideInInspector, SerializeField] protected float minDistance;
        protected const float CheckDistance = 0.05f;
        // custom inspector, see: https://www.youtube.com/watch?v=RImM7XYdeAc
#if UNITY_EDITOR
        [CustomEditor(typeof(PathFollow))]
        public class PathFollowEditor : Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();
                PathFollow pathFollow = (PathFollow)target;
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                pathFollow.debugRays = EditorGUILayout.Toggle("Rays:", pathFollow.debugRays);
                pathFollow.directionArrows = EditorGUILayout.Toggle("Arrows on Gizmos:", pathFollow.directionArrows);
                EditorGUILayout.LabelField("Ending Options", EditorStyles.boldLabel);
                pathFollow.destroyAtEnd = EditorGUILayout.Toggle("Destroy At End", pathFollow.destroyAtEnd);
                EditorGUI.BeginDisabledGroup(pathFollow.destroyAtEnd);
                pathFollow.teleportToFirst = EditorGUILayout.Toggle("Teleport To First", pathFollow.teleportToFirst);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField("Curved Transition", EditorStyles.boldLabel);
                pathFollow.curvedTransition =
                    EditorGUILayout.Toggle("Curved Transition", pathFollow.curvedTransition);
                if (!pathFollow.curvedTransition) {
                    pathFollow.minDistance = CheckDistance;
                }

                EditorGUI.BeginDisabledGroup(!pathFollow.curvedTransition);
                pathFollow.minDistance = EditorGUILayout.FloatField("Min Distance", pathFollow.minDistance);
                EditorGUI.EndDisabledGroup();
            }
        }
#endif

        protected int Index;

        // curve
        protected bool Curving;
        protected Vector3 CurvePoint;

        protected Vector3 CurveDestination;

        // For editor
        protected bool Playing;
        protected Vector3 ShiftedPosition;

        private bool _moveClockWise;

        protected virtual void Start() {
            var transform1 = transform;
            ShiftedPosition = transform1.position;
            transform1.position = ShiftedPosition + points[0];
            Index = 1;
            Playing = true;
            Curving = false;
            CurvePoint = Vector3.zero;
        }

        protected virtual void FixedUpdate() {
            if (Curving) {
                float distanceToNextPoint = MoveAngular(moveSpeed);
                if (distanceToNextPoint < CheckDistance * moveSpeed) {
                    Curving = false;
                }
            }
            else {
                float distanceToNextPoint = MoveLinear(moveSpeed);

                if (distanceToNextPoint < minDistance) {
                    if (curvedTransition) {
                        InitiateCurvedTransition();
                    }

                    IncrementIndex();
                }
            }
        }

        /**
         * <summary> returns whether game object is destroyed or not due to destroyAtEnd</summary>
         */
        protected bool IncrementIndex() {
            Index++;
            if (Index != points.Count) return false;
            if (destroyAtEnd) {
                Destroy(gameObject);
                return true;
            }
            else if (teleportToFirst) {
                Index = 0;
                transform.position = ShiftedPosition + points[0];
            }
            else {
                Index = 0;
            }

            return false;
        }

        protected void InitiateCurvedTransition() {
            int prevIndex = Index == 0 ? points.Count - 1 : Index - 1;
            int nextIndex = Index == points.Count - 1 ? 0 : Index + 1;

            Vector3 currentPositionVector = points[Index] + ShiftedPosition;
            float distance = Vector3.Distance(transform.position, currentPositionVector);
            Vector3 nextVector = distance * (points[nextIndex] - points[Index]).normalized;
            Vector3 prevVector = distance * (points[prevIndex] - points[Index]).normalized;

            float angle = Vector3.SignedAngle(nextVector, prevVector, Vector3.forward) / 2;
            _moveClockWise = angle > Mathf.Epsilon;
            float radianAngle = angle * Mathf.PI / 180f;
            Vector3 normalNextVector = new Vector3(-nextVector.y, nextVector.x, nextVector.z);
            CurvePoint = currentPositionVector + nextVector +
                         Mathf.Tan(radianAngle) * normalNextVector;
            CurveDestination = points[Index] + nextVector + ShiftedPosition;
            Curving = true;
            if (debugRays) {
                Debug.DrawLine(CurvePoint, currentPositionVector, Color.cyan, 5f);
                Debug.DrawLine(CurvePoint, CurveDestination, Color.cyan, 5f);
                Debug.DrawLine(CurvePoint, transform.position, Color.cyan, 5f);
            }
        }

        protected virtual float MoveAngular(float speed) {
            Vector3 pos = transform.position;
            if (debugRays) {
                Debug.DrawRay(pos, Vector2.Perpendicular(pos - CurvePoint));
            }

            if (!_moveClockWise) speed *= -1f;
            transform.Translate(Vector2.Perpendicular(pos - CurvePoint).normalized
                                * (Time.deltaTime * speed), Space.Self);
            return Vector3.Distance(pos, CurveDestination);
        }

        protected virtual float MoveLinear(float speed) {
            Vector3 pos = transform.position;
            transform.position = Vector3.MoveTowards(pos, ShiftedPosition + points[Index], speed * Time.deltaTime);


            return Vector3.Distance(pos, ShiftedPosition + points[Index]);
        }

        protected virtual void OnDrawGizmos() {
            if (transform.hasChanged && !Playing) {
                ShiftedPosition = transform.position;
            }

            VisualizeGizmos();
        }

        /**
         * <summary> direction is expected to be normalized</summary>
         */
        protected void DrawArrows(Vector3 position, Vector3 direction) {
            const float arrowAngle = 150f;
            Gizmos.DrawRay(position, Quaternion.AngleAxis(arrowAngle, Vector3.forward) * direction / 2f);
            Gizmos.DrawRay(position, Quaternion.AngleAxis(-arrowAngle, Vector3.forward) * direction / 2f);
        }

        protected virtual void VisualizeGizmos() {
            if (points != null) {
                for (int i = 0; i < points.Count; i++) {
                    // Draw points
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireSphere(ShiftedPosition + points[i], 0.4f);

                    Gizmos.color = Color.yellow;
                    // Draw lines
                    Gizmos.DrawLine(ShiftedPosition + points[i], ShiftedPosition + points[GetNextIndex(i)]);
                    if (directionArrows) {
                        Vector3 arrowPosition = (2 * ShiftedPosition + points[i] + points[GetNextIndex(i)]) / 2f;
                        Vector3 direction = (points[GetNextIndex(i)] - points[i]).normalized;
                        DrawArrows(arrowPosition, direction);
                    }
                }
            }
        }

        protected int GetNextIndex() {
            return Index == points.Count - 1 ? 0 : Index + 1;
        }

        protected int GetNextIndex(int i) {
            return i == points.Count - 1 ? 0 : i + 1;
        }
    }
}