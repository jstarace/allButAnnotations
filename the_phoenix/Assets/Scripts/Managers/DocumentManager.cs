using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Starace.Utils;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using Mono.Cecil.Cil;
using System.Net.Security;

#region file description
/********************************************************************************************
 * The Document Manager... Should either go with Controller, Handler or Manager... pick one *
 * This file is all about the document.  It will handle inserting characters, deleting      *
 * All things, for the document itself.  It will send off to other scripts for their        *
 * respective tasks, like, the file manager for saving and loading of anything in the       *
 * document.                                                                                * 
 *******************************************************************************************/
#endregion

public class DocumentManager : NetworkBehaviour
{

    public static DocumentManager Instance { set; get; }
    private List<List<ulong>> _charIds;
    private List<string> fileContainer;

    public GameObject PrefabToSpawn;
    private GameObject m_PrefabInstance;
    private NetworkObject m_SpawnedNetworkObject;
    private MeshFilter m_SpawnedMeshFilter;

    private int cellSize = 3;
    public bool DestroyWithSpawner;

    private string tempFileName;

    [SerializeField] public TMP_InputField fileName;
    [SerializeField] private Button saveFileButton;
    [SerializeField] private Button dismissButton;
    [SerializeField] private TextMeshProUGUI confMessage;
    [SerializeField] public TMP_Dropdown typeDropdown;
    public GameObject Confirmation = null;
    public GameObject SaveObjects = null;

    bool saveObj = true;
    bool confObj = false;

    private int fileIndex;
    private string fileType;


    private Dictionary<ulong, int> selectedCount;
    private List<List<char>> clientDocument;
    private bool configured = false;

    private Dictionary<ulong, List<ulong>> playerSelections;

    #region Initializations and startups
    private void Awake()
    {
        Instance = this;

        saveFileButton.onClick.AddListener(() =>
        {
            SaveFileForUser();
            ToggleSaveConfirm();
        });

        typeDropdown.onValueChanged.AddListener((val) =>
        {
            TypeSelection(val);
        });

        dismissButton.onClick.AddListener(() =>
        {
            confMessage.text = "";
            fileName.text = "";
            ToggleSaveConfirm();
        });
    }

    /* On spawn check that it is the server launching this.  Only allow the server
     to run and create the list of lists*/
    public override void OnNetworkSpawn()
    {
        enabled = IsServer;

        if (enabled)
        {
            Debug.Log("we get here?");
            playerSelections = new Dictionary<ulong, List<ulong>>();
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }
        if (!enabled || Instance == null)
        {
            clientDocument= new List<List<char>>();
            return;
        }
        _charIds = new List<List<ulong>>
        {
            new List<ulong>()
        };
        selectedCount= new Dictionary<ulong, int>();
        InsertCharacter(new Vector3(-100, 50, 0), "\n", 1);
    }
    #endregion

    private void Update()
    {

    }

    public void CheckDoc()
    {
        Debug.Log("Still here");
        for(int i = 0; i<clientDocument.Count; i++)
        {
            for(int j = 0; j < clientDocument[i].Count; j++)
            {
                Debug.Log(clientDocument[i][j]);
            }
        }
    }

    #region Loading the local copy of the open document

