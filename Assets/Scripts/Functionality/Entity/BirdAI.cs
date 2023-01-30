using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Level_Component.Hittable;
using UnityEngine;
using Pathfinding;
using Unity.Mathematics;
using UnityEditor.Timeline.Actions;

namespace Functionality.Entity {
    public class BirdAI : MonoBehaviour, IHittable {
        public static readonly List<Transform> EntitiesToTrack = new();

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Collider2D _collider;
        
        public static readonly ConcurrentDictionary<Transform, EntityValue> CordMap = new();

        public static readonly ConcurrentBag<BirdAI> Seekers = new();

        private enum State {
            IDLE,
            FOLLOW,
            ATTACK
        }

        // general
        private Rigidbody2D _rb;
        private State _state;

        // path follow
        [NonSerialized] public Seeker Seeker;
        private Path _path;
        private Vector2 idealDirection = Vector2.zero;
        private int _destinationIndex;

        // check thread
        public static bool StopCheckThread;
        public static readonly object MutexStopCheck = new object();
        private Vector3 _position;
        private readonly object _mutexPosition = new object();
        private Dictionary<Vector3, Transform> _pointsToCheck = null;
        private PathFindingState _pathFindingState = PathFindingState.IDLE;

        private readonly object _mutexPathFindingState = new object();

        // these 3 vars don't need mutexes since check Thread waits main thread before reading these
        // This is why I hope at least
        private bool _lastPath = false;
            
        // I'm not sure about _target. This may cause a race condition
        private Transform _target;
        private int _priorityLevel = 9999;
        public static bool IsThereCheckThread = false;
        private enum PathFindingState {
            IDLE,
            RUNNING,
            NOTFOUND,
            FOUND
        }

        // only 

        // inspector
        [SerializeField] private float checkDistance = 10f;
        [SerializeField] private float force = 12f;
        [SerializeField] private float maxSpeed = 7f;
        [SerializeField] private float minDistanceToNextPt = 2f;
        [SerializeField] private float minAcceptedAngle = Mathf.PI / 2f;
        private ContactFilter2D _hitFilter;

        private void Start() {
            Seeker = GetComponent<Seeker>();
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _state = State.IDLE;
            _position = transform.position;
            _hitFilter = new ContactFilter2D {
                layerMask = LayerMask.NameToLayer("Ground")
            };
            Seekers.Add(this);

            InvokeRepeating(nameof(FindPaths), 0f, 0.1f);
        }
        
        private void FindPaths() {
            if (!Seeker.IsDone()) return;
            if (_pointsToCheck is null) return;
            lock (_mutexPathFindingState) {
                _pathFindingState = PathFindingState.RUNNING;
            }

            Seeker.StartMultiTargetPath(_position, _pointsToCheck.Keys.ToArray(), false
                , UpdatePathCallback);
        }

        private void SetNotFound() {
            lock (_mutexPathFindingState) {
                _pathFindingState = PathFindingState.NOTFOUND;
                _pointsToCheck = null;
                if (_lastPath) {
                    _path = null;
                    _state = State.IDLE;
                    _target = null;
                }
            }
        }

        private void UpdatePathCallback(Path p) {
            if (p.error) {
                Debug.Log( "error");
                SetNotFound();
            }
            else {
                lock (_mutexPathFindingState) {
                    _pathFindingState = PathFindingState.FOUND;
                    Vector3 lastPt = p.vectorPath[^1];
                    float minD = Mathf.Infinity;
                    _target = null;
                    foreach ((Vector3 k, Transform tf) in _pointsToCheck) {
                        float distance = Vector3.Distance(k, lastPt);
                        if (distance < minD) {
                            minD = distance;
                            _target = tf;
                        }
                    }

                    _pointsToCheck = null;
                }

                _path = p;
                _destinationIndex = 1;
                _state = State.FOLLOW;
            }
        }

