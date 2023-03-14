using Starace.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MouseManager : NetworkBehaviour
{

    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI xValue;
    [SerializeField] private TextMeshProUGUI yValue;
    [SerializeField] private TextMeshProUGUI selectedItem;
    [SerializeField] private TextMeshProUGUI mouseUpItem;

    //private bool server = true;
    private bool initialized = false;

    private Vector3 downClick;
    private Vector3 upClick;

    private void Start()
    {
        selectedItem.text = string.Empty;
        mouseUpItem.text = string.Empty;
    }

    private void Update()
    {
        if(NetworkManager.Singleton.IsConnectedClient && !initialized)
        {
            InvokeRepeating("RefreshStats", 2f, 1f);
            initialized = true;
        }
        if(NetworkManager.Singleton.IsConnectedClient && initialized)
        {
            MouseInput();
        }

    }

    private void RefreshStats()
    {
        //Debug.Log("Looping");
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        xValue.text = mouseWorldPosition.x.ToString();
        yValue.text = mouseWorldPosition.y.ToString();
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
            Debug.Log("Single click");

            // Cast a ray from the mouse click into the world
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ProcessSingleLeftClick(rayCasPos);
            return;
        }

        if (Input.GetMouseButton(0))
        {
            // We need to know which player we're talking about.  So get the player
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();

            // Cast a ray from the mouse click into the world
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ProcessLeftClickHold(rayCasPos);

        }

        /*
        if(Input.GetMouseButtonUp(0))
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<PlayerNetwork>();
            Vector2 rayCasPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(rayCasPos, Vector2.zero);
            //Debug.Log("CLICKED HERE: (" + rayCasPos.x + ", " + rayCasPos.y + ")");
            if (hit.collider != null)
            {
                string theChar = hit.collider.gameObject.GetComponent<TextMesh>().text;
                int x, y;
                Utilities.GetListXY(hit.collider.transform.position, out x, out y);
                mouseUpItem.text = string.Format("{0}, ({1}, {2})", theChar, x, y);
                upClick= hit.collider.transform.position;
                player.ProcessMouseInput(downClick, upClick);
            }
            else
            {
                player.ProcessMouseInput(downClick, rayCasPos);
            }
        }
        */
        

        if(Input.GetMouseButtonDown(1)) 
        {
            Debug.Log("you right clicked");
            Vector3 start = new Vector3();
            Vector3 end = new Vector3();
        }
    }
}
