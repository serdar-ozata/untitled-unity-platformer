using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

/* transition hierarchy:
 * 1 - (exit or enter) + stop
 * 2 - exit or enter
 * 3 - stop 
 * 4 - curved
 * 5 - nothing
 */
namespace Level_Component.PathFollow {
    public class AdvancedPathFollow : PathFollow {
        private enum Transition {
            NONE,
            EASE,
        }

        private enum MoveState {
            ENTER,
            EXIT,
            DEFAULT,
            STOP
        }

        [Serializable]
        private class PointOptions {
            public Transition exitType;
            public float exitDistance;
            public Transition enterType;
            public float enterDistance;
            public float initialSpeed;
            public float speed;

            // curved transition is ignored if stop duration > 0f
            public float pointStopDuration;

            public PointOptions(float speed) {
                exitType = Transition.NONE;
                exitDistance = 0f;
                enterType = Transition.NONE;
                enterDistance = 0f;
                this.speed = speed;
                initialSpeed = 0f;
            }
        }

        [SerializeField, HideInInspector] private List<PointOptions> options;
        [SerializeField, HideInInspector] private bool foldout = false;
        [SerializeField, HideInInspector] private float easePower = 2f;

        // time of the states
        private float _stopTimer;
        private MoveState _moveState;
        private float _distanceToNextPoint = Mathf.Infinity;
        private float _distanceCheck = CheckDistance * 2f;
#if UNITY_EDITOR
        [CustomEditor(typeof(AdvancedPathFollow))]
        public class AdvancedPathFollowEditor : PathFollowEditor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();
                AdvancedPathFollow pathFollow = (AdvancedPathFollow)target;

                pathFollow.options ??= new List<PointOptions>(pathFollow.points.Count);
                if (pathFollow.points.Count < pathFollow.options.Count) {
                    for (int i = pathFollow.options.Count - 1; i >= pathFollow.points.Count; i--) {
                        pathFollow.options.RemoveAt(i);
                    }
                }
                else {
                    for (int i = pathFollow.options.Count; i < pathFollow.points.Count; i++) {
                        pathFollow.options.Add(new PointOptions(pathFollow.moveSpeed));
                    }
                }

