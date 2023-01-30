using System;
using System.Diagnostics.Tracing;
using UnityEngine;

namespace Level_Component {
    public class Platform : MonoBehaviour {
        [NonSerialized] public Rigidbody2D Rb;

        private void Start() {
            Rb = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionStay2D(Collision2D col) {
            LevelManager.OnPlatformTouch.Invoke();
        }
    }
}