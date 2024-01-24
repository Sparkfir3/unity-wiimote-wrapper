using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WiimoteApi;

namespace Sparkfire.WiimoteWrapper {

    [DefaultExecutionOrder(-11000), RequireComponent(typeof(WiimoteInputManager))]
    public class WiimoteConnectionManager : MonoBehaviour {

        public static WiimoteConnectionManager Instance;

        #region Internal Data Types

        [System.Serializable]
        private struct WiimoteLightSettings {
            public int playerID;
            public bool led1;
            public bool led2;
            public bool led3;
            public bool led4;

            public WiimoteLightSettings(int playerID, WiimoteLightSettings otherLights) {
                this.playerID = playerID;
                led1 = otherLights.led1;
                led2 = otherLights.led2;
                led3 = otherLights.led3;
                led4 = otherLights.led4;
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------

        // --- Settings ---
        [field: Header("Settings"), SerializeField]
        public int MaxPlayerCount { get; protected set; } = 4;
        [SerializeField]
        [Tooltip("The default light settings for a connected wiimote without an assigned player")]
        private WiimoteLightSettings defaultLightSettings = new() { playerID = -1, led1 = true, led2 = true, led3 = true, led4 = true };
        [SerializeField]
        [Tooltip("Controls which lights are enabled for each player number")]
        private List<WiimoteLightSettings> wiimoteLights;
        
        // --- Runtime Data ---
        public Dictionary<int, Wiimote> PlayerWiimotes { get; protected set; }

        // ------------------------------------------------------------------------------------

        #region Unity Functions

        private void Awake() {
            #region Singleton
            if(Instance == null) {
                Instance = this;
                WiimoteInputManager.Instance = GetComponent<WiimoteInputManager>();
                DontDestroyOnLoad(transform.root.gameObject);
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
            for(int i = WiimoteManager.Wiimotes.Count - 1; i >= 0; i--) {
                WiimoteManager.Cleanup(WiimoteManager.Wiimotes[i]);
            }
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if(MaxPlayerCount < 0) {
                MaxPlayerCount = 0;
            }
            while(wiimoteLights.Count < MaxPlayerCount) {
                wiimoteLights.Add(new WiimoteLightSettings(wiimoteLights.Count, defaultLightSettings));
            }
            while(wiimoteLights.Count > MaxPlayerCount) {
                wiimoteLights.RemoveAt(wiimoteLights.Count - 1);
            }
        }
#endif

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Setup and Assignment

        public void FindAndSetupWiimotes(in bool assignDefaultLights = true) {
            FindAndSetupWiimotes(assignDefaultLights, InputDataType.REPORT_BUTTONS_ACCEL_EXT16, IRDataType.BASIC);
        }

        public void FindAndSetupWiimotes(in bool assignDefaultLights, in InputDataType inputDataType, in IRDataType irDataType) {
            WiimoteManager.FindWiimotes();
            if(!WiimoteManager.HasWiimote()) {
                //Debug.Log("No wiimotes found, nothing to setup!");
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

        public bool AssignWiimoteToPlayer(in int playerID, Wiimote wiimote) {
            if(playerID >= MaxPlayerCount) {
                Debug.LogError($"Failed to assign wiimote to player number {playerID}, as it is over the set player limit of {MaxPlayerCount}!");
                return false;
            }
            if(wiimote == null) {
                Debug.LogError($"Assigning a wiimote to player {playerID}, but the provided wiimote is null! Please call RemoveWiimoteFromPlayer() directly instead.");
                RemoveWiimoteFromPlayer(playerID);
                return false;
            }

            PlayerWiimotes[playerID] = wiimote;
            // Fetch light settings
            bool assignedLights = false;
            foreach(WiimoteLightSettings lightSettings in wiimoteLights) {
                if(lightSettings.playerID == playerID) {
                    assignedLights = true;
                    wiimote.SendPlayerLED(lightSettings.led1, lightSettings.led2, lightSettings.led3, lightSettings.led4);
                    break;
                }
            }
            if(!assignedLights) {
                Debug.LogWarning($"WiimoteConnectionManager could not find a corresponding light settings for player number {playerID}");
            }
            return true;
        }

        public bool RemoveWiimoteFromPlayer(in int playerID) {
            if(playerID >= MaxPlayerCount) {
                Debug.LogError($"Failed to remove wiimote from player number {playerID}, as it is over the set player limit of {MaxPlayerCount}!");
                return false;
            }

            if(PlayerWiimotes[playerID] != null) {
                PlayerWiimotes[playerID] = null;
                return true;
            } else {
                return false;
            }
        }

        public void AutoAssignWiimotesToPlayers() {
            List<Wiimote> wiimotes = AllWiimotesWithoutAPlayer;
            int playerID = 0;
            for(int i = 0; i < wiimotes.Count; i++) {
                // SKip players that already have wiimotes
                while(playerID < MaxPlayerCount && PlayerWiimotes[playerID] != null) {
                    playerID++;
                }
                // If all players are assigned, this is an extra wiimote -> set wiimote to default lights
                if(playerID >= MaxPlayerCount) {
                    wiimotes[i].SendPlayerLED(defaultLightSettings.led1, defaultLightSettings.led2, defaultLightSettings.led3, defaultLightSettings.led4);
                    continue;
                }
                // Assign
                AssignWiimoteToPlayer(playerID, wiimotes[i]);
            }
        }

        #endregion

        // ------------------------------------------------------------------------------------

        #region [Public] Get/Read Data
        
        public static bool PlayerHasWiimote(int playerID) => playerID < Instance.MaxPlayerCount && Instance.PlayerWiimotes[playerID] != null;

        public List<int> AllPlayersWithoutAWiimote => PlayerWiimotes.Where(x => x.Value == null).Select(x => x.Key).ToList();

        public List<Wiimote> AllWiimotesWithoutAPlayer => WiimoteManager.Wiimotes.Where(x => x != null && !PlayerWiimotes.ContainsValue(x)).ToList();

        #endregion

    }
}