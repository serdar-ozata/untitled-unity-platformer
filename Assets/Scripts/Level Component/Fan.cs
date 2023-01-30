using System;
using Player_States;
using UnityEngine;

namespace Level_Component {
    public class Fan : MonoBehaviour {
        public static Action<Vector2> OnFanExposure;
        public float force = 10f;
        private float _effectDistance;
        [NonSerialized] public float MinBounds;
        private Collider2D _collider;

        private void Start() {
            _collider = GetComponent<Collider2D>();
            var bounds = _collider.bounds;
            _effectDistance = bounds.max.y - bounds.min.y;
            MinBounds = bounds.min.y;
        }

        private void OnTriggerEnter2D(Collider2D col) {
            if (col.gameObject.GetComponent<PlayerJump>() != null)
                FanManager.OnFanExposure?.Invoke(this, true);
        }

        private void OnTriggerExit2D(Collider2D col) {
            if (col.gameObject.GetComponent<PlayerJump>() != null)
                FanManager.OnFanExposure?.Invoke(this, false);
        }


        public void ApplyForce(Vector3 other) {
            float yDist = other.y - MinBounds;
            if (yDist < 0.01f || _effectDistance - yDist < 0f) return;
            float forceEquation = force * Mathf.Pow(_effectDistance - yDist, 0.4f);
            OnFanExposure.Invoke(new Vector2(0f, forceEquation));
        }

        public override int GetHashCode() {
            Vector3 pos = transform.position;
            return (int)(pos.x + 10000 * pos.y);
        }
    }
}