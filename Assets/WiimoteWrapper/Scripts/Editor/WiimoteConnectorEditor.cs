using UnityEngine;
using UnityEditor;

namespace Sparkfire.WiimoteWrapper.EditorScripts {

    [CustomEditor(typeof(WiimoteConnectionManager), true)]
    public class WiimoteConnectorEditor : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            WiimoteConnectionManager connectionManager = target as WiimoteConnectionManager;

            if(GUILayout.Button("Setup and Auto Assign Wiimotes")) {
                connectionManager.FindAndSetupWiimotes(false);
                connectionManager.AutoAssignWiimotesToPlayers();
            }
        }
    }
}
