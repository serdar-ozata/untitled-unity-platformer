using Level_Component.Hittable;
using UnityEngine;
// entity with basic functionality. Cannot move
namespace Functionality.Entity {
    public class BaseEntity : MonoBehaviour, IHittable {
        public int lives = 1;
        protected Animator Animator;
        
        protected virtual void Start() {
            Animator = GetComponent<Animator>();
        }

        public virtual int Hit(Vector2 hitFrom) {
            if (lives > 0) {
                lives--;
                // put some animation
                return 0;
            }
            else {
                Destroy(this);
                return 0;
            }
        }
    }
}