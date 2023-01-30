using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
//using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : NetworkBehaviour
{


    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private TextMeshProUGUI greeting;
    [SerializeField] private TextMeshProUGUI selectedFile;

    [SerializeField] private int cellSize;
    [SerializeField] public static int cell;


    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;

    [SerializeField] private Button submitInfoButton;

    [SerializeField] private Button chatButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveLogsButton;
    [SerializeField] private Button loadPreviousSession;
    [SerializeField] private Button clearWindowButton;
    [SerializeField] private Button logoutButton;


    [SerializeField] private Button cancelButton;

    [SerializeField] private Button confirmLoadSession;
    [SerializeField] private Button cancelLoadSession;

    public GameObject loginPanel = null;
    public GameObject userInfoPanel = null;
    public GameObject HUDPanel = null;
    public GameObject chatPanel = null;
    public GameObject savePanel = null;

    public GameObject loadFilePanel = null;


    public GameObject filePrefab;

    private GameObject[] loadedFiles;

    private string displayedMessage;
    private string uName;
    public static NetworkUI Instance { set; get; }

    private bool chatStatus = false;
    private bool saveStatus = false;
    private bool uploadStatus = false;
    private bool hudStatus = true;
    public bool displayMessage = false;

     string[] args = System.Environment.GetCommandLineArgs();

    private void Awake()
    {
        Instance = this;
        userInfoPanel.SetActive(true);
        chatPanel.SetActive(false);
        HUDPanel.SetActive(false);
        loginPanel.SetActive(false);
        loadFilePanel.SetActive(false);

        cell = cellSize;

        #region Development Buttons
        serverButton.onClick.AddListener(() =>
        {
            if (loginPanel != null)
            {
                NetworkManager.Singleton.StartServer();
                loginPanel.SetActive(false);
            }
        });
        clientButton.onClick.AddListener(() =>
        {
            if (loginPanel != null)
            {
                NetworkManager.Singleton.StartClient();
                loginPanel.SetActive(false);
                HUDPanel.SetActive(true);
            }
        });
        #endregion

        #region The one to login
        submitInfoButton.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(userNameInput.text)) userNameInput.text = "Zardoz";
            greeting.text = "Hello " + userNameInput.text + "!!!";
            uName= userNameInput.text;
            userNameInput.text = null;
            loginPanel.SetActive(true);
            userInfoPanel.SetActive(false);
        });
        #endregion

        #region HUD Buttons
        chatButton.onClick.AddListener(() =>
        {
            if (loginPanel != null)
            {
                chatStatus = !chatStatus;
                chatPanel.SetActive(chatStatus);
            }
        });
        saveButton.onClick.AddListener(() =>
        {
            
            if(loginPanel != null)
            {
                saveStatus = !saveStatus;
                savePanel.SetActive(saveStatus) ;
            }
        });
        saveLogsButton.onClick.AddListener(() =>
        {
            FileManager.Instance.SaveLogsServerRpc();
        });
        loadPreviousSession.onClick.AddListener(() =>
        {
            GetFileNameServerRpc();
        });
        clearWindowButton.onClick.AddListener(() =>
        {
            /* Make note in both logs that the UI has been cleared 
               - Note who cleared the things
               - Then we clear the things
               - Gotta hit the chat controller to clear out it's items
               - Gotta hit the document controller to clear out it's items
               - This of course should just be 3 methods called from here.
             */
            ClearScreenServerRpc();
        });
        logoutButton.onClick.AddListener(() =>
        {

        });
        #endregion

        #region Return To HUD
        cancelButton.onClick.AddListener(() =>
        {
            if (loginPanel != null)
            {
                saveStatus = !saveStatus;
                savePanel.SetActive(saveStatus);
            }
        });
        #endregion


        cancelLoadSession.onClick.AddListener(() =>
        {
            ToggleUploadPanel();
            CleanUpFiles();
        });

        confirmLoadSession.onClick.AddListener(() =>
        {
            RequestFileLoad();
        });


        // serverButton.enabled = false;
    }

    private void Start()
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-launch-as-server")
            {
                //Debug.Log("Server making it here");
                NetworkManager.Singleton.StartServer();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("You are the almighty server");
        }
        else if (IsClient)
        {
            Debug.Log("You're logged in as a client");
        }
        else if (IsHost)
        {
            Debug.Log("You're logged in as a host");
        }
    }

    public string GetUsername()
    {
        return uName;
    }

    public float GetCell()
    {
        return cellSize;
    }

    private void LoadFileName(string theName)
    {
        this.selectedFile.text = theName;
    }

    private void CleanUpFiles()
    {
        foreach(var item in loadedFiles) 
        {
            Destroy(item.gameObject);
        }
    }

    private void ToggleUploadPanel()
    {
        uploadStatus = !uploadStatus;
        hudStatus = !hudStatus;
        loadFilePanel.SetActive(uploadStatus);
        HUDPanel.SetActive(hudStatus);
    }

    private void RequestFileLoad()
    {
        NetworkString fileName = new NetworkString() { st = selectedFile.text };
        ClearScreenServerRpc();
        LoadFileServerRpc(fileName);
        CleanUpFiles();
        ToggleUploadPanel();
    }

    #region ServerRpcs
    [ServerRpc(RequireOwnership = false)]
    public void ClearScreenServerRpc(bool document = true, bool chat = true, ServerRpcParams serverRpcParams = default)
    {
        //I'll just call the one from here, this can kick everything else off
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            // Here we'd do a look up to make sure the user has auth to request
            // A reset even though the button should only be available to those
            // that do
            // After user is verified, save the current logs
            FileManager.Instance.SaveLogsServerRpc();

            // Clear the document
            if (document)
            {
                DocumentManager.Instance.ClearDocument();
            }
            // Clear the chat
            if (chat)
            {
                ChatController.Instance.ClearChatMessages();
            }
            // We will need to eventually clear annotations too.

            // Don't forget to return characters to origin
            foreach (var id in NetworkManager.ConnectedClients.Keys)
            {
                var playerOb = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(id);
                playerOb.transform.position = new Vector3(-100, 50, 0);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetFileNameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientID },
                }
            };

            DisplayFileOptionsClientRpc(clientRpcParams);
            string fileName = Application.streamingAssetsPath + "/Logs/DocumentPayload";
            DirectoryInfo directoryInfo = new DirectoryInfo(fileName);
            FileInfo[] savedFiles = directoryInfo.GetFiles("*.json");
            NetworkString[] fileNames = new NetworkString[savedFiles.Length];
            int i = 0;
            foreach (FileInfo file in savedFiles)
            {

                string tempName = file.Name;
                string extension = Path.GetExtension(file.Name);
                string rootName = tempName.Substring(0, tempName.Length - extension.Length);
                fileNames[i] = new NetworkString { st = rootName };
                i++;
            }
            DisplayAvailableFilesClientRpc(fileNames, clientRpcParams);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadFileServerRpc(NetworkString filename, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            string path = Application.streamingAssetsPath + "/Logs/DocumentPayload/" + filename.st + ".json";
            if (!File.Exists(path))
            {
                Debug.Log("File not found... exiting");
                return;
            }
            else
            {
                FileManager.Instance.LoadADocument(path);
            }
        }

    }

    #endregion

    #region ClientRpcs
    [ClientRpc]
    private void DisplayFileOptionsClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        ToggleUploadPanel();
    }

    [ClientRpc]
    private void DisplayAvailableFilesClientRpc(NetworkString[] fileNames, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        var parentComponent = GameObject.Find("AvailableFiles");
        loadedFiles = new GameObject[fileNames.Length];
        int i = 0;
        foreach (NetworkString name in fileNames)
        {
            GameObject fileEntity = Instantiate(filePrefab) as GameObject;
            fileEntity.transform.SetParent(parentComponent.transform);
            var textComponents = fileEntity.transform.Find("TextComponents");
            var theName = textComponents.transform.Find("FileName");
            var theText = theName.GetComponent<Text>();
            theText.text = name.st;
            loadedFiles[i] = fileEntity;
            i++;

            var btn = fileEntity.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                LoadFileName(name.st);
            });
        }
    }

    [ClientRpc]
    public void DisplayErrorMessageClientRpc(string errorMessage, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        //DocumentManager.Instance.DrawDocumentBorders();
        Debug.Log(errorMessage);
        displayedMessage= errorMessage;
        Debug.Log("but we do change the bool");
        ToggleDisplayMessage();
        Debug.Log("We can tell cause, that button is showing");


        StartCoroutine(Countdown(3));
        //Countdown(3);

        Debug.Log("but does this trigger?");
    }

    #endregion

    private void OnGUI()
    {
        if (displayMessage)
        {
            int w, h;
            w = Screen.width / 2;
            h = Screen.height / 2;
            Debug.Log("The width is: " + w);
            Debug.Log("The height is: " + h);

            GUI.Box(new Rect(w - 300, h - 50, 600, 100), "ALERT: " + displayedMessage);

            
        }
    }

    IEnumerator Countdown (int seconds)
    {
        Debug.Log("We fucking here?: " + seconds);
        int counter = seconds;
        while(counter > 0)
        {
            yield return new WaitForSeconds(1);
            Debug.Log(counter);
            counter--;
        }

        ToggleDisplayMessage();
    }

    private void ToggleDisplayMessage()
    {
        displayMessage= !displayMessage;
    }
}
