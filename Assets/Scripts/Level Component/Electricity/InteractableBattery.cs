using Functionality.Interactable;
using UnityEngine;

namespace Level_Component.Electricity {
    public class InteractableBattery : BaseConductor, IBattery, IInteractable {
        private SpriteRenderer _spriteRenderer;
        private Sprite _unchargedSprite;
        public Sprite chargedSprite;
    
        protected override void Start() {
            base.Start();
            Manager = GetComponentInParent<ElectricityManager>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _unchargedSprite = _spriteRenderer.sprite;
            if (Open)
                _spriteRenderer.sprite = chargedSprite;
        }


        public override void OnActivate() {
            Open = true;
            _spriteRenderer.sprite = chargedSprite;
        }

        public override void OnDeactivate() {
            Open = false;
            _spriteRenderer.sprite = _unchargedSprite;
        }


        public void OnInteract() {
            Open = !Open;
            Manager.OnCircuitCharge(this);
        }
    }
}