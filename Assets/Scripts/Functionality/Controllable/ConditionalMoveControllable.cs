using Level_Component.Electricity;
using UnityEngine;

namespace Functionality.Controllable {
    public class ConditionalMoveControllable : MoveControllable {
        [SerializeField] private BaseConductor invoker;

        public override void Initialize() {
            base.Initialize();
            invoker ??= GetComponentInChildren<BaseConductor>();
        }

        public override void Execute() {
            if (invoker.On())
                base.Execute();
        }

        protected override void FixedUpdate() {
            if (invoker.On())
                base.FixedUpdate();
        }
    }
}