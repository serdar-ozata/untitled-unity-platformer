using Level_Component.Electricity;
using UnityEditor;
using UnityEngine;

namespace Level_Component.PathFollow {
    public class ConditionalAPF : AdvancedPathFollow {
#if UNITY_EDITOR
        [CustomEditor(typeof(ConditionalAPF))]
        public class ConditionalAdvancedPathFollowEditor : AdvancedPathFollowEditor {
        }
#endif

        [SerializeField] private BaseConductor invoker;

        protected override void Start() {
            base.Start();
            invoker ??= GetComponentInChildren<BaseConductor>();
            if (invoker is null) {
                Debug.LogError("Object at " + transform.position + " doesn't have an invoker");
                Destroy(this);
            }
        }

        protected override void FixedUpdate() {
            if (invoker.On())
                base.FixedUpdate();
        }
    }
}