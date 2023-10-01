using Sparkfire.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WiimoteApi;

namespace Sparkfire.WiimoteWrapper {

    public enum WiimoteButton { A, B, Up, Down, Left, Right, Plus, Minus, Home, One, Two, Z, C };

    [DefaultExecutionOrder(-10000)]
    public class WiimoteInputManager : MonoBehaviour {
        
        public static WiimoteInputManager Instance;

        #region Internal Data Types

        [System.Serializable]
        private class WiimoteButtonEvents {
            public SerializedDictionary<WiimoteButton, bool> buttonDown;
            public SerializedDictionary<WiimoteButton, bool> buttonHeld;
            public SerializedDictionary<WiimoteButton, bool> buttonReleased;

            public WiimoteButtonEvents(in WiimoteButton[] buttonList) {
                buttonDown = new SerializedDictionary<WiimoteButton, bool>();
                buttonHeld = new SerializedDictionary<WiimoteButton, bool>();
                buttonReleased = new SerializedDictionary<WiimoteButton, bool>();
                foreach(WiimoteButton button in buttonList) {
                    buttonDown.Add(button, false);
                    buttonHeld.Add(button, false);
                    buttonReleased.Add(button, false);
                }
            }
        }

        #endregion

        // ------------------------------------------

        // --- Internal Data ---
        private SerializedDictionary<int, WiimoteButtonEvents> playerWiimoteButtonEvents;
        private WiimoteButton[] buttonList;

        // ------------------------------------------------------------------------------------

        #region Unity Functions

        private void Awake() {
            #region Singleton
            if(Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else if(Instance != this) {
                Destroy(gameObject);
            }
            #endregion

            buttonList = (WiimoteButton[])System.Enum.GetValues(typeof(WiimoteButton));
            playerWiimoteButtonEvents = new();
            for(int i = 0; i < WiimoteConnectionManager.Instance.MaxPlayerCount; i++) {
                playerWiimoteButtonEvents.Add(i, new WiimoteButtonEvents(buttonList));
            }
        }

        private void Update() {
            for(int i = 0; i < WiimoteConnectionManager.Instance.MaxPlayerCount; i++) {
                // TODO - update playerWiimoteButtonEvents dictionary with input status
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get Button

        public bool GetButton(int playerNumber, WiimoteButton button) {
            if(!playerWiimoteButtonEvents.ContainsKey(playerNumber)) {
                Debug.LogError($"Attempted to get the status of button {button} for player {playerNumber}, but that player number does not exist!");
                return false;
            }
            return playerWiimoteButtonEvents[playerNumber].buttonHeld[button];
        }

        public bool GetButtonDown(int playerNumber, WiimoteButton button) {
            if(!playerWiimoteButtonEvents.ContainsKey(playerNumber)) {
                Debug.LogError($"Attempted to get the status of button {button} for player {playerNumber}, but that player number does not exist!");
                return false;
            }
            return playerWiimoteButtonEvents[playerNumber].buttonDown[button];
        }

        public bool GetButtonUp(int playerNumber, WiimoteButton button) {
            if(!playerWiimoteButtonEvents.ContainsKey(playerNumber)) {
                Debug.LogError($"Attempted to get the status of button {button} for player {playerNumber}, but that player number does not exist!");
                return false;
            }
            return playerWiimoteButtonEvents[playerNumber].buttonReleased[button];
        }

        #endregion

        // ---------------------

        #region [Private] Get Button Calculations

        private bool GetCorrespondingWiimoteButton(Wiimote wiimote, in WiimoteButton button) {
            switch(button) {
                case WiimoteButton.A:
                    return wiimote.Button.a;
                case WiimoteButton.B:
                    return wiimote.Button.b;
                case WiimoteButton.Up:
                    return wiimote.Button.d_up;
                case WiimoteButton.Down:
                    return wiimote.Button.d_down;
                case WiimoteButton.Left:
                    return wiimote.Button.d_left;
                case WiimoteButton.Right:
                    return wiimote.Button.d_right;
                case WiimoteButton.Plus:
                    return wiimote.Button.plus;
                case WiimoteButton.Minus:
                    return wiimote.Button.minus;
                case WiimoteButton.Home:
                    return wiimote.Button.home;
                case WiimoteButton.One:
                    return wiimote.Button.one;
                case WiimoteButton.Two:
                    return wiimote.Button.two;
                case WiimoteButton.Z:
                case WiimoteButton.C:
                    return GetNunchuckButton(wiimote, button);
                default:
                    return false;
            }
        }
        private bool GetNunchuckButton(Wiimote wiimote, in WiimoteButton button) {
            if(wiimote.current_ext != ExtensionController.NUNCHUCK) {
                //Debug.LogError("Nunchuck not detected");
                return false;
            }

            NunchuckData data = wiimote.Nunchuck;
            if(button == WiimoteButton.Z)
                return data.z;
            else if(button == WiimoteButton.C)
                return data.c;
            else
                return false;
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get Axis

        // TODO

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get Pointer

        // TODO

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get Motion Controls

        // TODO

        #endregion

    }
}