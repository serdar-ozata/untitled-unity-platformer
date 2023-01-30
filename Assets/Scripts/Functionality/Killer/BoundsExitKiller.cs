using System;
using Level_Component.Hittable;
using UnityEngine;

namespace Functionality.Killer {
    public class BoundsExitKiller : MonoBehaviour {
        private void OnTriggerExit2D(Collider2D other) {
            other.gameObject.GetComponent<IHittable>()?.Hit(other.gameObject.transform.position);
        }
    }
}