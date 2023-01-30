using System.Runtime.CompilerServices;
using UnityEngine;

namespace Functionality {
    //TODO doesn't work - will fix it if it's needed
    public class RelativeSticky: Sticky {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 GetVelocity() {
            return (transform.position - PrevLocation) / Time.deltaTime;
        }

        protected override void OnTriggerExit2D(Collider2D other) {
            base.OnTriggerExit2D(other);
            Movement.ApplyVelocity(GetVelocity());
        }
    }
}