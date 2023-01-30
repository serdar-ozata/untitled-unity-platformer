using System.Collections.Generic;
using UnityEngine;

namespace Level_Component.Electricity {
    public interface IConductor {
        protected const float WireCheckEnter = 0.5f;
        protected const float WireCheckExit = 0.44f;
        public bool On();
        public void OnActivate();
        public void OnDeactivate();
        public Vector2 GetPosition();
        public List<IConductor> GetConductorsConnected();
        public IConductor[] UpdateConnection(float wireCheckArea = WireCheckEnter, IConductor conductor = null);
        public void Connect(IConductor other);
        public void Disconnect(IConductor other);
    }
}