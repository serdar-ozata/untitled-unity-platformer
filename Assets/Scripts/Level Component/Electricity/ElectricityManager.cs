using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Level_Component.Electricity {
    public class ElectricityManager : MonoBehaviour {
        [Header("Debug")] public bool showInfo = true;

        /**
         * DISABLE: disables every object it traverses until finds a battery
         * ENABLE: enables every object it traverses
         * SEARCH_ONLY: the traversed objects' state doesn't change
         * FORCE_DISABLE: Disables the circuit no matter what
         */
        private enum DfsType {
            DISABLE,
            ENABLE,
            SEARCH_ONLY,
            FORCE_DISABLE,
            FORCE_ENABLE,
            LEAKED_ENABLE,
        }
        
        private void OnEnable() {
            HashSet<IConductor> conductors = new HashSet<IConductor>(GetComponentsInChildren<IConductor>());
            if (showInfo)
                Debug.Log("Conductor count: " + conductors.Count);
            InitializeCircuits(conductors);
        }

        public void OnCircuitCharge(IConductor battery) {
            CircuitCheckDfs(battery, battery.On() ? DfsType.ENABLE : DfsType.DISABLE);
        }


        private bool IsNotLeakingBattery(IConductor conductor) {
            if (conductor.On() && conductor is IBattery and not LeakingBattery) {
                CircuitCheckDfs(conductor, DfsType.ENABLE);
                return true;
            }
            else return false;
        }

        private bool IsBattery(IConductor conductor) {
            if (conductor.On() && conductor is IBattery) {
                DfsType type = DfsType.ENABLE;
                if (conductor is LeakingBattery battery) {
                    battery.StartCountDown();
                    type = DfsType.LEAKED_ENABLE;
                }

                CircuitCheckDfs(conductor, type);
                return true;
            }
            else return false;
        }

        private bool IsVoid(IConductor conductor) {
            if (conductor is not Void) return false;
            CircuitCheckDfs(conductor, DfsType.FORCE_DISABLE);
            
            return true;
        }

        // similar to CircuitCheckDfs()
        private void InitializeCircuits([NotNull] HashSet<IConductor> set) {
            // store the enabled batteries so that we can run CircuitCheckDfs() after the initialization
            List<IConductor> enabledBatteries = new List<IConductor>();
            int circuitCounter = 0;
            // iterate over all conductors and find the enabled batteries
            while (set.Count > 0) {
                circuitCounter++;
                HashSet<IConductor>.Enumerator numerator = set.GetEnumerator();
                numerator.MoveNext();
                IConductor initialConductor = numerator.Current;
                numerator.Dispose();
                set.Remove(initialConductor);
                // init stack based dfs
                Stack<IConductor> stack = new Stack<IConductor>();
                stack.Push(initialConductor);
                bool foundEnabledBatteryInThisCircuit = false;

                while (stack.Count > 0) {
                    IConductor currentConductor = stack.Pop();
                    // add to list if it's an enabled battery

                    if (!foundEnabledBatteryInThisCircuit && currentConductor.On() &&
                        currentConductor is IBattery) {
                        enabledBatteries.Add(currentConductor);
                        if (showInfo) {
                            Debug.Log("Enabled battery has been found!");
                        }

                        foundEnabledBatteryInThisCircuit = true;
                    }

                    // get conductors nearby 
                    var conductorsConnected = currentConductor.GetConductorsConnected();
                    // filter then add them to stack
                    foreach (IConductor cond in conductorsConnected.Where(set.Contains)) {
                        stack.Push(cond);
                        set.Remove(cond);
                    }
                }
            }
            
            foreach (IConductor battery in enabledBatteries) {
                CircuitCheckDfs(battery, DfsType.ENABLE);
            }

            if (showInfo)
                Debug.Log("Circuits created: " + circuitCounter);
        }

        public void OnCircuitChange(IConductor[] conductorsChanged) {
            
            foreach (IConductor conductor in conductorsChanged) {
                // to prevent shutting down a charged battery
                // note: this may cause bugs in future since it disables DFSing on that circuit
                if (!(conductor.On() && conductor is IBattery))
                    CircuitCheckDfs(conductor);
            }
        }


        private void CircuitCheckDfs(IConductor startPoint, DfsType type = DfsType.DISABLE) {
            Stack<IConductor> stack = new Stack<IConductor>();
            HashSet<IConductor> visited = new HashSet<IConductor>();
            stack.Push(startPoint);

            while (stack.Count > 0) {
                IConductor currentConductor = stack.Pop();

                //TODO oneway bug
                // if (currentConductor is OneWayWire oneWayWire) {
                //     if (!oneWayWire.IsConnectable(transform.position)) {
                //         
                //     }
                // }

                switch (type) {
                    case DfsType.DISABLE:
                        if (startPoint != currentConductor && IsBattery(currentConductor)) return;
                        currentConductor.OnDeactivate();
                        break;
                    case DfsType.SEARCH_ONLY:
                        if (startPoint != currentConductor && IsBattery(currentConductor)) return;
                        break;
                    case DfsType.ENABLE:
                        if (startPoint != currentConductor && IsVoid(currentConductor)) return;
                        currentConductor.OnActivate();
                        break;
                    case DfsType.FORCE_DISABLE:
                        currentConductor.OnDeactivate();
                        break;
                    case DfsType.FORCE_ENABLE:
                        currentConductor.OnActivate();
                        break;
                    case DfsType.LEAKED_ENABLE:
                        if (startPoint != currentConductor) {
                            if (IsVoid(currentConductor)) return;
                            if (IsNotLeakingBattery(currentConductor)) return;
                        }

                        if (currentConductor is LeakingBattery battery) battery.StartCountDown();
                        currentConductor.OnActivate();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, "EM Exception");
                }

                // get conductors nearby 
                var conductorsConnected = currentConductor.GetConductorsConnected();
                // filter then add them to stack
                foreach (IConductor cond in conductorsConnected.Where(cond => !visited.Contains(cond))) {
                    stack.Push(cond);
                    visited.Add(cond);
                }
            }
        }
    }
}