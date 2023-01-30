using UnityEngine;
// Base Class
// virtual doesn't necessarily mean it has to be overriden. 
// Usually just override ExecuteState and getInput (they're abstract so you HAVE TO override them)
namespace Player_States {
    public abstract class PlayerStates : MonoBehaviour {
        protected PlayerController PlayerController;
        protected float HorizontalInput;
        protected float VerticalInput;
    
        protected virtual void InitState() {
            PlayerController = GetComponent<PlayerController>();
        }

        protected virtual void Start() {
            InitState();
        }
        // Use this in general
        public abstract void ExecuteState();
        public virtual void LocalInput() {
            HorizontalInput = Input.GetAxisRaw("Horizontal");
            VerticalInput = Input.GetAxisRaw("Vertical");
            GetInput();
        }
        
        // Use this if you'll get any input
        protected abstract void GetInput();
    }
}
