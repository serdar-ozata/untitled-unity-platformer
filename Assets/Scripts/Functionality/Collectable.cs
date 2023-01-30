using System;
using Functionality.Interactable;
using UnityEngine;

namespace Functionality {
    public class Collectable: MonoBehaviour, IInteractable {
        public void OnInteract() {
            LevelManager.Instance.CollectCoin();
            Destroy(this, 0.02f);
        }
    }
}