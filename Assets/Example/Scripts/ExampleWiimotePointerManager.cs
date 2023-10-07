using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WiimoteApi;

namespace Sparkfire.WiimoteWrapper.Example {

    public class ExampleWiimotePointerManager : MonoBehaviour {

        [Header("Runtime Data"), SerializeField]
        private List<RectTransform> pointers;
        [Header("Object References"), SerializeField]
        private RectTransform pointerPrefab;

        private void Start() {
            for(int i = 0; i < WiimoteConnectionManager.Instance.MaxPlayerCount; i++) {
                RectTransform newPointer = Instantiate(pointerPrefab, transform);
                pointers.Add(newPointer);
            }
        }

        private void Update() {
            for(int i = 0; i < WiimoteConnectionManager.Instance.MaxPlayerCount; i++) {
                if(WiimoteConnectionManager.PlayerHasWiimote(i)) {
                    pointers[i].gameObject.SetActive(true);
                    WiimoteInputManager.Instance.SetRectTransformToPointerScreenPosition(i, pointers[i]);
                } else {
                    pointers[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
