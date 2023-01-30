using UnityEngine;

namespace Functionality.Interactable {
    public class AreaInteractable : MonoBehaviour, IInteractable {
        private IInteractable _parent;
        // put this object as a child to the object that you want to be interactable
        // also put a collider which set to LevelComponent Layer
        private int _playerLayer;
        private void Start() {
            // it also finds itself that's why it returns an array
            // what we want to find is the parent which implemented IInteractable interface
            IInteractable[] list = GetComponentsInParent<IInteractable>();
            foreach (IInteractable el in list) {
                if (el == (IInteractable)this) continue;
                _parent = el;
                break;
            }
            _playerLayer = LayerMask.NameToLayer("Player");
        }

        private void OnTriggerEnter2D(Collider2D col) {
            int layer = col.gameObject.layer;
            if (layer == _playerLayer) {
                IInteractable.InteractionPopup.Invoke(this, true);
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            int layer = other.gameObject.layer;
            if (layer == _playerLayer) {
                IInteractable.InteractionPopup.Invoke(this, false);
            }
        }
        
        public void OnInteract() {
            _parent.OnInteract();
        }
    }
}