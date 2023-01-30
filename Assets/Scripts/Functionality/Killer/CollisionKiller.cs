using Level_Component.Hittable;
using UnityEngine;

namespace Functionality.Killer {
    public class CollisionKiller : MonoBehaviour {
        protected virtual void OnCollisionEnter2D(Collision2D col) {
            IHittable entity = col.gameObject.GetComponent<IHittable>();
            // ?. means call the function if not null
            entity?.Hit(col.rigidbody.ClosestPoint(transform.position));
        }
    }
}