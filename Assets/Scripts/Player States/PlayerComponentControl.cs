using System.Collections.Generic;
using System.Linq;
using Functionality.Controllable;
using UnityEngine;

namespace Player_States {
    public class PlayerComponentControl : PlayerStates {
        public List<GameObject> controllableGrids;
        private List<IControllable> _controllables;

        protected override void InitState() {
            base.InitState();
            _controllables = new List<IControllable>();
            foreach (var components in controllableGrids.Select(controllableGrid =>
                         controllableGrid.GetComponentsInChildren<IControllable>())) {
                _controllables.AddRange(components);
            }

            foreach (IControllable objs in _controllables) {
                objs.Initialize();
            }
        }

        public override void ExecuteState() {
        }

        // We may use jobs for this function in future
        protected override void GetInput() {
            if (PlayerController.Conditions.OnInteraction) return;
            if (Input.GetButtonDown("Control")) {
                // my code was this:
                // foreach (IControllable objs in _controllables) {
                // if (!objs.IsValid()) return;
                // }
                // rider changed it to code below
                if (_controllables.Any(objs => !objs.IsValid())) return;

                foreach (IControllable objs in _controllables) {
                    objs.Execute();
                }
            }
        }
    }
}