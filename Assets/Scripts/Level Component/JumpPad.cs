using System;
using Player_States;
using UnityEngine;

namespace Level_Component {
    public class JumpPad : MonoBehaviour {
        public static Action<Vector2> OnJump;
        const float jumpPadDurationLimit = 0.6f;
        const float jumpPadLimitMultiplier = 0.9f;
        [SerializeField] private float jumpForce = 5f;

        private Vector2 _direction;
        private Animator _animator;
        private static readonly int Jump = Animator.StringToHash("Jump");

        private void Start() {
            _animator = GetComponent<Animator>();
            _direction = transform.rotation * Vector3.up * jumpForce;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.gameObject.GetComponent<PlayerJump>() == null) return; // if object can jump
            if (OnJump is not null) {
                OnJump.Invoke(_direction);
                PlayerMovement.OnMovementLimit.Invoke(Mathf.Abs(_direction.normalized.x) * jumpPadLimitMultiplier,
                    jumpPadDurationLimit);
            }

            _animator.SetTrigger(Jump);
        }
    }
}