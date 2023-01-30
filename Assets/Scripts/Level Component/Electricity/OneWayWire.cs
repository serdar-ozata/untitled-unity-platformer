using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Level_Component.Electricity {
    public class OneWayWire : Wire {
        public float maximumAngle = 30f;
        private Vector2 _direction;
        private Vector2 _position;

        protected override void OnEnable() {
            _direction = Rotate(Vector2.up, transform.rotation.eulerAngles.z);
            _position = transform.position;
            base.OnEnable();
        }

        public bool IsConnectable(Vector2 pos) {
            Vector2 otherDirection = _position - pos;
            return Vector2.Angle(otherDirection, _direction) < maximumAngle;
        }

        private bool IsConnectableSelf(Vector2 pos) {
            Vector2 otherDirection = pos - _position;
            return Vector2.Angle(otherDirection, _direction) < maximumAngle;
        }

        protected override Collider2D[] GetColliders(float wireCheckArea) {
            Vector3 transformPos = transform.position;
            var cols = Physics2D.OverlapAreaAll(
                new Vector2(transformPos.x - wireCheckArea, transformPos.y - wireCheckArea),
                new Vector2(transformPos.x + wireCheckArea, transformPos.y + wireCheckArea));
            return cols.Where(col => IsConnectableSelf(col.transform.position)).ToArray();
        }

        private static Vector2 Rotate(Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        protected override List<IConductor> FilterConductors(Collider2D[] cols) {
            List<IConductor> conductors = new List<IConductor>(cols.Length);
            foreach (Collider2D col in cols) {
                IConductor conductor = col.GetComponent<IConductor>();
                if (conductor == null || conductor == (IConductor)this) continue;
                if (conductor is OneWayWire oneWayWire) {
                    if (oneWayWire.IsConnectable(transform.position)) {
                        conductors.Add(conductor);
                    }
                }
                else {
                    conductors.Add(conductor);
                }
            }

            return conductors;
        }
    }
}