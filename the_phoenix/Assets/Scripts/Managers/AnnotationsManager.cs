using Starace.Utils;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class AnnotationsManager : NetworkBehaviour
{
    public static AnnotationsManager Instance { set; get; }
    private Dictionary<ulong, ulong> userSelections;
    private Dictionary<ulong, List<ulong>> testSelections;

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;
        if (enabled)
        {
            if(Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }
        if(!enabled || Instance == null) return;
        userSelections = new Dictionary<ulong, ulong>();
        testSelections = new Dictionary<ulong, List<ulong>>();
    }

    public void ToggleHighlight(ulong netID, ulong clientID)
    {
        Debug.Log(netID);

        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
        var tempChild = tempObject.transform.GetChild(0);
        bool state = tempChild.gameObject.activeSelf;
        tempChild.gameObject.SetActive(!state);
        ToggleHighlightClientRpc(netID, !state);
    }

    [ClientRpc]
    private void ToggleHighlightClientRpc(ulong netID, bool state)
    {
        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
        var tempChild = tempObject.transform.GetChild(0);
        //bool state = tempChild.gameObject.activeSelf;
        tempChild.gameObject.SetActive(state);
    }
}
