using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace Level_Component.Electricity {
    public abstract class BaseConductor : MonoBehaviour, IConductor {
        protected ElectricityManager Manager;
        [NonSerialized] public bool Open = false;
        private List<IConductor> _conductors = new();

        protected virtual void OnEnable() {
            UpdateConnection();
        }

        protected virtual void Start() {
            Manager = GetComponentInParent<ElectricityManager>();
        }

        public virtual bool On() {
            return Open;
        }

        public abstract void OnActivate();

        public abstract void OnDeactivate();

        public virtual Vector2 GetPosition() {
            return transform.position;
        }

        [NotNull]
        public List<IConductor> GetConductorsConnected() {
            return _conductors;
        }


        // first array is for the items that added the second for the discarded

        public IConductor[] UpdateConnection(float wireCheckArea = IConductor.WireCheckEnter,
            IConductor detectedConductor = null) {
            // manual search if null
            // TODO a bug can occur if manual search is used with one ways 
            if (detectedConductor == null) {
                Collider2D[] cols = GetColliders(wireCheckArea);
                List<IConductor> conductors = FilterConductors(cols);
                return ProcessManualConnection(conductors);
            } // use the detected one
            else return ProcessDetected(wireCheckArea, detectedConductor);
        }

        protected virtual List<IConductor> FilterConductors(Collider2D[] cols) {
            List<IConductor> conductors = new List<IConductor>(cols.Length);
            foreach (Collider2D col in cols) {
                IConductor conductor = col.GetComponent<IConductor>();
                if (conductor == null || conductor == (IConductor)this) continue;
                if (conductor is OneWayWire oneWayWire) {
                    if (oneWayWire.IsConnectable(transform.position)) {
                        conductors.Add(conductor);
                    }
                }
                else {
                    conductors.Add(conductor);
                    conductor.Connect(this);
                }
            }

            return conductors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IConductor[] ProcessManualConnection(List<IConductor> conductors) {
            int itemsAdded = 0;
            int itemsRemained = 0;
            foreach (IConductor conductor in conductors) {
                if (!_conductors.Contains(conductor))
                    itemsAdded++;
                else
                    itemsRemained++;
            }

            IConductor[] changedList = new IConductor[_conductors.Count - itemsRemained + 1];
            // add items
            int k = 0;
            foreach (IConductor conductor in _conductors.Where(conductor => !conductors.Contains(conductor))) {
                changedList[k++] = conductor;
            }

            changedList[k] = this;
            for (int index = 0; index < changedList.Length - 1; index++) {
                IConductor conductor = changedList[index];
                conductor.Disconnect(this);
            }

            _conductors = conductors;
            return changedList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual IConductor[] ProcessDetected(float wireCheckArea, IConductor detectedConductor) {
            bool connect = true;
            if (detectedConductor is OneWayWire oneWayWire) {
                if (oneWayWire.IsConnectable(transform.position))
                    connect = false;
                else
                    return new[] { detectedConductor, this };
            }

            switch (wireCheckArea) {
                case IConductor.WireCheckEnter:
                    if (!_conductors.Contains(detectedConductor)) {
                        _conductors.Add(detectedConductor);
                        if (connect) {
                            detectedConductor.Connect(this);
                            return new IConductor[] { this };
                        }
                        else return new[] { this, detectedConductor };
                    }

                    return Array.Empty<IConductor>();
                case IConductor.WireCheckExit:
                    _conductors.Remove(detectedConductor);
                    detectedConductor.Disconnect(this);
                    return new[] { detectedConductor, this };
                default: // if a special parameter is used
                    return UpdateConnection(wireCheckArea);
            }
        }

        protected virtual Collider2D[] GetColliders(float wireCheckArea) {
            Vector3 transformPos = transform.position;
            return Physics2D.OverlapAreaAll(
                new Vector2(transformPos.x - wireCheckArea, transformPos.y - wireCheckArea),
                new Vector2(transformPos.x + wireCheckArea, transformPos.y + wireCheckArea));
        }

        public void Connect(IConductor other) {
            if (!_conductors.Contains(other))
                _conductors.Add(other);
        }

        public void Disconnect(IConductor other) {
            _conductors.Remove(other);
        }
    }
}