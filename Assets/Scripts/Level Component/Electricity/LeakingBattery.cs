using System;
using Level_Component.Hittable;
using UnityEngine;

namespace Level_Component.Electricity {
    public class LeakingBattery : BaseConductor, IBattery, IHittable {
        private SpriteRenderer _spriteRenderer;
        private Sprite _unchargedSprite;
        [SerializeField] private Sprite chargedSprite;
        [SerializeField] private float deactivationTime = 3f;
        private bool _permanentActivation;
        private float _deltaDeactivation;

        private const float NoDeactivation = -1000f;

        protected override void Start() {
            base.Start();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _unchargedSprite = _spriteRenderer.sprite;
            if (Open)
                _spriteRenderer.sprite = chargedSprite;
            _permanentActivation = true;
        }


        public override void OnActivate() {
            Open = true;
            _spriteRenderer.sprite = chargedSprite;
            if (_permanentActivation) {
                _deltaDeactivation = NoDeactivation;
                _permanentActivation = false;
            }
        }

        private void FixedUpdate() {
            if (Open) {
                if (deactivationTime < -100f) return;
                if (_deltaDeactivation > Mathf.Epsilon) {
                    _deltaDeactivation -= Time.deltaTime;
                }
                else {
                    Open = false;
                    _deltaDeactivation = NoDeactivation;
                    Manager.OnCircuitCharge(this);
                }
            }
        }

        public override void OnDeactivate() {
            Open = false;
            _spriteRenderer.sprite = _unchargedSprite;
        }

        public int Hit(Vector2 hitFrom) {
            Open = true;
            _permanentActivation = false;
            Manager.OnCircuitCharge(this);
            _permanentActivation = true;
            return 0;
        }

        public void StartCountDown() {
            _deltaDeactivation = deactivationTime;
        }
    }
}