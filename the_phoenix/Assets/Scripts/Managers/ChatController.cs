using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatController : NetworkBehaviour
{
    [SerializeField] private Button chatSubmitButton;
    [SerializeField] public TMP_InputField chatInput;
    [SerializeField] private TextMeshProUGUI chatWindow;

    public static ChatController Instance { set; get; }
    private static ChatMessage chatMessage;
    private static List<ChatMessage> chatMessages = new List<ChatMessage>();
    private static LogEntry logEntry;
    //private int code;
    private bool firstMessage;
    //private string message;
    
    private void Awake()
    {
        Instance = this;
        chatSubmitButton.onClick.AddListener(() =>
        {
            SendMessageToServer(chatInput.text);
            chatInput.text = null;
            chatInput.ActivateInputField();
        });
        firstMessage= false;
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var tempMessage = BuildServerMessage(OwnerClientId.ToString(), "Server", "I was Initialized");
            chatMessages.Add(tempMessage);
        }
        if (!NetworkManager.Singleton.IsServer && IsClient)
        {
            LoadChatLogServerRpc();
        }
    }

    private void Update()
    {
        /* 
         * If the server is restarted during the day, we want to preserve any previous logs
         * So on the server start, we run this check.  This will test out processing a request
         * Which will create a save any previous file and create a new one for current session
        */
        if (NetworkManager.Singleton.IsServer && !firstMessage)
        {
            logEntry = CreateEntry(OwnerClientId, "Server", "Admin", "Initialization Message", "I was Initialized");
            FileManager.Instance.ProcessRequests(logEntry);
            firstMessage = true;
        }
    }

    private void SendMessageToServer(string newMessage)
    {
         ReceiveChatMessageServerRpc(newMessage);
    }

    public void ClearChatMessages()
    {
        chatMessages.Clear();
        ClearChatLogsClientRpc();
        var tempMessage = BuildServerMessage(OwnerClientId.ToString(), "Server User", "Professor Reset the chat");
        chatMessages.Add(tempMessage);
        AllLoadChatClientRpc(tempMessage);
    }

    #region ServerRpcs
    [ServerRpc(RequireOwnership = false)]
    
    public void JoinedChatServerRpc(string name, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            var tempMessage = BuildServerMessage("0", "Server User", string.Format("Welcome {0} to the lecture.", name));
            chatMessages.Add(tempMessage);
            LoadMessageClientRpc(tempMessage);
            logEntry = CreateEntry(0, "Server", "admin", "Welcome User", string.Format("Welcome {0} to the lecture.", name));
            FileManager.Instance.ProcessRequests(logEntry);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReceiveChatMessageServerRpc(string newMessage, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            var tempUser = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
            var tempMessage = BuildServerMessage(clientID.ToString(), tempUser.name, newMessage);
            chatMessages.Add(tempMessage);
            LoadMessageClientRpc(tempMessage);
            logEntry = CreateEntry(clientID, tempUser.name, "user", "Chat Message", newMessage);
            FileManager.Instance.ProcessRequests(logEntry);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadChatLogServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientID }
                }
            };
            foreach (var message in chatMessages)
            {
                LoadMessageClientRpc(message, clientRpcParams);
            }
        }
    }
    #endregion

    #region ClientRpcs
    [ClientRpc]
    private void LoadMessageClientRpc(ChatMessage theThing, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        chatWindow.text += BuildMessageString(theThing);
    }

    [ClientRpc]
    private void ClearChatLogsClientRpc()
    {
        chatWindow.text = string.Empty;
    }

    [ClientRpc]
    private void AllLoadChatClientRpc(ChatMessage theThing)
    {
        chatWindow.text += BuildMessageString(theThing);
    }
    #endregion

    #region Helper functions
    private void PrintMessage(ChatMessage x)
    {
        Debug.Log(x._userId.st);
        Debug.Log(x._date.st);
        Debug.Log(x._time.st);
        Debug.Log(x._userName.st);
        Debug.Log(x._message.st);

    }

    private string BuildMessageString(ChatMessage theThing)
    {
        string returnMessage = "";
        if (theThing._userId.st == "0")
        {
            returnMessage = string.Format("<color=white>Message from {0}\n {1} on:\n {2} at {3} (UTC)\n\n</color>", theThing._userName.st, theThing._message.st, theThing._date.st, theThing._time.st);
            return returnMessage;
        }
        else if (NetworkManager.LocalClientId.ToString() == theThing._userId.st)
        {
            returnMessage = string.Format("<color=green>[{0} {1} (UTC)] {2}: {3}\n\n</color>", theThing._date.st, theThing._time.st, theThing._userName.st, theThing._message.st);
            return returnMessage;
        }
        else
        {
            returnMessage = string.Format("<color=yellow>[{0} {1} (UTC)] {2}: {3}\n\n</color>", theThing._date.st, theThing._time.st, theThing._userName.st, theThing._message.st);
            return returnMessage;
        }
    }

    private ChatMessage BuildServerMessage(string id, string name, string message)
    {
        var chatMessage = new ChatMessage();
        chatMessage._userId = new NetworkString() { st = id };
        chatMessage._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
        chatMessage._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
        chatMessage._userName = new NetworkString() { st = name };
        chatMessage._message = new NetworkString() { st = message };
        return chatMessage;
    }

    private LogEntry CreateEntry(ulong id, string name, string uType, string aName, string message)
    {
        logEntry = new LogEntry(
            DateTime.UtcNow.ToString("MM-dd-yyyy"),
            DateTime.UtcNow.ToString("HH:mm:ss"),
            id,
            name,
            uType,
            Vector3.zero,
            Vector3.zero,
            "Chat",
            0,
            aName,
            message,
            ""
            );

        return logEntry;
    }

    #endregion
}