        public static void CheckTarget() {
            while (true) {
                try {
                    bool processedAnything = false;
                    foreach (BirdAI seeker in Seekers) {
                        bool processedInternal = false;
                        SortedList<int, Dictionary<Vector3, Transform>> priorityMap = new();
                        foreach ((Transform otf, EntityValue val) in CordMap) {
                            if (ReferenceEquals(otf, seeker._target)) {
                                if (priorityMap.ContainsKey(val.Priority - 1)) {
                                    priorityMap[val.Priority - 1].Add(val.Cord, otf);
                                }
                                else {
                                    priorityMap.Add(val.Priority - 1, new Dictionary<Vector3, Transform>(1));
                                    priorityMap[val.Priority - 1].Add(val.Cord, otf);
                                }
                            }
                            else {
                                float distance;
                                lock (seeker._mutexPosition) {
                                    distance = Vector3.Distance(val.Cord, seeker._position);
                                }

                                if (distance > seeker.checkDistance) continue;
                                if (priorityMap.ContainsKey(val.Priority)) {
                                    priorityMap[val.Priority].Add(val.Cord, otf);
                                }
                                else {
                                    priorityMap.Add(val.Priority, new Dictionary<Vector3, Transform>(1));
                                    priorityMap[val.Priority].Add(val.Cord, otf);
                                }
                            }
                        }

                        if (priorityMap.Count == 0) continue;
                        int lastKey = priorityMap.Keys[priorityMap.Keys.Count - 1];
                        foreach ((int key, var list) in priorityMap) {
                            int exit = 0;
                            const int maxExit = 10;
                            bool copied = false;
                            if (processedInternal) {
                                processedAnything = true;
                                break;
                            }

                            bool last = lastKey == key;
                            
                            while (exit < maxExit) {
                                PathFindingState state;
                                lock (seeker._mutexPathFindingState) {
                                    state = seeker._pathFindingState;
                                }

                                switch (state) {
                                    case PathFindingState.IDLE:
                                        if (copied) {
                                            exit++;
                                            Thread.Sleep(TimeSpan.FromSeconds(0.1f));
                                        }
                                        else {
                                            exit = 0;
                                            seeker._pointsToCheck = list;
                                            seeker._lastPath = last;
                                            copied = true;
                                        }

                                        break;
                                    case PathFindingState.RUNNING:
                                        exit++;
                                        Thread.Sleep(TimeSpan.FromSeconds(0.1f));
                                        break;
                                    case PathFindingState.NOTFOUND:
                                        exit = maxExit;
                                        seeker._pathFindingState = PathFindingState.IDLE;
                                        break;
                                    case PathFindingState.FOUND:
                                        exit = maxExit;
                                        seeker._pathFindingState = PathFindingState.IDLE;
                                        processedInternal = true;
                                        seeker._priorityLevel = key;

                                        break;
                                }
                            }
                        }

                        if (StopRequested()) return;
                    }


                    if (!processedAnything) {
                        Thread.Sleep(TimeSpan.FromSeconds(0.2));
                    }
                    if (StopRequested()) return;
                }
                catch (Exception e) {
                    Debug.Log(e);
                    throw;
                }
            }
        }

        private static bool StopRequested() {
            lock (MutexStopCheck) {
                if (!StopCheckThread) return false;
                IsThereCheckThread = false;
                StopCheckThread = false;
                return true;
            }
        }


        private void FixedUpdate() {
            switch (_state) {
                case State.IDLE:
                    break;
                case State.FOLLOW:
                    lock (_mutexPosition) {
                        _position = transform.position;
                    }

                    if (MoveToTarget()) break;
                    
                    Vector3 destination = _path.vectorPath[_destinationIndex];
                    Vector2 direction = (destination - _position);
                    _rb.velocity = direction.normalized * maxSpeed;
                    bool tooClose = Mathf.Abs(direction.magnitude) < 0.3f;
                    if (tooClose) {
                        _destinationIndex++;
                        idealDirection = Vector2.zero;
                        if (_destinationIndex == _path.vectorPath.Count) {
                            _state = State.IDLE;
                        }
                    }
                    
                    // if (idealDirection.Equals(Vector2.zero)) {
                    //     idealDirection = direction;
                    // }
                    //
                    // _rb.AddForce(direction.normalized * (force * Time.deltaTime));
                    //
                    //
                    // float magnitude = direction.magnitude;
                    // bool tooClose = magnitude < 0.1f;
                    // if (tooClose || (magnitude < minDistanceToNextPt &&
                    //                  Vector2.Angle(idealDirection, direction) < minAcceptedAngle)) {
                    //     _destinationIndex++;
                    //     idealDirection = Vector2.zero;
                    //     if (_destinationIndex == _path.vectorPath.Count) {
                    //         _state = State.IDLE;
                    //     }
                    // }

                    break;
            }
        }

        private bool MoveToTarget() {
            Vector2 targetPoint = _target.position;
            Bounds bounds = _collider.bounds;
            Vector2 maxStartPoint = bounds.min;
            Vector2 minStartPoint = bounds.max;
            Vector2 minDirection = targetPoint - minStartPoint;
            float minDirLength = minDirection.magnitude;
            Vector2 maxDirection = targetPoint - maxStartPoint;
            float maxDirLength = maxDirection.magnitude;
            int groundMask = LayerMask.GetMask("Ground");
            RaycastHit2D hitMin = Physics2D.Raycast(minStartPoint, minDirection, minDirLength,groundMask);
            if (hitMin) return false;
            
            RaycastHit2D hitMax = Physics2D.Raycast(maxStartPoint, maxDirection, maxDirLength,groundMask);
            if (hitMax) return false;
            
            _rb.velocity = maxDirection.normalized * maxSpeed;
            return true;
        }


        private void OnCollisionEnter2D(Collision2D col) {
            IHittable hittable = col.gameObject.GetComponent<IHittable>();
            if (hittable is not null) {
                hittable.Hit(col.rigidbody.ClosestPoint(transform.position));
                _state = State.IDLE;
            }
        }

        private void OnDestroy() {
            BirdAI ai = this;
            if (!Seekers.TryTake(out ai)) {
                Debug.Log("error couldn't take out object at" + transform.position);
            }
        }

        private void OnEnable() {
            Transform tf = transform;
            _initialPosition = tf.position;
            _initialRotation = tf.rotation;
        }

        private void OnDisable() {
            Transform tf = transform;
            tf.position = _initialPosition;
            tf.rotation = _initialRotation;
        }

        public int Hit(Vector2 hitFrom) {
            Destroy(this);
            return 0;
        }
    }
}