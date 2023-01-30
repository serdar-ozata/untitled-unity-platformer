using UnityEngine;

namespace Level_Component.Electricity {
    // absorbs the charge of the circuit
    public class Void : BaseConductor {
        private SpriteRenderer _spriteRenderer;

        private Sprite _unchargedSprite;

        // we may add an animation that is executed if the charge gets absorbed
        protected override void Start() {
            base.Start();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _unchargedSprite = _spriteRenderer.sprite;
            Open = false;
        }

        public override void OnActivate() {
        }

        public override void OnDeactivate() {
        }

        public override bool On() {
            return false;
        }
    }
}