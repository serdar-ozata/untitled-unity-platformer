using Functionality.Interactable;
using UnityEngine;

namespace Player_States {
    public class PlayerInteraction : PlayerStates {
        // a popup will hover over the player if there is an interactable object
        // we can edit this class e.g. the object might get highlighted instead of a popup
        private IInteractable _interactableObject;
        private bool _open;
        public SpriteRenderer spriteRenderer;

        protected override void InitState() {
            base.InitState();
            _interactableObject = null;
            spriteRenderer.enabled = false;
        }

        public override void ExecuteState() {
        }

        protected override void GetInput() {
            if (Input.GetButtonDown("Interact")) {
                if (PlayerController.Conditions.OnInteraction) {
                    // text system
                }
                else {
                  _interactableObject?.OnInteract();
                }
            }
        }

        private void PopupResponse(IInteractable obj, bool enable) {
            if (enable) {
                _interactableObject = obj;
                PlayerController.Conditions.IsThereInteractable = true;
            }
            else {
                if (_interactableObject == obj) {
                    PlayerController.Conditions.IsThereInteractable = false;
                    _interactableObject = null;
                }
            }

            spriteRenderer.enabled = _interactableObject != null;
        }

        private void OnEnable() {
            IInteractable.InteractionPopup += PopupResponse;
        }

        private void OnDisable() {
            IInteractable.InteractionPopup -= PopupResponse;
        }
    }
}