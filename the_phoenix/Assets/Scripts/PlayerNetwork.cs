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
    private Vector3 mousePreviousPosition;

    private Dictionary<Vector2, bool> localSelection;

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
                localSelection = new Dictionary<Vector2, bool>();
                //this.documentChars = new List<List<char>> { new List<char>() };
            }
            textSelected = false;
            mouseClickPosition = default(Vector3);
            mousePreviousPosition= default(Vector3);
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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DocumentManager.Instance.CheckDoc();
        }

    }
    #endregion
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
        DocumentManager.Instance.ResetHighlightsServerRpc();
    }

    #region Mouse actions

    public void ProcessSingleLeftClick(Vector3 clickLocation)
    {
        if (!IsOwner) return;
        DocumentManager.Instance.ResetHighlightsServerRpc();

        int x, y;
        Vector3 targetLoc;
        Utilities.GetListXY(clickLocation, out x, out y);
        Utilities.GetWorldXY(x, y, out targetLoc);
        mouseClickPosition = targetLoc;
        mousePreviousPosition= targetLoc;

        if (DocumentManager.Instance.LocalInBounds(clickLocation))
        {
            Debug.Log("Inbounds");
        }
        else
        {
            Debug.Log("Missed it by that much");
        }
        

        // Here we check if there's already a selection, if yes, clear it, if not... we cool
        if(textSelected)
        {
            // Clear the highlights
            DocumentManager.Instance.ResetHighlightsServerRpc();

            // Reset the dictionary
            localSelection = new Dictionary<Vector2, bool>();
            // Reset the flag
            textSelected = false;
        }
        // move the user
        ProcessMouseMoveServerRpc(mouseClickPosition);

        //Check if click is in the document

/*        if (DocumentManager.Instance.LocalInBounds(mouseClickPosition))
        {
            textSelected = true;
            localSelection.Add(new Vector2(x, y), true);
            Debug.Log("Added (" + x+ ", " + y + ")");
        }
        */
    }

    private void PreProcessHighlight(Vector2 loc)
    {
        if (!localSelection.TryGetValue(loc, out bool value))
        {
            value = true;
            localSelection.Add(loc, value);
        }
        else
        {
            value = !localSelection[loc];
            localSelection[loc] = value;
        }

        DocumentManager.Instance.ToggleHighlightServerRpc(loc, localSelection[loc]);
    }


    public void ProcessLeftClickHold(Vector3 mousePosition)
    {
        int newX, newY, origX, origY, prevX, prevY;
        Vector3 targetLoc;
        Utilities.GetListXY(mousePreviousPosition, out prevX, out prevY);
        Utilities.GetListXY(mouseClickPosition, out origX, out origY);
        Utilities.GetListXY(mousePosition, out newX, out newY);
        Utilities.GetWorldXY(newX, newY, out targetLoc);

        /* 
         * We have to take into consideration that this will be a constant stream of data
         * So, if the player is just clicking and holding, we don't want to continuing
         * processing anything and just return... right?
         *         
        */

        var inbounds = DocumentManager.Instance.LocalInBounds(mousePosition);
        if (prevX == newX && prevY == newY)
        {
            return;
        }
        else
        {
            ProcessMouseMoveServerRpc(targetLoc);

            /*
             * Now let's check boundarys.  We want the player to be able to move whereever they want
             * But we don't want to worry about highlighting anything outside of the document, so we can
             * do some things to check it out.
             * 
            */
            

            if(newY == origY)
            {
                if(newX > origX)
                {
                    if(newX > prevX && inbounds) PreProcessHighlight(new Vector2(newX - 1, newY));
                    else if(newX <= prevX && inbounds) PreProcessHighlight(new Vector2(newX, newY));
                }
                else if(newX <= origX)
                {
                    if (newX < prevX && inbounds) PreProcessHighlight(new Vector2(newX, newY));
                    else if(newX >= prevX && inbounds) PreProcessHighlight(new Vector2(newX-1, newY));
                }
                else
                {
                    if (inbounds) PreProcessHighlight(new Vector2(newX, newY));   
                }

                //TODO: Handle the logic of returning to this line from above and below
            }
            else if(newY > origY)
            {
                int maxY = DocumentManager.Instance.GetLocalRowCount();
                
                if(newY > prevY && newY < maxY)//inbounds)
                {
                    for(int i = prevY; i <= newY; i++)
                    {
                        if (i == prevY)
                        {
                            for(int j = prevX; j < DocumentManager.Instance.GetLocalColumnCount(i); j++)
                            {
                                PreProcessHighlight(new Vector2(j, i));
                            }
                        }
                        else
                        {
                            var maxX = DocumentManager.Instance.GetLocalColumnCount(i);
                            if(newX < maxX)
                            {
                                for (int j = 0; j < newX; j++)
                                {
                                    PreProcessHighlight(new Vector2(j, i));
                                }
                            }
                            else
                            {
                                for (int j = 0; j < maxX; j++)
                                {
                                    PreProcessHighlight(new Vector2(j, i));
                                }
                            }

                        }
                    }
                }
                else if(newY < prevY && newY < maxY)
                {
                    for(int i = prevY; i >= newY; i--)
                    {
                        if(i == prevY)
                        {
                            var maxX = DocumentManager.Instance.GetLocalColumnCount(i);
                            if(prevX < maxX)
                            {
                                for (int j = maxX-1; j>=0; j--)
                                {
                                    PreProcessHighlight(new Vector2(j, i));
                                }
                            }
                            else
                            {
                                for (int j = prevX - 1; j >= 0; j--)
                                {
                                    PreProcessHighlight(new Vector2(j, i));
                                }
                            }
                        }
                        else
                        {
                            for (int j = DocumentManager.Instance.GetLocalColumnCount(newY)+1; j >= newX; j--)
                            {
                                PreProcessHighlight(new Vector2(j, i));
                            }
                        }
                    }
                }
                else if(newY == prevY)
                {
                    Debug.Log("You stayed in the same place");
                }
            }
            else if(newY < origY)
            {
                

            }



            mousePreviousPosition = mousePosition;
            return;
        }


        /* 
         * Here's where the fun will happen... Let me type out my thoughts
         * First thing.  Check to see if we moved.  If we did, did we move a column or a row?
         * Are we still in the document?  Are we do the right or left of the document?
         * How many in between?
         * 
         * I don't think we need to check movement.  Let's think it out.  The first click is always going
         * to be constant and in place.  We shouldn't be adding the first click no matter what.  it should only be added
         * when we highlight in one direction or another...
         * 
         * To the right and down it's included
         * To the left and up it's excluded
         * 
        */ 



        /*
        if (targetLoc != mouseClickPosition)
        {
            // Let's get the original x and y
            int origX, origY;
            Utilities.GetListXY(mouseClickPosition, out origX, out origY);
            int maxY = DocumentManager.Instance.GetLocalRowCount();

            if (origY > maxY && newY > maxY)
            {
                origY= maxY;
                newY = maxY;
            }
            if(origY < 0 && newY < 0)
            {
                origY = 0;
                newY = 0;
            }
            if(origY != newY)
            {
                int[] yBounds = { origY, newY };
                int[] xBounds = { origX, newX };
                
                if(origX < 0)
                {
                    origX = 0;
                }
                if(newX < 0)
                {
                    newX = 0;
                }
                // Is the new location below the old
                if (origY < newY && origY < DocumentManager.Instance.GetLocalRowCount())
                {
                    for(int i = origX; i < DocumentManager.Instance.GetLocalColumnCount(yBounds[0]); i++)
                    {
                        PreProcessHighlight(new Vector2(i, origY));
                    }
                    int xLimit = DocumentManager.Instance.GetLocalColumnCount(yBounds[1]);
                    if (newX <= origX)
                    {
                        for (int i = 0; i < origX; i++)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                        }
                    }
                    else if(newX > xLimit)
                    {
                        for (int i = 0; i < xLimit; i++)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                        }
                    }
                }
                else if(origY > newY)
                {
                    for (int i = origX; i >= 0; i--)
                    {
                        PreProcessHighlight(new Vector2(i, origY));
                    }
                    int xLimit = DocumentManager.Instance.GetLocalColumnCount(yBounds[1])-1;

                    if (newX >= 0)
                    {
                        for (int i = xLimit; i >= newX; i--)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                        }
                    }
                }
            }
            else
            {
                int xLimit = DocumentManager.Instance.GetLocalColumnCount(newY);
                if (origX < newX && newX > -1)
                {
                    if(newX <= xLimit)
                    {
                        for(int i = origX-1; i<newX; i++)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                            //Debug.Log("(" + i + ", " + newY + ")");
                        }
                    }
                    else
                    {
                        for(int i = origX; i<xLimit; i++)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                            //Debug.Log("(" + i + ", " + newY + ")");
                        }
                    }
                }
                else if( origX > newX && newX < xLimit )
                {
                    if(newX <= 0)
                    {
                        for (int i = origX; i > -1; i--)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                            //Debug.Log("(" + i + ", " + newY + ")");
                        }
                    }
                    else
                    {
                        for (int i = origX; i > newX; i--)
                        {
                            PreProcessHighlight(new Vector2(i, newY));
                            //Debug.Log("(" + i + ", " + newY + ")");
                        }
                    }
                }
            }

            mouseClickPosition = targetLoc;
            
            Vector2 rayCasPos = new Vector2(targetLoc.x, targetLoc.y);
            RaycastHit2D hit = Physics2D.Raycast(rayCasPos, Vector2.zero);
            if(hit.collider != null)
            {
                var test = hit.transform.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                //ProcessHighlightServerRpc(test);
            }
            
        }*/
        //ProcessMouseMoveServerRpc(targetLoc);
    }
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
                    DocumentManager.Instance.UpdateDocumentListClientRpc(origPosition, code, newChar);
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