    [ServerRpc(RequireOwnership = false)]
    public void LoadLocalDocServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientID }
                }
            };
  
            for(int y = 0; y < GetRowCount(); y++)
            {
                ClientDocAddRowClientRpc(clientRpcParams);
                for (int x=0; x < GetColumnCount(y); x++)
                {
                    char temp = NetworkManager.SpawnManager.SpawnedObjects[_charIds[y][x]].GetComponent<TextMesh>().text[0];
                    ClientLoadDocClientRpc(y, temp, clientRpcParams);
                }
            }
        }
    }

    [ClientRpc]
    private void ClientLoadDocClientRpc(int y, char text, ClientRpcParams clientRpcParams = default)
    {
        clientDocument[y].Add(text);
    }
    [ClientRpc]
    private void ClientDocAddRowClientRpc(ClientRpcParams clientRpcParams = default)
    {
        List<char> tempList = new List<char>();
        clientDocument.Add(tempList);
    }
    #endregion

    [ClientRpc]
    public void UpdateDocumentListClientRpc(Vector3 worldPos, int code, string newChar = "")
    {
        if (IsServer) return;
        int x, y;
        Utilities.GetListXY(worldPos, out x, out y);
        int eod = this.clientDocument.Count - 1;
        int eol = this.clientDocument[y].Count - 1;
        List<char> newList = new List<char>();

        if (code == 5 || code == 6)
        {
            if (x == eol)
            {
                newList.Add('\n');
                this.clientDocument.Add(newList);
            }
            else
            {
                List<char> original = new List<char>();
                Utilities.SplitList(this.clientDocument[y], x, out original, out newList);
                original.Add('\n');
                this.clientDocument.Insert(y, newList);
                this.clientDocument.Insert(y, original);
                this.clientDocument.RemoveAt(y + 2);
            }
        }
        else if (code == 7 || code == 8)
        {
            bool startOfLine = x == 0;
            bool endOfLine = x == this.clientDocument[y].Count - 1;
            bool topOfDocument = y == 0;
            bool bottomOfDocument = y == this.clientDocument.Count - 1;

            // 7 is true, 8 is false
            if (code == 7)
            {
                if (bottomOfDocument && endOfLine) return;
                else if (!bottomOfDocument && endOfLine)
                {
                    int nextRow = y + 1;
                    Utilities.JoinTwoLists(this.clientDocument[y], this.clientDocument[nextRow], out newList);
                    this.clientDocument[y] = newList;
                    this.clientDocument.RemoveAt(nextRow);
                    this.clientDocument[y].RemoveAt(x);
                }
                else
                {
                    this.clientDocument[y].RemoveAt(x);
                }
            }
            else
            {
                if (startOfLine && topOfDocument) return;
                else if (startOfLine && !topOfDocument)
                {
                    int prevRow = y - 1;
                    int lastCharInPrev = this.clientDocument[prevRow].Count - 1;

                    this.clientDocument[prevRow].RemoveAt(lastCharInPrev);
                    Utilities.JoinTwoLists(this.clientDocument[prevRow], this.clientDocument[y], out newList);
                    this.clientDocument[prevRow] = newList;
                    this.clientDocument.RemoveAt(y);
                }
                else
                {
                    this.clientDocument[y].RemoveAt(x - 1);
                }
            }
            // Do something else
        }
        else if (code == 9 || code == 10 || code == 42)
        {
            //Debug.Log(newChar);
            this.clientDocument[y].Insert(x, newChar[0]);
        }
        else if (code == 42)
        {
            // The last thing
        }
        int charCount = 0;
        for (int i = 0; i < this.clientDocument.Count; i++)
        {
            for (int j = 0; j < this.clientDocument[i].Count; j++)
            {
                charCount++;
            }
        }
        Debug.Log("The count is: " + charCount);
    }

    #region Main functions to handle the document

    #region Inserts and deletes
    public void InsertCharacter(Vector3 targetPos, string text, int code = 0)
    {
        // targetPos refers to the target for the inserted value, not the target position of the player
        int listPosX, listPosY;
        Utilities.GetListXY(targetPos, out listPosX, out listPosY);

        #region This region handles creating the Network object that will hold the entered character
        if (Instance == null) return;
        m_PrefabInstance = Instantiate(PrefabToSpawn);
        m_PrefabInstance.transform.position = targetPos;
        m_SpawnedNetworkObject = m_PrefabInstance.GetComponent<NetworkObject>();
        TextMesh tempMesh = m_SpawnedNetworkObject.GetComponent<TextMesh>();
        tempMesh.text = text;

        Mesh mesh = new Mesh();
        Utilities.GetMesh(out mesh);

        var tempChild = m_PrefabInstance.transform.GetChild(0);
        m_SpawnedMeshFilter = tempChild.GetComponent<MeshFilter>();
        m_SpawnedMeshFilter.mesh = mesh;
        tempChild.gameObject.SetActive(false);
        m_SpawnedNetworkObject.Spawn();
        #endregion

        #region This region handles upkeep, inserting ID into the list and shifting values
        var id = m_SpawnedNetworkObject.NetworkObjectId;

        /*
         * Once we have an object id we want to add it into the dictionary as a key with a new list
         * then we can handle everything from the insert functions
        */

        //Debug.Log("This should fire");
        if (!playerSelections.ContainsKey(id))
        {
            playerSelections.Add(id, new List<ulong>());
        }

        if (code == 1)
        {
            _charIds[listPosY].Add(id);
        }
        else
        {
            // Shift the position of all existing objects
            ShiftRow(listPosY, listPosX, cellSize);
            // Update the text of new object on all clients
            SetTextClientRpc(m_SpawnedNetworkObject, text);
            AddMeshClientRpc(m_PrefabInstance);
            //Insert the new network object id into the list
            _charIds[listPosY].Insert(listPosX, id);
            //if (!playerSelections.ContainsKey(id)) playerSelections.Add(id, new List<ulong>());
        }
        #endregion
    }

    public void InsertRow(Vector3 playerPos)
    {
        List<ulong> newList = new List<ulong>();
        int listPosX, listPosY;
        Utilities.GetListXY(playerPos, out listPosX, out listPosY);
        Vector3 newPos;

        //Get End of Document(EOD) & End of Line(EOL)
        int eod = GetRowCount() - 1;
        int eol = GetColumnCount(listPosY) - 1;

        //Create base components
        m_PrefabInstance = Instantiate(PrefabToSpawn);
        m_SpawnedNetworkObject = m_PrefabInstance.GetComponent<NetworkObject>();
        TextMesh tempMesh = m_SpawnedNetworkObject.GetComponent<TextMesh>();

        Mesh mesh = new Mesh();
        Utilities.GetMesh(out mesh);

        var tempChild = m_PrefabInstance.transform.GetChild(0);
        m_SpawnedMeshFilter = tempChild.GetComponent<MeshFilter>();
        m_SpawnedMeshFilter.mesh = mesh;
        tempChild.gameObject.SetActive(false);
        m_SpawnedNetworkObject.Spawn();

        var id = m_SpawnedNetworkObject.NetworkObjectId;

        if (listPosY == eod)
        {
            // If we're at the end of the document and the end of the line all we need to do is add
            // An empty row with a new line and a new list to the main list
            if (listPosX == eol)
            {
                newList.Add(id);
                _charIds.Add(newList);
                Utilities.GetWorldXY(0, listPosY + 1, out newPos);
                m_SpawnedNetworkObject.transform.position = newPos;
            }
            else
            {
                List<ulong> originalList = new List<ulong>();
                SplitList(_charIds[listPosY], listPosX, out originalList, out newList);
                originalList.Add(id);
                _charIds.Insert(listPosY, newList);
                _charIds.Insert(listPosY, originalList);
                _charIds.RemoveAt(listPosY + 2);
                for (int i = listPosY; i < GetRowCount(); i++)
                {
                    refreshRowPositions(i);
                }
            }
        }
        else
        {
            List<ulong> originalList = new List<ulong>();
            SplitList(_charIds[listPosY], listPosX, out originalList, out newList);
            originalList.Add(id);
            _charIds.Insert(listPosY, newList);
            _charIds.Insert(listPosY, originalList);
            _charIds.RemoveAt(listPosY + 2);
            for (int i = listPosY; i < GetRowCount(); i++)
            {
                refreshRowPositions(i);
            }
        }
        tempMesh.text = "\n";
        SetTextClientRpc(m_SpawnedNetworkObject, "\n");
        AddMeshClientRpc(m_PrefabInstance);
    }

    public void Delete(Vector3 playerPos, bool isDelete, out int x, out int y)
    {
        int listPosX, listPosY;
        Utilities.GetListXY(playerPos, out listPosX, out listPosY);
        bool startOfLine = listPosX == 0;
        bool endOfLine = listPosX == GetColumnCount(listPosY)-1;
        bool topOfDocument = listPosY == 0;
        bool bottomOfDocument = listPosY == GetRowCount()-1;

        x = -1; y = -1;
        if (!isDelete)
        {
            // Here we handle all things backspace
            if (startOfLine && topOfDocument) return;
            // Handle combining two lines
            else if (startOfLine && !topOfDocument)
            {
                int prevRow = listPosY - 1;
                int lastCharPos = GetColumnCount(prevRow) - 1;

                NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[prevRow][lastCharPos]].GetComponent<NetworkObject>();
                _charIds[prevRow].RemoveAt(lastCharPos);
                tempNO.Despawn();
                _charIds[prevRow] = JoinTwoLists(_charIds[prevRow], _charIds[listPosY]);
                _charIds.RemoveAt(listPosY);
                for (int i = 0; i < GetRowCount(); i++)
                {
                    refreshRowPositions(i);
                }
                y = (prevRow * -cellSize) + 50;
                x = (lastCharPos * cellSize) - 100;
            }
            else
            {
                // Handle deleting a character within the line
                NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[listPosY][listPosX - 1]].GetComponent<NetworkObject>();
                _charIds[listPosY].RemoveAt(listPosX - 1);
                tempNO.Despawn();
                refreshRowPositions(listPosY);
            }
        }
        else
        {
            //Here we handle all things delete
            if (bottomOfDocument && endOfLine) return;
            else if (!bottomOfDocument && endOfLine)
            {
                int nextRow = listPosY + 1;
                _charIds[listPosY] = JoinTwoLists(_charIds[listPosY], _charIds[nextRow]);
                _charIds.RemoveAt(nextRow);
                NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[listPosY][listPosX]].GetComponent<NetworkObject>();
                _charIds[listPosY].RemoveAt(listPosX);
                tempNO.Despawn();
                for (int i = 0; i < GetRowCount(); i++)
                {
                    refreshRowPositions(i);
                }
            }
            else
            {
                NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[listPosY][listPosX]].GetComponent<NetworkObject>();
                _charIds[listPosY].RemoveAt(listPosX);
                tempNO.Despawn();
                refreshRowPositions(listPosY);
            }
        }
    }
    #endregion

    #region File saving
    private void TypeSelection(int index)
    {
        //string fileType;
        switch (index)
        {
            case 0:
                fileType = ".txt";
                fileIndex= 0;
                break;
            case 1:
                fileType = ".cs";
                fileIndex= 1;
                break;
            case 2:
                fileType = ".cpp";
                fileIndex= 2;
                break;
            case 3:
                fileType = ".py";
                fileIndex= 3;
                break;
            default:
                break;
        }
    }
    private void SaveFileForUser()
    {
        tempFileName = fileName.text;
        ExportFileServerRpc(tempFileName, fileIndex);
    }

    private void ToggleSaveConfirm()
    {
        saveObj = !saveObj;
        SaveObjects.SetActive(saveObj);

        confObj = !confObj;
        Confirmation.SetActive(confObj);
    }


    #endregion

    #region Document Reset
    public void ClearDocument()
    {
        //Debug.Log("We clear");

        for (int i = 0; i < _charIds.Count; i++)
        {
            for (int j = 0; j < _charIds[i].Count; j++)
            {
                var netId = _charIds[i][j];
                NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[netId];
                tempNO.Despawn();
            }
        }
        _charIds = new List<List<ulong>> { new List<ulong>() };
        fileContainer = new List<string>();
        InsertCharacter(new Vector3(-100, 50, 0), "\n", 1);
    }
    #endregion

    #endregion

    #region ServerRpcs

    [ServerRpc(RequireOwnership = false)]
    public void ToggleHighlightServerRpc(Vector2 loc, bool value, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {

            /* 
             * Here's where we'll build active selections.. we'll need to track the document
             * and which characters have an active selection and who the owner is
             * if they are the ONLY owner they can remove it, otherwise just remove their
             * id from the list but keep it selected.
             * So we'll need a bool to check prior to toggle
            */

            /*
             * there are four specific scenarios to cover
             * 1. Request to highlight
             *    a. Check to see if the text is already highlighted
             *       if yes - Register them as having it highlighted
             *                Don't toggle
             *       if no  - Register them as having it highlighted
             *                toggle 
             * 
             * 2. Request to Remove highlight
             * 
            */

            

            Vector3 worldPos = new Vector3(loc.x, loc.y);
            //Debug.Log("Here's the pos: (" + worldPos.x + ", " + worldPos.y +")");
            var netID = _charIds[(int)loc.y][(int)loc.x];
            var toggle = true;

            if (!value)
            {
                playerSelections.TryGetValue(netID, out var removeList);
                removeList.Remove(clientID);
                if (removeList.Count > 0) toggle = false;
            }
            else if (value)
            {
                if(playerSelections.ContainsKey(netID))
                {
                    Debug.Log("uh.....");
                    if (playerSelections[netID].Count == 0) playerSelections[netID].Add(clientID);
                    else
                    {
                        foreach (var item in playerSelections[netID])
                        {
                            if (item == clientID) Debug.Log("Should not happen");
                            else playerSelections[netID].Add(clientID);
                        }
                    }

                    if (playerSelections[netID].Count > 1) toggle = false;
                    Debug.Log(playerSelections[netID].Count);
                }
            }
            if (playerSelections.TryGetValue(netID, out var theList))
            {
                //Debug.Log("ok, this is happening");
                /*
                 * Ok, we have the dictionary created with the netID of the text.  Now we need to look up
                 * if the player requesting is added to the contained list, if not add them,
                 * if they are... do nothing?
                */
                
                //if (value) theList.Add(clientID);
                //if (!value) theList.Remove(clientID);

                //Debug.Log("The network ID is: " + netID + "\nThe value is: " + value);



                //else if (!value) theList.Remove(clientID);
                //if (theList.Count > 0) toggle = false;
            }

            if(toggle)
            {
                var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
                var tempChild = tempObject.transform.GetChild(0);
                bool state = tempChild.gameObject.activeSelf;
                tempChild.gameObject.SetActive(!state);
                ToggleHighlightClientRpc(netID, !state);

            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHighlightsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            for(int i=0; i<_charIds.Count; i++)
            {
                for(int j=0; j < _charIds[i].Count; j++)
                {
                    var netID = _charIds[i][j];
                    var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
                    var tempChild = tempObject.transform.GetChild(0);
                    bool state = tempChild.gameObject.activeSelf;
                    tempChild.gameObject.SetActive(false);
                    ToggleHighlightClientRpc(netID, false);
                }
            }
        }
    }

    [ClientRpc]
    private void ToggleHighlightClientRpc(ulong netID, bool state)
    {
        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
        var tempChild = tempObject.transform.GetChild(0);
        //bool state = tempChild.gameObject.activeSelf;
        tempChild.gameObject.SetActive(state);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExportFileServerRpc(string fileName, int fileIndex, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientID },
                }
            };
            
            // Empty the 'file' on the client so they can receive the current document version
            FileManager.Instance.EmptyContainerClientRpc(clientRpcParams);

            // This builds out the most recent document for the client
            for (int i = 0; i < _charIds.Count; i++)
            {
                string newLine = "";
                for (int j = 0; j < _charIds[i].Count; j++)
                {
                    NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[i][j]].GetComponent<NetworkObject>();
                    TextMesh textMesh = tempNO.GetComponent<TextMesh>();
                    newLine += textMesh.text;
                }
                NetworkString temp = new NetworkString() { st = newLine };
                FileManager.Instance.BuildFileClientRpc(temp, clientRpcParams);
            }
            FileManager.Instance.SaveFileClientRpc(fileName, fileIndex, clientRpcParams);
        }
    }
    #endregion

    #region ClientRpcs
    [ClientRpc]
    private void AddMeshClientRpc(NetworkObjectReference totalComponent)
    {
        Mesh mesh = new Mesh();
        Utilities.GetMesh(out mesh);

        var tempObject = ((GameObject)totalComponent);
        var tempChild = tempObject.transform.GetChild(0);
        var tempFilter = tempChild.GetComponent<MeshFilter>();
        tempChild.gameObject.SetActive(false);
        tempFilter.mesh = mesh;
    }

    [ClientRpc]
    private void SetTextClientRpc(NetworkObjectReference mesh, string text)
    {
        var tempMesh = ((GameObject)mesh);
        tempMesh.GetComponent<TextMesh>().text = text;
    }

    public void SetConfirmationText(string message)
    {
        Debug.Log(message);
        confMessage.text = message;
    }
    #endregion

    #region Helper functions... maybe one day we move em to utilities

    private void refreshRowPositions(int row)
    {
        int yLocalPosition = (row * -cellSize) + 50;
        for(int i = 0; i < GetColumnCount(row); i++)
        {
            int xLocalPosition = (i * cellSize) - 100;
            ulong NetId = _charIds[row][i];
            //// Debug.Log("Here's the IDs" + NetId);
            NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[NetId];
            Transform tempTransform = tempNO.GetComponent<Transform>();
            Vector3 newPos = new Vector3(xLocalPosition, yLocalPosition, 0);
            tempTransform.localPosition = newPos;
        }
    }
    private void ShiftRow(int row, int column, int increment)
    {
        for (int i = column; i < GetColumnCount(row); i++)
        {
            NetworkObject tempNO = NetworkManager.SpawnManager.SpawnedObjects[_charIds[row][i]].GetComponent<NetworkObject>();
            Transform tempTransform = tempNO.GetComponent<Transform>();
            Vector3 newPos = new Vector3(tempTransform.position.x + increment, tempTransform.position.y, 0);
            tempTransform.localPosition = newPos;
        }
    }
    public void PlayerBoundaryCheck()
    {
        if (!IsServer) return;
        foreach(var netId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            //First look up the player
            int id = (int)netId;
            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(netId);
            
            //Get and convert player position to list pointer
            int playerX, playerY;
            int newPlayerX, newPlayerY;
            Utilities.GetListXY(player.transform.position, out playerX, out playerY);

            //First we make sure the player is with the rows of the list
            int totalRowsInDocument = GetRowCount()-1;
            if(playerY > totalRowsInDocument)
            {
                newPlayerY = totalRowsInDocument;
            }
            else
            {
                newPlayerY = playerY;
            }

            //Now we check the specific row
            int tolalCharactersInRow = GetColumnCount(newPlayerY)-1;

            if(playerX > tolalCharactersInRow)
            {
                newPlayerX = tolalCharactersInRow;
            }
            else
            {
                newPlayerX = playerX;
            }

            //Create a new world position with values
            Vector3 newPos;
            Utilities.GetWorldXY(newPlayerX, newPlayerY, out newPos);

            if (player.transform.position != newPos)
            {
                player.transform.position = newPos;
            }
        }
    }
    private void SplitList<T>(List<T> incomingList, int splitPoint, out List<T> list_1, out List<T> list_2)
    {
        list_1 = incomingList.GetRange(0, splitPoint);
        list_2 = incomingList.GetRange(splitPoint, incomingList.Count - splitPoint);
    }
    private List<T> JoinTwoLists<T>(List<T> list_1, List<T> list_2)
    {
        List<T> tempList = new List<T>();
        tempList.AddRange(list_1);
        tempList.AddRange(list_2);
        return tempList;
    }
    #endregion

    #region Getters
    public int GetColumnCount(int row) { return _charIds[row].Count; }
    public int GetRowCount() { return _charIds.Count; }
    public ulong GetObjectID(int x, int y) { return _charIds[x][y]; }

    public char GetChar(int x, int y)
    {
        char theChar = ' ';
        theChar = NetworkManager.SpawnManager.SpawnedObjects[_charIds[y][x]].GetComponent<TextMesh>().text.ToCharArray()[0];
        return theChar;
    }
    
    public bool InBounds(Vector3 pos)
    {
        bool result = true;
        // so this checks if the player is in the current list
        int xPos, yPos, xMax, yMax;
        Utilities.GetListXY(pos, out xPos, out yPos);

        if(xPos < 0 || yPos < 0)
        {
            result = false;
            return result;
        }

        yMax = GetRowCount();

        if( yPos >= yMax )
        {
            result = false;
            return result;
        }
        else
        {
            xMax = GetColumnCount(yPos);
            if (xPos >= xMax) result = false;
        }     
     
        return result;
    }
    
    public int GetLocalColumnCount(int y)
    {
        if(y >= clientDocument.Count)
        {
            return clientDocument[clientDocument.Count - 1].Count; 
        }
        else if(y < 0)
        {
            return clientDocument[0].Count;
        }
        else
        {
            return clientDocument[y].Count;
        }   
    }

    public int GetLocalRowCount()
    {
        return clientDocument.Count;
    }

    public bool LocalInBounds(Vector3 pos)
    {
        bool result = true;
        // so this checks if the player is in the current list
        int xPos, yPos, xMax, yMax;
        Utilities.GetListXY(pos, out xPos, out yPos);

        if (xPos < 0 || yPos < 0)
        {
            result = false;
            return result;
        }

        yMax = clientDocument.Count;

        if (yPos >= yMax)
        {
            result = false;
            return result;
        }
        else
        {
            xMax = clientDocument[yPos].Count;
            if (xPos >= xMax) result = false;
        }

        return result;
    }
    #endregion

    #region Some Print statements for debugging
    public void PrintList()
    {
        int index = _charIds.Count;
        for(int i = 0; i<index; i++)
        {
            int index_2 = _charIds[i].Count;
            for(int j = 0; j<index_2; j++)
            {
                // Debug.Log("Here's the thing? " + _charIds[i][j]);
            }
        }
        
    }
    private void PrintFile(List<string> file)
    {
        for (int i = 0; i < file.Count; i++)
        {
            // Debug.Log(file[i].ToString());
        }
    }
    public void PrintListOverview()
    {
        // Debug.Log("The list currently has " + GetRowCount() +" row(s)");
        for(int i=0; i<GetRowCount(); i++)
        {
            // Debug.Log("Row " + i + " has " + GetColumnCount(i) + " columns");
        }
    }

    public void DrawDocumentBorders()
    {
        Debug.Log("trying to");
        for (int i = 0; i < _charIds.Count; i++)
        {
            Vector3 start = new Vector3();
            Vector3 end = new Vector3();
            for (int j = 0; j < _charIds[i].Count; j++)
            {
                Utilities.GetWorldXY(i, j, out start);
                Utilities.GetWorldXY(i, j+1, out start);
                Debug.DrawLine(start, end, Color.white, 3);
            }
        }
    }

    #endregion



}