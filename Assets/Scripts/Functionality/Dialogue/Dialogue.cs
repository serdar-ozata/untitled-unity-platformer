using Functionality.Interactable;
using UnityEngine;

namespace Functionality.Dialogue {
    public class Dialogue : MonoBehaviour, IInteractable {
        public string[] dialogues = new[] { "hello!" };
        public string[] args = new[] { "default" };

        public void OnInteract() {
            DialogueManager.TriggerConversation.Invoke(dialogues, args);
        }
    }
}