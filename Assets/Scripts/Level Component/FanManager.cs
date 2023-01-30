using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Level_Component {
    public class FanManager : MonoBehaviour {
        public static Action<Fan, bool> OnFanExposure;
        private PlayerController _controller;
        private HashSet<Fan> _set;
        private Fan _activeFan = null;

        private void Start() {
            _controller = LevelManager.Instance.Controller;
            if (_controller == null)
                throw new Exception("no player controller");
            _set = new HashSet<Fan>();
        }

        private void ProcessFanExposure(Fan fan, bool isIn) {
            if (isIn) {
                _set.Add(fan);
                _controller.Conditions.IsExposedToFan = true;
                if (_activeFan == null) {
                    _activeFan = fan;
                }
                else {
                    _activeFan = fan.MinBounds > _activeFan.MinBounds ? fan : _activeFan;
                }
            }
            else if (_set.Remove(fan)) {
                _controller.Conditions.IsExposedToFan = _set.Count > 0;
                if (_set.Count == 0)
                    _activeFan = null;
                else {
                    float max = Mathf.NegativeInfinity;
                    foreach (Fan setFan in _set) {
                        max = setFan.MinBounds > max ? setFan.MinBounds : max;
                        _activeFan = setFan;
                    }
                }
            }
        }

        private void FixedUpdate() {
            if (_controller.Conditions.IsExposedToFan) {
                _activeFan.ApplyForce(_controller.transform.position);
            }
        }

        private void OnEnable() {
            OnFanExposure += ProcessFanExposure;
        }

        private void OnDisable() {
            OnFanExposure -= ProcessFanExposure;
        }
    }
}