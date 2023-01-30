using System;
using System.Collections;
using System.Collections.Generic;
using Player_States;
using UnityEngine;

public class PlayerMotor : MonoBehaviour {
    private PlayerStates[] _playerStates;

    private void Awake() {
        // this should be taken out from here later
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
    }

    // Start is called before the first frame update
    private void Start() {
        _playerStates = GetComponents<PlayerStates>();
    }

    // Update is called once per frame
    private void Update() {
        foreach (PlayerStates state in _playerStates) {
            state.LocalInput();
            state.ExecuteState();
        }
    }
}