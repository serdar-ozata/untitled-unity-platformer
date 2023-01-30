using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Functionality.Dialogue {
    public class DialogueManager : MonoBehaviour {
        // first string array is for the dialogues, second for the image urls
        public static Action<string[], string[]> TriggerConversation;
        public Dictionary<string, Sprite> Portraits = new();

        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI dialogueText;

        private void OnEnable() {
            TriggerConversation += StartDialogue;
        }

        private void StartDialogue(string[] texts, string[] args) {
        }

        private void OnDisable() {
            TriggerConversation -= StartDialogue;
        }
    }
}