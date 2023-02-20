using Starace.Utils;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using Cinemachine;
using System.Collections.Generic;

public class PlayerNetwork: NetworkBehaviour
{
    private NetworkVariable<Color32> tempColor = new NetworkVariable<Color32>(Color.blue,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
 
    private bool isConfigured = false;
    private Vector2 userScale = new Vector2(1f, 3f);
    private bool isMoving;
    private bool welcome = false;

    private int cellSize = 3;
    private DocumentEntry docEntry;

    private bool textSelected;
    private Vector3 mouseClickPosition;

    #region Initialization and update
    private void Awake()
    {
        tempColor.OnValueChanged += (Color32 previousValue, Color32 newValue) =>
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);
                var player = playerObject.GetComponent<SpriteRenderer>();
                player.color = tempColor.Value;
                SetUserColorClientRpc(playerObject, player.color);
            }
        };

        playerName.OnValueChanged += (FixedString32Bytes previousValue, FixedString32Bytes newValue) =>
        {
            if(NetworkManager.Singleton.IsServer)
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);
                var player = playerObject.GetComponent<Transform>();
                player.name = playerName.Value.ToString();
                SetUserNameClientRpc(playerObject, player.name);
            }
        };
    }
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            var spriteRender = gameObject.GetComponent<SpriteRenderer>();
            spriteRender.color = tempColor.Value;
            var transform = gameObject.GetComponent<Transform>();
            transform.name = playerName.Value.ToString();
            if (IsLocalPlayer)
            {
                CinemachineVirtualDynamic.Instance.FollowPlayer(transform);
                //this.documentChars = new List<List<char>> { new List<char>() };
            }
            textSelected = false;
            mouseClickPosition = default(Vector3);   
        }
    }
    
    private void Update()
    {
 
        if(!IsOwner) return;

        if (!isConfigured)
        {
            tempColor.Value = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            playerName.Value = NetworkUI.Instance.GetUsername();
            SetScaleServerRpc(userScale);
            MoveServerRpc(new Vector3(-100, 50, 0));
            LoadDocumentServerRpc(OwnerClientId);
            DocumentManager.Instance.LoadLocalDocServerRpc();
            isConfigured = true;
            
        }
        if (!welcome && isConfigured)
        {
            ChatController.Instance.JoinedChatServerRpc(playerName.Value.ToString());
            welcome = true;
            //DocumentManager.Instance.CheckDoc();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            tempColor.Value = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }

    }

    public void ProcessInput(Vector2Int newLoc, int code = 0, string newChar = "")
    {
        #region Code definitions
        /*Code breakdown for now
         * 0 = Do nothing, just log it
         * 1 = Navigation arrows, check if target is in bounds, if so, move, if not log and return
         * 2 = Return or Enter -> this will be adding a new list and a bunch of stuff
         * 3 = Delete -> Alot here as well end of line checks
         * 4 = Backspace -> Yep... start of line checks
         * 5 = Tab or Space -> same same just different increment.  Insert a space, move cursor the same
         * 42 = Actually inserting a character
        */
        #endregion

        if (!IsOwner) { return; }
        ProcessInputServerRpc(newLoc, code, newChar);
    }

    #region Mouse actions
    public void ProcessMouseInput(Vector3 downClick, Vector3 release = default(Vector3))
    {
        //Debug.Log("User clicked here: " + downClick + "\nuser released here: " + release);
        //Debug.Log("Let's package and send to the server");
        if (!IsOwner) { return; }

        //ProcessHighlightServerRpc();        
    }

    public void ProcessSingleLeftClick(Vector3 clickLocation)
    {
        if (!IsOwner) return;
        Debug.Log("User clicked here: " + clickLocation);

        int x, y;
        Vector3 targetLoc;
        Utilities.GetListXY(clickLocation, out x, out y);
        Utilities.GetWorldXY(x, y, out targetLoc);
        mouseClickPosition = targetLoc;

        // Here we check if there's already a selection, if yes, clear it, if not... we cool
        if(textSelected)
        {
            // Clear the highlights

            // Reset the flag
            textSelected = false;
        }

        ProcessMouseMoveServerRpc(mouseClickPosition);
    }

    public void ProcessLeftClickHold(Vector3 mousePosition)
    {
        int x, y;
        Vector3 targetLoc;
        Utilities.GetListXY(mousePosition, out x, out y);
        Utilities.GetWorldXY(x, y, out targetLoc);

        if (targetLoc != mouseClickPosition)
        {
            //Debug.Log("Player moved to: " + targetLoc);
            mouseClickPosition = targetLoc;
            Vector2 rayCasPos = new Vector2(targetLoc.x, targetLoc.y);
            RaycastHit2D hit = Physics2D.Raycast(rayCasPos, Vector2.zero);
            if(hit.collider != null)
            {
                var test = hit.transform.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                ProcessHighlightServerRpc(test);
            }
        }
        ProcessMouseMoveServerRpc(targetLoc);
    }
    #endregion


    #endregion

    #region Server RPCs

    [ServerRpc]
    private void MoveServerRpc(Vector3 dir)
    {
        isMoving = true;
        transform.position = dir;
        isMoving = false;
    }

    [ServerRpc]
    private void SetScaleServerRpc(Vector2 scale)
    {
        transform.localScale = scale;
        SetScalesClientRpc(scale);
    }

    [ServerRpc]
    private void ProcessHighlightServerRpc(ulong netID, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        AnnotationsManager.Instance.ToggleHighlight(netID, clientID);
    }

    [ServerRpc]
    private void ProcessMouseMoveServerRpc(Vector3 location, ServerRpcParams serverRpcParams = default)
    {
        MoveServerRpc(location);
    }

    [ServerRpc]
    private void ProcessInputServerRpc(Vector2Int increment, int code, string newChar = "", ServerRpcParams serverRpcParams = default)
    {
        // Grab the id of the client making the request
        var clientID = serverRpcParams.Receive.SenderClientId;

        // Declare the variables needed to do the things
        int currentListX, currentListY, targetListX, targetListY;

        // Transform for the current player
        Vector3 origPosition = transform.position;

        // First determine the new location based on the move
        Vector3 targetPos = new Vector3(transform.position.x + increment.x, transform.position.y + increment.y, 0);
        
        // Get the players current list position
        Utilities.GetListXY(transform.position, out currentListX, out currentListY);
        
        // Get the players target list position
        Utilities.GetListXY(targetPos, out targetListX, out targetListY);

        //bool move = false;
        bool success = false;
        int x, y;

        if(NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            #region new Region for Arrow Keys
            if (code >= 1 && code <= 4)
            {
                MoveServerRpc(targetPos);
                success = true;
            }
            #endregion

            // Now because the player can move anywhere on the screen we need to check that 
            // if they type, they are in the list.  If not, display a message, maybe draw
            // lines for them.
            if (code > 4)
            {
                /* here we check if the player is within the bounds of the document.  If they aren't we display the message */
                if (!DocumentManager.Instance.InBounds(origPosition))
                {
                    // OnGUI message for the user for 3 seconds
                    // Debug.Log("Send something to the client");
                    ClientRpcParams clientRpcParams = new ClientRpcParams()
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientID },
                        }
                    };
                    string eMessage = "Please return to the document to enter any text";
                    NetworkUI.Instance.DisplayErrorMessageClientRpc(eMessage, clientRpcParams);
                } /* if they are, we process their movement */
                else
                {
                    success = true;
                    //DocumentManager.Instance.ThisCantBeRight(origPosition, code);
                    DocumentManager.Instance.UpdateDocumentListClientRpc(origPosition, code);
                    // New Line commands
                    if (code == 5 || code == 6)
                    {
                        targetPos.x = -100;
                        DocumentManager.Instance.InsertRow(transform.position);
                        //DocumentManager.Instance.UpdateDocumentListClientRpc(origPosition, code);
                        MoveServerRpc(targetPos);
                    }

                    // Delete
                    if (code == 7)
                    {
                        DocumentManager.Instance.Delete(transform.position, true, out y, out x);
                        MoveServerRpc(targetPos);
                    }

                    // Backspace
                    if (code == 8)
                    {
                        if (currentListX == 0 && currentListY == 0)
                        {
                            success = false;
                            ClientRpcParams clientRpcParams = new ClientRpcParams()
                            {
                                Send = new ClientRpcSendParams
                                {
                                    TargetClientIds = new ulong[] { clientID },
                                }
                            };
                            string eMessage = "You're in the first position, nothing to delete";
                            NetworkUI.Instance.DisplayErrorMessageClientRpc(eMessage, clientRpcParams);

                        }
                        else
                        {
                            // Check if they're at the top of document, if they are, set to fail and skip move
                            // if you're cool, pass a message.
                            DocumentManager.Instance.Delete(transform.position, false, out x, out y);
                            if (y != -1)
                            {
                                targetPos.y = y;
                            }
                            if (x != -1)
                            {
                                targetPos.x = x;
                            }
                            MoveServerRpc(targetPos);
                        }
                    }

                    // Space & Tab
                    if (code == 9 || code == 10)
                    {
                        // Insert character
                        DocumentManager.Instance.InsertCharacter(transform.position, newChar);
                        // Move player
                        MoveServerRpc(targetPos);
                    }

                    if (code == 42)
                    {
                        // Insert character
                        DocumentManager.Instance.InsertCharacter(transform.position, newChar);
                        // Move player
                        MoveServerRpc(targetPos);
                    }
                }
            }
        }

        #region Log log it's better than bad it's good
        // Log the move attempted by the user
        /* Here's where we can get the full payload.  For now, let's track what we're going to want
         * Date & Time
         * Username - User ID
         * User Type
         * Action attempted
         * Previous position
         * Target position
         * String - if applicable
         */

        //This is where the action is logged
        docEntry = new DocumentEntry(
            DateTime.UtcNow.ToString("MM-dd-yyyy"),
            DateTime.UtcNow.ToString("HH:mm:ss"),
            transform.gameObject.GetComponent<NetworkObject>().GetInstanceID().ToString(),  //transform.GetInstanceID().ToString(),
            transform.name,
            "Student",
            code,
            newChar,
            origPosition,
            transform.position,
            success);

        FileManager.Instance.ProcessRequest(1, new ChatMessage(), docEntry);

        #endregion
    }

    [ServerRpc]
    private void LoadDocumentServerRpc(ulong ownerClientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { ownerClientId }
            }
        };

        foreach (NetworkObject character in NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            var temp = ((NetworkObject)character).GetComponent<TextMesh>();
            if (temp != null)
            {
                var id = character.NetworkObjectId;
                var content = character.GetComponent<TextMesh>().text;
                //var content = DocumentManager.Instance.GetChar(id).ToString();
                
                bool isActive = character.transform.GetChild(0).gameObject.activeSelf;
                LoadDocumentClientRpc(id, content, isActive, clientRpcParams);
            }
        }

        for(int i = 0; i < DocumentManager.Instance.GetRowCount(); i++)
        {
            for(int j = 0; j < DocumentManager.Instance.GetColumnCount(i); j++)
            {
                //char aChar = DocumentManager.Instance.GetChar(i, j);
                //BuildDocumentListClientRpc(i, aChar, clientRpcParams);
            }
            //BuildDocumentListClientRpc(i, '\0', clientRpcParams);
        }
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void SetUserColorClientRpc(NetworkObjectReference player, Color32 color)
    {
        var tempPlayer = ((GameObject)player).GetComponent<SpriteRenderer>();
        tempPlayer.color = color;
    }
    [ClientRpc]
    private void SetUserNameClientRpc(NetworkObjectReference player, string userName)
    {
        var tempPlayer = ((GameObject)player);
        tempPlayer.name= userName;
    }
    [ClientRpc]
    private void SetScalesClientRpc(Vector2 scale) { transform.localScale = scale; }
    [ClientRpc]
    private void LoadDocumentClientRpc(ulong id, string text, bool isActive, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner || !IsLocalPlayer) return;
        var tempMesh = NetworkManager.SpawnManager.SpawnedObjects[id].GetComponent<TextMesh>();
        tempMesh.text = text;

        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[id].gameObject;
        Mesh mesh = new Mesh();
        Utilities.GetMesh(out mesh);
        //var tempObject = ((GameObject)totalComponent);
        var tempChild = tempObject.transform.GetChild(0);
        var tempFilter = tempChild.GetComponent<MeshFilter>();
        tempFilter.mesh = mesh;
        tempChild.gameObject.SetActive(isActive);
        // Debug.Log("That was: " + isActive);
    }

    #endregion

}