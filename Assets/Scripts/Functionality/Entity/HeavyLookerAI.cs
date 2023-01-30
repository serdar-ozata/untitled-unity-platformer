using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Level_Component.Hittable;
using UnityEngine;

namespace Functionality.Entity {
    public class HeavyLookerAI : MonoBehaviour, IHittable {
        public static readonly SortedDictionary<int, HashSet<Transform>> CordMap = new();

        private enum State {
            IDLE,
            LOST,
            ALERT
        }

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        [SerializeField] private float checkDistance = 20f;
        [SerializeField] private float raycastDeltaY = 0.8f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Transform sideCheckPoint;
        private bool _facingRight;
        private State _state;
        private Transform _target;
        private Rigidbody2D _rb;
        private LayerMask _layersToCheck;

        private void Start() {
            _state = State.IDLE;
            _facingRight = transform.localScale.z > Mathf.Epsilon;
            _rb = GetComponent<Rigidbody2D>();
            _layersToCheck = LayerMask.GetMask("Ground", "LevelComponent");
        }

        private void FixedUpdate() {
            Search();
            switch (_state) {
                case State.IDLE:
                    break;
                case State.LOST:
                    MoveLost();
                    break;
                case State.ALERT:
                    MoveAlert();
                    break;
            }
        }

        private void MoveLost() {
            if (NextToCorner() || Mathf.Abs(transform.position.x - _target.position.x) < 0.01f) {
                _state = State.IDLE;
            }
            else {
                Vector2 velocity = _facingRight ? new Vector2(1f, 0f) : new Vector2(-1f, 0f);
                Move(velocity);
            }
        }

        private void OnCollisionEnter2D(Collision2D col) {
            Transform otf = col.gameObject.transform;
            if (_state != State.ALERT || !ReferenceEquals(_target, otf)) return;
            IHittable hittable = otf.GetComponent<IHittable>();
            hittable?.Hit(col.rigidbody.ClosestPoint(transform.position));
        }

        private void MoveAlert() {
            Vector3 pos = transform.position;
            Vector3 direction = _target.position - pos;
            bool right = direction.x > Mathf.Epsilon;
            if (_facingRight != right) {
                _facingRight = right;
                transform.localScale = _facingRight ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
            }

            if (NextToCorner()) return;
            if (direction.x > 0.2f) {
                Move(new Vector2(1f, 0f));
            }
            else if (direction.x < -0.2f) {
                Move(new Vector2(-1f, 0f));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Move(Vector2 vel) {
            _rb.transform.Translate(vel * (speed * Time.deltaTime), Space.Self);
        }

        private bool NextToCorner() {
            bool groundCheck = Physics2D.OverlapCircle(groundCheckPoint.position, 0.2f, _layersToCheck);
            bool sideCheck = Physics2D.OverlapCircle(sideCheckPoint.position, 0.2f, _layersToCheck);
            return !groundCheck || sideCheck;
        }

        private void Search() {
            Vector3 rawPos = transform.position;
            Vector3 pos = new(rawPos.x, rawPos.y + raycastDeltaY, rawPos.z);
            foreach ((int priority, var list) in CordMap) {
                foreach (Transform otf in list) {
                    Vector3 direction = otf.position - pos;

                    // check if it's close
                    if (direction.magnitude > checkDistance) continue;
                    // check if it's visible
                    bool searchingTheSameEnemy = State.ALERT == _state && ReferenceEquals(otf, _target);
                    bool faceMismatch = !(searchingTheSameEnemy || (direction.x > Mathf.Epsilon) == _facingRight);
                    // means entity doesn't need to look at the light to notice the light
                    // 12 light level
                    bool isMismatchImportant = priority != 12;
                    if (isMismatchImportant && faceMismatch) continue;
                    if (Physics2D.Raycast(pos, direction.normalized, direction.magnitude,
                            _layersToCheck)) continue;

                    _state = State.ALERT;
                    _target = otf;
                    return;
                }
            }

            if (_state != State.IDLE) {
                _state = State.LOST;
            }
        }

        public int Hit(Vector2 hitFrom) {
            Vector2 relativeToCenterVec = hitFrom - (Vector2)transform.position;

            if (_facingRight) relativeToCenterVec *= -1f;
            if (relativeToCenterVec.x < Mathf.Epsilon) return 1;
            Destroy(gameObject);
            return 0;
        }

        private void OnEnable() {
            Transform tf = transform;
            _initialPosition = tf.position;
            _initialRotation = tf.rotation;
        }

        private void OnDisable() {
            Transform tf = transform;
             tf.position = _initialPosition;
             tf.rotation = tf.rotation;
        }
    }
}