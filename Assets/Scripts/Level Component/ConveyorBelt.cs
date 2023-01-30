using System;
using Player_States;
using UnityEngine;

namespace Level_Component {
    public class ConveyorBelt : MonoBehaviour {
        public float beltSpeed;
        private Vector2 _direction;
        private PlayerMovement _playerMovement;

        private void Start() {
            _direction = transform.rotation * Vector3.left * beltSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            // if player can move
            _playerMovement = other.gameObject.GetComponent<PlayerMovement>();
            if (_playerMovement == null) return;
            _playerMovement.AddRelativeSpeed(_direction);
        }


        private void OnTriggerExit2D(Collider2D other) {
            if (_playerMovement != null)
                _playerMovement.AddRelativeSpeed(-_direction);
            _playerMovement = null;
        }
    }
}