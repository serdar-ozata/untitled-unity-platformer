using UnityEngine;

namespace Level_Component.Hittable {
    public interface IHittable {
        // we will also use this interface to damage enemies
        // 0 means target is killed, 1 means it is not
        public int Hit(Vector2 hitFrom);
    }
}