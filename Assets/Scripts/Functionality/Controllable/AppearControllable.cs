using UnityEngine;

namespace Functionality.Controllable {
    // objects vanishes and appears with key press
    // - We can add an animation support later
    // - Make sure the object initially has the activated sprite
    public class AppearControllable : MonoBehaviour, IControllable {
        public bool activated = true;
        
        public Sprite deactivatedSprite = null;
        private Sprite _activatedSprite;
        private SpriteRenderer _spriteRenderer;
        protected Collider2D Collider;
        
        public void Initialize() {
            Collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _activatedSprite = _spriteRenderer.sprite;
            if (!activated) {
                _spriteRenderer.sprite = deactivatedSprite;
                Collider.enabled = false;
            }
            
        }
        //TODO
        public bool IsValid() {
            return true;
        }

        public virtual void Execute() {
            if (activated) {
                activated = false;
                _spriteRenderer.sprite = deactivatedSprite;
                Collider.enabled = false;
            }
            else {
                Collider.enabled = true;
                activated = true;
                _spriteRenderer.sprite = _activatedSprite;
            }
        }
    }
}