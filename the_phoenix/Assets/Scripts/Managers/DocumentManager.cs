using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Starace.Utils;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

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
    private int cellSize = 3;
    public bool DestroyWithSpawner;

    private string tempFileName;
    private string fileType;

    [SerializeField] public TMP_InputField fileName;
    [SerializeField] private Button saveFileButton;
    [SerializeField] private Button dismissButton;

    [SerializeField] private TextMeshProUGUI confMessage;

    //[SerializeField] private TMP_Text selectedType;
    [SerializeField] public TMP_Dropdown typeDropdown;
    public GameObject Confirmation = null;
    public GameObject SaveObjects = null;

    bool saveObj = true;
    bool confObj = false;

    private int fileIndex;

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
        if(!enabled || Instance == null) return;
        _charIds = new List<List<ulong>>
        {
            new List<ulong>()
        };
        InsertCharacter(new Vector3(-100, 50, 0), "\n", 1);
    }
    #endregion

    #region Main functions to handle the document

    #region Cursor movement
    public void MoveCursorRight(int curX, int curY, int newX, int newY, out bool move, out Vector3 newTarget)
    {
        int eol = GetColumnCount(curY)-1;
        int eod = GetRowCount()-1;
        move = true;
        newTarget = Vector3.zero;
        if (curX == eol && curY == eod)
        {
            move = false;
        }
        else if(curX == eol && curY != eod)
        {
            Utilities.GetWorldXY(0, curY+1, out newTarget); 
        }
        else
        {
            Utilities.GetWorldXY(newX, newY, out newTarget);
        }
    }
    public void MoveCursorLeft(int curX, int curY, int newX, int newY, out bool move, out Vector3 newTarget)
    {
        move = true;
        newTarget = Vector3.zero;

        if (curX == 0 && curY == 0)
        {
            move = false;
        }
        else if (curX == 0 && curY != 0)
        {
            Utilities.GetWorldXY(GetColumnCount(curY-1)-1, curY - 1, out newTarget);
        }   
        else
        {
            Utilities.GetWorldXY(newX, newY, out newTarget);
        }
            
    }
    public void MoveCursorUp(int curX, int curY, int newY, out bool move, out Vector3 newTarget)
    {
        move = true;
        newTarget = Vector3.zero;
        if (curY == 0) 
        { 
            move = false;
            return;
        }
        int prevEOL = GetColumnCount(curY - 1);

        if (curY != 0 && curX >= prevEOL)
        {
            Utilities.GetWorldXY(prevEOL-1, newY, out newTarget);
        }
        else
        {
            Utilities.GetWorldXY(curX, newY, out newTarget);
        }
    }
    public void MoveCursorDown(int curX, int curY, int newY, out bool move, out Vector3 newTarget)
    {
        move = true;
        newTarget = Vector3.zero;
        int eod = GetRowCount() - 1;
        if (curY == eod)
        {
            move = false;
            return;
        }
        int nextEOL = GetColumnCount(curY + 1);
        
        if(curY != eod && curX >= nextEOL)
        {
            Utilities.GetWorldXY(nextEOL-1, newY, out newTarget);
        }
        else
        {
            Utilities.GetWorldXY(curX, newY,out newTarget);
        }
    }
    #endregion

    #region Inserts and deletes
    public void InsertCharacter(Vector3 targetPos, string text, int code = 0)
    {
        int listPosX, listPosY;
        Utilities.GetListXY(targetPos, out listPosX, out listPosY);

        #region This region handles creating the Network object that will hold the entered character
        if (Instance == null) return;
        m_PrefabInstance = Instantiate(PrefabToSpawn);
        m_PrefabInstance.transform.position = targetPos;
        m_SpawnedNetworkObject = m_PrefabInstance.GetComponent<NetworkObject>();
        TextMesh tempMesh = m_SpawnedNetworkObject.GetComponent<TextMesh>();
        tempMesh.text = text;
        m_SpawnedNetworkObject.Spawn();
        #endregion

        #region This region handles upkeep, inserting ID into the list and shifting values
        var id = m_SpawnedNetworkObject.NetworkObjectId;
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
            //Insert the new network object id into the list
            _charIds[listPosY].Insert(listPosX, id);
        }
        #endregion
    }
    public void InsertRow(Vector3 playerPos)
    {
        List<ulong> newList= new List<ulong>();
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
        m_SpawnedNetworkObject.Spawn();
        var id = m_SpawnedNetworkObject.NetworkObjectId;

        if (listPosY == eod)
        {
            // If we're at the end of the document and the end of the line all we need to do is add
            // An empty row with a new line and a new list to the main list
            if(listPosX == eol)
            {
                newList.Add(id);
                _charIds.Add(newList);
                Utilities.GetWorldXY(0, listPosY+1, out newPos);
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
                for(int i = listPosY; i < GetRowCount(); i++)
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
        Debug.Log("We clear");

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
    #endregion
}
