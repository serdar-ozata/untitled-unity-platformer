using UnityEngine;

namespace Functionality.Controllable {
    public class MoveControllable : MonoBehaviour, IControllable {
        [Tooltip("0f makes the transition instantly")]
        public float speed = 0f;

        [Tooltip("Set a relative coordinate")] public Vector3 otherPoint = Vector3.up;
        private bool _inInitialState;
        protected bool Moving;
        protected Vector3 Destination;

        public virtual void Initialize() {
            _inInitialState = true;
            Moving = false;
            Destination = transform.position;
        }

        //TODO
        public bool IsValid() {
            return true;
        }

        public virtual void Execute() {
            if (_inInitialState) {
                Destination += otherPoint;
            }
            else {
                Destination -= otherPoint;
            }

            _inInitialState = !_inInitialState;
            if (speed > 0.01f) {
                Moving = true;
            }
            else {
                transform.position = Destination;
            }
        }

        protected virtual void FixedUpdate() {
            if (Moving) {
                Vector3 pos = transform.position;
                transform.position = Vector3.MoveTowards(pos, Destination, Time.deltaTime * speed);
                if (Vector3.Distance(pos, Destination) < 0.01f) {
                    transform.position = Destination;
                    Moving = false;
                }
            }
        }

        protected void OnDrawGizmos() {
            // Draw points
            Gizmos.color = Color.red;
            Vector3 pos = transform.position;
            Gizmos.DrawWireSphere(pos, 0.4f);
            Gizmos.DrawWireSphere(pos + otherPoint, 0.4f);

            Gizmos.color = Color.yellow;
            // Draw lines
            Gizmos.DrawLine(pos, pos + otherPoint);
        }
    }
}