using UnityEngine;

namespace Level_Component.Electricity {
    public class MovableWire : Wire {
        public override Vector2 GetPosition() {
            return GetComponentInParent<Transform>().position;
        }

        protected override void Start() {
            base.Start();
        }

        private void OnTriggerEnter2D(Collider2D col) {
            CheckCollision(col, true);
        }

        private void OnTriggerExit2D(Collider2D other) {
            CheckCollision(other, false);
        }

        private void CheckCollision(Collider2D col, bool enter) {
            IConductor conductor = col.GetComponentInParent<IConductor>();
            if (null == conductor) return;
            int layer = col.gameObject.layer;
            if (layer == LayerMask.NameToLayer("InnerLevelComponent")) {
                var result = UpdateConnection(enter ? IConductor.WireCheckEnter : IConductor.WireCheckExit, conductor);
                Manager.OnCircuitChange(result);
            }
        }
    }
}