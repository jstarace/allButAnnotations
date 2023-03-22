using Starace.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MouseManager : NetworkBehaviour
{

    [SerializeField] private Camera mainCamera;
    //[SerializeField] private TextMeshProUGUI xValue;
    //[SerializeField] private TextMeshProUGUI yValue;
    //[SerializeField] private TextMeshProUGUI selectedItem;
    //[SerializeField] private TextMeshProUGUI mouseUpItem;

    private bool initialized = false;
    private Vector3 prevMousePosition = Vector3.zero;
    private Vector3 curMousePosition = Vector3.zero;

    private void Start()
    {
      //  selectedItem.text = string.Empty;
      //  mouseUpItem.text = string.Empty;
    }

    private void Update()
    {
        if(NetworkManager.Singleton.IsConnectedClient && !initialized)
        {
            InvokeRepeating("RefreshStats", 1f, 0.2f);
            prevMousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            prevMousePosition.z = 0f;
            initialized = true;
        }
        if(NetworkManager.Singleton.IsConnectedClient && initialized)
        {
            MouseInput();
        }
    }

    private void RefreshStats()
    {
        //Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //mouseWorldPosition.z = 0f;
        curMousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        curMousePosition.z = 0f;
        
        if((int)curMousePosition.x != (int)prevMousePosition.x && (int)curMousePosition.y != (int)prevMousePosition.y)
        {
            LogMousePositionServerRpc(prevMousePosition, curMousePosition);
        }
        prevMousePosition = curMousePosition;
    }

    private void MouseInput()
    {
        // First we have to make sure that the player is in the document and nowhere else
        if (ChatController.Instance.chatInput.isFocused || DocumentManager.Instance.fileName.isFocused) return;
        if (AnnotationsManager.Instance.annotationPanel.active) return;
        // Left click, just the down part, we'll handle the continuous part later
        if (Input.GetMouseButtonDown(0))
        {
            // We need to know which player we're talking about.  So get the player
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();

            // Cast a ray from the mouse click into the world
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ProcessSingleLeftClick(rayCasPos);
            return;
        }
        else if (Input.GetMouseButton(0))
        {
            // We need to know which player we're talking about.  So get the player
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();

            // Cast a ray from the mouse click into the world
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ProcessLeftClickHold(rayCasPos);
        }  
        else if (Input.GetMouseButtonUp(0))
        {
            // We need to know which player we're talking about.  So get the player
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();

            // Cast a ray from the mouse click into the world
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ProcessLeftClickRelease(rayCasPos);
        }
    }

    [ServerRpc (RequireOwnership = false)]

    private void LogMousePositionServerRpc(Vector3 prev, Vector3 cur, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if(NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID);
            LogEntry mouseEntry = new LogEntry(
                DateTime.UtcNow.ToString("MM-dd-yyyy"),
                DateTime.UtcNow.ToString("HH:mm:ss"),
                clientID,
                player.name,
                "user",
                prev,
                cur,
                "Mouse Movement",
                3,
                "Navigation",
                "",
                ""
                );
            FileManager.Instance.ProcessRequests( mouseEntry );
        }

    }
}
