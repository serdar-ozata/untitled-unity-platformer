using System;

namespace Functionality.Interactable {
    public interface IInteractable {
        public static Action<IInteractable, bool> InteractionPopup;

        public void OnInteract();
    }
}