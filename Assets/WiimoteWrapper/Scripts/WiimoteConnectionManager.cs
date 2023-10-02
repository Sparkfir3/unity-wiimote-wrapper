using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WiimoteApi;

namespace Sparkfire.WiimoteWrapper {

    [DefaultExecutionOrder(-11000)]
    public class WiimoteConnectionManager : MonoBehaviour {

        public static WiimoteConnectionManager Instance;

        #region Internal Data Types

        [System.Serializable]
        private struct WiimoteLightSettings {
            public int playerNumber;
            public bool led1;
            public bool led2;
            public bool led3;
            public bool led4;
        }

        #endregion

        // ------------------------------------------------------------------------------------

        // --- Settings ---
        [field: Header("Settings"), SerializeField]
        public int MaxPlayerCount { get; protected set; } = 4;
        [SerializeField]
        [Tooltip("The default light settings for a connected wiimote without an assigned player")]
        private WiimoteLightSettings defaultLightSettings = new() { playerNumber = -1, led1 = true, led2 = true, led3 = true, led4 = true };
        [SerializeField]
        [Tooltip("Controls which lights are enabled for each player number")]
        private List<WiimoteLightSettings> wiimoteLights;

        // --- Runtime Data ---
        public Dictionary<int, Wiimote> PlayerWiimotes { get; private set; }

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

            PlayerWiimotes = new();
            for(int i = 0; i < MaxPlayerCount; i++) {
                PlayerWiimotes.Add(i, null);
            }
        }

        private void OnApplicationQuit() {
            foreach(Wiimote wiimote in WiimoteManager.Wiimotes) {
                WiimoteManager.Cleanup(wiimote);
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Setup and Assignment

        public void FindAndSetupWiimotes(in bool assignDefaultLights = true) {
            FindAndSetupWiimotes(assignDefaultLights, InputDataType.REPORT_BUTTONS_ACCEL_EXT16, IRDataType.BASIC);
        }

        public void FindAndSetupWiimotes(in bool assignDefaultLights, in InputDataType inputDataType, in IRDataType irDataType) {
            WiimoteManager.FindWiimotes();
            if(!WiimoteManager.HasWiimote()) {
                Debug.Log("No wiimotes found, nothing to setup!");
                return;
            }

            foreach(Wiimote wiimote in WiimoteManager.Wiimotes) {
                if(assignDefaultLights)
                    wiimote.SendPlayerLED(defaultLightSettings.led1, defaultLightSettings.led2, defaultLightSettings.led3, defaultLightSettings.led4);
                // Mode = acceleration + extensions
                wiimote.SendDataReportMode(inputDataType);
                // IR
                wiimote.SetupIRCamera(irDataType);
            }
        }

        public void AssignWiimoteToPlayer(int playerNumber, Wiimote wiimote) {
            if(playerNumber >= MaxPlayerCount) {
                Debug.LogError($"Failed to assign wiimote to player number {playerNumber}, as it is over the set player limit of {MaxPlayerCount}!");
                return;
            }
            if(wiimote == null) {
                Debug.LogError($"Assigning a wiimote to player {playerNumber}, but the provided wiimote is null! Please call RemoveWiimoteFromPlayer() directly instead.");
                RemoveWiimoteFromPlayer(playerNumber);
                return;
            }

            PlayerWiimotes[playerNumber] = wiimote;
            // Fetch light settings
            bool assignedLights = false;
            foreach(WiimoteLightSettings lightSettings in wiimoteLights) {
                if(lightSettings.playerNumber == playerNumber) {
                    assignedLights = true;
                    wiimote.SendPlayerLED(lightSettings.led1, lightSettings.led2, lightSettings.led3, lightSettings.led4);
                    break;
                }
            }
            if(!assignedLights) {
                Debug.LogWarning($"WiimoteConnectionManager could not find a corresponding light settings for player number {playerNumber}");
            }
        }

        public void RemoveWiimoteFromPlayer(int playerNumber) {
            if(playerNumber >= MaxPlayerCount) {
                Debug.LogError($"Failed to remove wiimote from player number {playerNumber}, as it is over the set player limit of {MaxPlayerCount}!");
                return;
            }

            PlayerWiimotes[playerNumber] = null;
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get/Read Data
        
        public List<int> AllPlayersWithoutAWiimote => PlayerWiimotes.Where(x => x.Value == null).Select(x => x.Key).ToList();

        public List<Wiimote> AllWiimotesWithoutAPlayer => WiimoteManager.Wiimotes.Where(x => x != null && !PlayerWiimotes.ContainsValue(x)).ToList();

        #endregion

    }
}