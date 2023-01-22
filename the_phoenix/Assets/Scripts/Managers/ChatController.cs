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
    private int code;


    private bool firstMessage;
    private string message;
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
/*            chatMessage._userId = new NetworkString() { st = OwnerClientId.ToString() };
            chatMessage._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
            chatMessage._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
            chatMessage._userName = new NetworkString() { st = "The Almighty Server" };
            chatMessage._message = new NetworkString() { st = "I was initialized" };*/

            var tempMessage = BuildServerMessage(OwnerClientId.ToString(), "Server User", "I was Initialized");
            chatMessages.Add(tempMessage);
        }
        if (!NetworkManager.Singleton.IsServer && IsClient)
        {
            LoadChatLogServerRpc();
        }
    }

    private void Update()
    {
        // If the server is restarted during the day, we want to preserve any previous logs
        // So on the server start, we run this check.  This will test out processing a request
        // Which will create a save any previous file and create a new one for current session
        if (NetworkManager.Singleton.IsServer && !firstMessage)
        {
            code = 0;
            FileManager.Instance.ProcessRequest(code, chatMessage);
            firstMessage = true;
        }
    }

    private void SendMessageToServer(string newMessage)
    {
         ReceiveChatMessageServerRpc(newMessage);
    }

    public void ClearChatMessages()
    {
        //chatMessages = new List<ChatMessage>();
        chatMessages.Clear();
        ClearChatLogsClientRpc();
        var tempMessage = BuildServerMessage(OwnerClientId.ToString(), "Server User", "Professor Reset the chat");
/*        chatMessage._userId = new NetworkString() { st = OwnerClientId.ToString() };
        chatMessage._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
        chatMessage._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
        chatMessage._userName = new NetworkString() { st = "The Almighty Server" };
        chatMessage._message = new NetworkString() { st = "Professor Reset the chat" };*/
        chatMessages.Add(tempMessage);
        AllLoadChatClientRpc(tempMessage);
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

    #region ServerRpcs
    [ServerRpc(RequireOwnership = false)]
    
    public void JoinedChatServerRpc(string name, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            //var tempUser = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
            var tempMessage = BuildServerMessage("0", "Server User", string.Format("Welcome {0} to the lecture.", name));
/*            chatMessage._userId = new NetworkString() { st = "0" };
            chatMessage._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
            chatMessage._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
            chatMessage._userName = new NetworkString() { st = "The Almighty Server" };
            chatMessage._message = new NetworkString() { st = string.Format("Everyone please welcome {0} to the lesson.", name) };*/
            chatMessages.Add(tempMessage);
            LoadMessageClientRpc(tempMessage);
            FileManager.Instance.ProcessRequest(0, tempMessage);
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
/*            chatMessage._userId = new NetworkString() { st = clientID.ToString() };
            chatMessage._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
            chatMessage._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
            chatMessage._userName = new NetworkString() { st = tempUser.name };
            chatMessage._message = new NetworkString() { st = newMessage };*/
            chatMessages.Add(tempMessage);
            LoadMessageClientRpc(tempMessage);
            FileManager.Instance.ProcessRequest(0, tempMessage);
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
        //Debug.Log("The owner is probably the server... so... 0?: " + OwnerClientId);
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


    #endregion

}