                pathFollow.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(pathFollow.foldout, "Options");
                if (pathFollow.foldout) {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < pathFollow.options.Count; i++) {
                        PointOptions option = pathFollow.options[i];
                        EditorGUILayout.LabelField("Option " + i, EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal();
                        option.exitType = (Transition)EditorGUILayout.EnumPopup("Exit", option.exitType);
                        option.exitDistance = EditorGUILayout.FloatField("Distance",
                            option.exitDistance);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        option.enterType = (Transition)EditorGUILayout.EnumPopup("Enter", option.enterType);
                        option.enterDistance =
                            EditorGUILayout.FloatField("Distance", option.enterDistance);
                        EditorGUILayout.EndHorizontal();
                        option.initialSpeed = EditorGUILayout.FloatField(
                            new GUIContent("Initial speed", "Only works if there is an enter or exit transition"),
                            option.initialSpeed);
                        option.speed = EditorGUILayout.FloatField("Speed", option.speed);
                        option.pointStopDuration =
                            EditorGUILayout.FloatField("Stop Duration", option.pointStopDuration);
                    }

                    EditorGUI.indentLevel--;
                    if (pathFollow.options.Any(opt =>
                            opt.enterType == Transition.EASE || opt.exitType == Transition.EASE)) {
                        EditorGUILayout.Space();
                        pathFollow.easePower = EditorGUILayout.FloatField("Ease power :", pathFollow.easePower);
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
#endif

        protected override void Start() {
            base.Start();
            _stopTimer = 0.0f;
            _moveState = MoveState.STOP;
            _distanceCheck = options[0].speed * CheckDistance;
            Index = 0;
        }

        protected override void VisualizeGizmos() {
            if (points != null) {
                for (int i = 0; i < points.Count; i++) {
                    // Draw points
                    Gizmos.color = new Color(10, 6, 214);
                    Vector3 currentPos = ShiftedPosition + points[i];
                    Gizmos.DrawWireSphere(currentPos, 0.4f);
                    //Handles.Label(new Vector3(currentPos.x, currentPos.y + 0.3f), "" + i, EditorStyles.boldLabel);
                    // Draw lines
                    PointOptions nextOption = options[GetNextIndex(i)];
                    Vector3 nextPos = points[GetNextIndex(i)] + ShiftedPosition;
                    Vector3 direction = (nextPos - currentPos).normalized;
                    Vector3 exitPos = currentPos + direction * options[i].exitDistance;
                    Vector3 enterPos = nextPos - direction * (nextOption.enterDistance);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(currentPos, exitPos);
                    Gizmos.color = Color.yellow;
                    if (directionArrows)
                        DrawArrows((enterPos + exitPos) / 2f, direction);

                    Gizmos.DrawLine(exitPos, enterPos);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(enterPos, nextPos);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetSpeed(Transition transition, float initialSpeed, float maxDistance, float endSpeed,
            float currentDistance) {
            return transition switch {
                Transition.NONE => GetNoneSpeed(initialSpeed, maxDistance, endSpeed, currentDistance),
                Transition.EASE => GetEaseSpeed(initialSpeed, maxDistance, endSpeed, currentDistance),
                _ => endSpeed
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetEaseSpeed(float initialSpeed, float maxDistance, float endSpeed, float currentDistance) {
            return initialSpeed + (endSpeed - initialSpeed) * Mathf.Pow(currentDistance / maxDistance, easePower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetNoneSpeed(float initialSpeed, float maxDistance, float endSpeed, float currentDistance) {
            return initialSpeed + (endSpeed - initialSpeed) * currentDistance / maxDistance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetDefaultCheckDistance() {
            return options[Index].speed * CheckDistance / 4f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetCurveCheckDistance() {
            return minDistance;
        }

        private float GetCheckDistance(PointOptions option) {
            if (option.enterDistance > 0.1f) {
                return option.enterDistance;
            }
            else if (curvedTransition && option.pointStopDuration < 0.1f && option.exitDistance < 0.1f) {
                return GetCurveCheckDistance();
            }
            else {
                return GetDefaultCheckDistance();
            }
        }

        private int GetPreviousIndex() {
            return Index > 0 ? Index - 1 : options.Count - 1;
        }


        protected override void FixedUpdate() {
            PointOptions option = options[Index];
            PointOptions previousOption = options[GetPreviousIndex()];
            float distance;
            float speed = option.speed;
            switch (_moveState) {
                case MoveState.STOP:
                    if (_stopTimer < 0.01f) {
                        bool destroyed = IncrementIndex();
                        if (destroyed) return;
                        // no exit transition
                        if (option.exitDistance < 0.1f) {
                            _moveState = MoveState.DEFAULT;
                            // update option variable
                            option = options[Index];
                            // didn't update prevOption because it's not needed
                            speed = option.speed;
                            _distanceCheck = GetCheckDistance(option);
                        } // exit transition
                        else {
                            _moveState = MoveState.EXIT;
                            previousOption = option;
                            option = options[Index];
                            distance = previousOption.exitDistance -
                                       Vector3.Distance(points[GetPreviousIndex()] + ShiftedPosition,
                                           transform.position);
                            speed = GetSpeed(previousOption.exitType, previousOption.initialSpeed,
                                previousOption.exitDistance, option.speed, distance);
                            _distanceCheck = previousOption.exitDistance;
                        }
                    }
                    else {
                        _stopTimer -= Time.deltaTime;
                        return;
                    }

                    break;
                case MoveState.EXIT:
                    distance = Vector3.Distance(points[GetPreviousIndex()] + ShiftedPosition, transform.position);
                    speed = GetSpeed(previousOption.exitType, previousOption.initialSpeed, previousOption.exitDistance,
                        option.speed, distance);
                    if (speed > option.speed) {
                        LeaveExitState(option);
                    }

                    break;
                case MoveState.ENTER:
                    distance = option.enterDistance -
                               Vector3.Distance(points[Index] + ShiftedPosition, transform.position);
                    speed = GetSpeed(option.enterType, option.speed, option.enterDistance,
                        option.initialSpeed, distance);
                    break;
            }

            if (Curving) {
                float distanceToNextPoint = MoveAngular(speed);
                if (distanceToNextPoint < speed * CheckDistance) {
                    Curving = false;
                    _distanceCheck = GetCheckDistance(option);
                }
            }
            else {
                _distanceToNextPoint = MoveLinear(speed);

                if (_distanceToNextPoint < _distanceCheck) {
                    switch (_moveState) {
                        case MoveState.EXIT:
                            // this case is never triggered since there is another "exit end" detection in
                            // _moveState switches
                            // I may remove this in future
                            LeaveExitState(option);
                            return;
                        case MoveState.DEFAULT:
                            if (Mathf.Abs(option.enterDistance - _distanceCheck) < 0.01f &&
                                option.enterDistance > 0.1f) {
                                _moveState = MoveState.ENTER;
                                _distanceCheck = GetDefaultCheckDistance();
                            }
                            else if (option.pointStopDuration > 0.01f) {
                                _stopTimer = option.pointStopDuration;
                                _moveState = MoveState.STOP;
                            }
                            else if (curvedTransition && option.exitDistance < 0.1f) {
                                InitiateCurvedTransition();
                                IncrementIndex();
                            }
                            else {
                                _moveState = MoveState.STOP;
                            }

                            break;
                        case MoveState.ENTER:
                            _stopTimer = option.pointStopDuration;
                            _moveState = MoveState.STOP;
                            break;
                    }
                }
            }
        }

        private void LeaveExitState(PointOptions option) {
            _moveState = MoveState.DEFAULT;
            float nextDist = option.enterDistance;
            _distanceCheck = nextDist > 0.1f ? nextDist : GetDefaultCheckDistance();
        }
    }
}