using System.Collections.Generic;
using Functionality.Entity;
using UnityEngine;

namespace Level_Component.Electricity {
    public class Light : BaseConductor {
        [SerializeField] public bool isBait = false;
        [SerializeField] public int priority = 12;
        private SpriteRenderer _spriteRenderer;
        protected override void Start() {
            base.Start();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void OnActivate() {
            if (Open) return;
            Open = true;
            _spriteRenderer.color = Color.white;
            if (!HeavyLookerAI.CordMap.ContainsKey(priority)) {
                HeavyLookerAI.CordMap.Add(priority, new HashSet<Transform>());
            }
            HeavyLookerAI.CordMap[priority].Add(transform);
        }
        
        
        public override void OnDeactivate() {
            if (!Open) return;
            Open = false;
            _spriteRenderer.color = Color.black;
            if (!HeavyLookerAI.CordMap[priority].Remove(transform)) {
                Debug.LogError("Couldn't remove the light");
            }
        }
    }
}