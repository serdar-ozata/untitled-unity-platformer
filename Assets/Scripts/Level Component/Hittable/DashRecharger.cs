using UnityEngine;

namespace Level_Component.Hittable {
    public class DashRecharger : MonoBehaviour, IHittable {
        //public Animator animator;

        // TEMP
        private SpriteRenderer _renderer;

        private float _deltaActivation;

        // END TEMP
        private void Start() {
            _deltaActivation = -1f;
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Hit() {
            // TODO put some animations
            // put a color change to see if it's working
        }

        private void FixedUpdate() {
            // TEMP
            if (_deltaActivation > 0f)
                _deltaActivation -= Time.deltaTime;
            else
                _renderer.color = Color.clear;
        }

        public int Hit(Vector2 hitFrom) {
            _renderer.color = Color.green;
            _deltaActivation = 2f;
            return 0;
        }
    }
}