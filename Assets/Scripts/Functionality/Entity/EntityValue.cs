using UnityEngine;

namespace Functionality.Entity {
    public class EntityValue {
        public Vector3 Cord;

        // small values have higher priority
        public int Priority;

        public EntityValue(Vector3 cord, int priority) {
            Cord = cord;
            Priority = priority;
        }
    }
}