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

        [SerializeField]
        private bool debugMode;

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
                Wiimote wiimote = WiimoteConnectionManager.Instance.PlayerWiimotes[i];
                if(wiimote == null)
                    continue;

                // For some reason this code chunk is required to properly read data from the wiimote
                // Don't ask me why, idk either I'm just writing the wrapper
                int ret;
                do {
                    ret = wiimote.ReadWiimoteData();
                } while(ret > 0);

                // Read for button presses
                // We must track this constantly so that we can know when buttons have just been pressed & released
                foreach(WiimoteButton button in buttonList) {
                    if(GetCorrespondingWiimoteButtonPressed(wiimote, button)) { // Button pressed
                        playerWiimoteButtonEvents[i].buttonDown[button] = !playerWiimoteButtonEvents[i].buttonHeld[button];
                        playerWiimoteButtonEvents[i].buttonReleased[button] = false;
                        playerWiimoteButtonEvents[i].buttonHeld[button] = true;

                    } else { // Button not pressed
                        playerWiimoteButtonEvents[i].buttonDown[button] = false;
                        playerWiimoteButtonEvents[i].buttonReleased[button] = playerWiimoteButtonEvents[i].buttonHeld[button];
                        playerWiimoteButtonEvents[i].buttonHeld[button] = false;
                    }
                }
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get Button

        public bool GetButton(int playerNumber, WiimoteButton button) {
            if(!ValidPlayereNumber(playerNumber))
                return false;
            return playerWiimoteButtonEvents[playerNumber].buttonHeld[button];
        }

        public bool GetButtonDown(int playerNumber, WiimoteButton button) {
            if(!ValidPlayereNumber(playerNumber))
                return false;
            return playerWiimoteButtonEvents[playerNumber].buttonDown[button];
        }

        public bool GetButtonUp(int playerNumber, WiimoteButton button) {
            if(!ValidPlayereNumber(playerNumber))
                return false;
            return playerWiimoteButtonEvents[playerNumber].buttonReleased[button];
        }

        #endregion

        #region [Private] Get Button Logic

        private bool GetCorrespondingWiimoteButtonPressed(Wiimote wiimote, in WiimoteButton button) {
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

        public float GetNunchuckJoystickHorizontal(int playerNumber) {
            if(!ValidPlayereNumber(playerNumber))
                return 0f;
            return GetNunchuckAxis(WiimoteConnectionManager.Instance.PlayerWiimotes[playerNumber], horizontal: true);
        }

        public float GetNunchuckJoystickVertical(int playerNumber) {
            if(!ValidPlayereNumber(playerNumber))
                return 0f;
            return GetNunchuckAxis(WiimoteConnectionManager.Instance.PlayerWiimotes[playerNumber], horizontal: false);
        }

        public Vector2 GetNunchuckJoystick2D(int playerNumber) {
            if(!ValidPlayereNumber(playerNumber))
                return default;
            return new Vector2(GetNunchuckAxis(WiimoteConnectionManager.Instance.PlayerWiimotes[playerNumber], horizontal: true),
                GetNunchuckAxis(WiimoteConnectionManager.Instance.PlayerWiimotes[playerNumber], horizontal: false));
        }

        #endregion

        #region [Private] Get Axis Logic

        private float GetNunchuckAxis(Wiimote wiimote, bool horizontal = true) {
            if(wiimote.current_ext != ExtensionController.NUNCHUCK)
                return 0f;

            NunchuckData data = wiimote.Nunchuck;
            int value;
            if(horizontal) {
                value = data.stick[0]; // Horizontal - General range is 35-228
            } else {
                value = data.stick[1]; // Vertical - General range is 27-220
            }

            // Check if input mode was not setup - if not, setup and return 0 (need to try again)
            if(value == 0) {
                wiimote.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
                return 0f;
            }

            // Center is around 128
            if(value > 112 && value < 144)
                return 0f;

            // Adjust horizontal to be in similar range as vertical
            if(horizontal)
                value -= 8;

            // Check for upper/lower bounds
            if(value > 200)
                return 1f;
            else if(value < 47)
                return -1f;

            // Return normalized value
            float normalizedValue = (value - 128f) / 128f;
            return Mathf.Clamp(normalizedValue, -1f, 1f);
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Pointer

        public Vector2 GetPointerScreenPosition(int playerNumber) {
            if(!ValidPlayereNumber(playerNumber))
                return default;
            if(!WiimoteConnectionManager.Instance.PlayerWiimotes.TryGetValue(playerNumber, out Wiimote wiimote) || wiimote == null) {
                if(debugMode)
                    Debug.LogWarning($"Attempting to read pointer position for player {playerNumber}, but that player does not have a wiimote assigned!");
                return Vector2.zero;
            }

            float[] points = wiimote.Ir.GetPointingPosition();
            Debug.Log(points.Length + ": " + points[0] + " / " + points[1]);
            return new Vector2(points[0], points[1]);
        }

        public void SetRectTransformToPointerScreenPosition(int playerNumber, RectTransform rectTransform, bool smoothed = true) {
            Vector2 position = GetPointerScreenPosition(playerNumber);
            if(smoothed)
                position = SmoothPointerPos(rectTransform.anchorMin, position);
            rectTransform.anchorMin = position;
            rectTransform.anchorMax = position;
        }

        #endregion

        #region [Private] Pointer Logic

        /// <summary>
        /// Returns a lerped/smoothed position for the pointer, to smooth out jittery movement
        /// </summary>
        /// <param name="basePos">The pointer's current position</param>
        /// <param name="newPos">The position the pointer is attempting to go towards</param>
        private Vector3 SmoothPointerPos(Vector3 basePos, Vector3 newPos) {
            float distance = (newPos - basePos).magnitude;

            // TODO - make these adjustable settings
            if(distance < 0.03f)
                return Vector2.Lerp(basePos, newPos, 0.3f);
            else if(distance < 0.05f)
                return Vector2.Lerp(basePos, newPos, 0.7f);
            return newPos;
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Accelerometer/Motion Controls

        /// <summary>
        /// Returns the wiimote's acceleration data as a vector, normalized.
        /// </summary>
        public Vector3 GetAccelVector(int playerNumber) {
            return GetAccelVectorRaw(playerNumber).normalized;
        }

        /// <summary>
        /// Returns the wiimote's acceleration data as a vector.
        /// </summary>
        public Vector3 GetAccelVectorRaw(int playerNumber) {
            if(!ValidPlayereNumber(playerNumber))
                return default;
            if(!WiimoteConnectionManager.Instance.PlayerWiimotes.TryGetValue(playerNumber, out Wiimote wiimote) || wiimote == null) {
                Debug.LogError($"Attempted to get accelerometer vector for player {playerNumber}, but that player does not have a wiimote assigned!");
                return default;
            }

            float accel_x;
            float accel_y;
            float accel_z;

            float[] accel = wiimote.Accel.GetCalibratedAccelData();
            accel_x = accel[0];
            accel_y = accel[2];
            accel_z = accel[1];

            return new Vector3(accel_x, accel_y, accel_z);
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region Other

        private bool ValidPlayereNumber(int playerNumber) {
            if(playerNumber >= WiimoteConnectionManager.Instance.MaxPlayerCount) {
                Debug.LogError($"Attempted to read an input for player {playerNumber}, but that player number is out of range and invalid!\n" +
                    $"The maximum player count is {WiimoteConnectionManager.Instance.MaxPlayerCount} (set in WiimoteConnectionManager), " +
                        $"meaning the highest valid number is {WiimoteConnectionManager.Instance.MaxPlayerCount - 1}");
                return false;
            }
            return true;
        }

        #endregion

    }
}