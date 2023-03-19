// using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
// using System.Collections.Specialized;
using System.IO;
using Unity.Netcode;
// using UnityEditor.VersionControl;
using UnityEngine;
// using UnityEngine.UIElements;

public class FileManager : NetworkBehaviour
{
    public static FileManager Instance { set; get; }

    private string sysInfo;
    private string dataPath;
    private string currentDate;
    private List<string> _paths = new List<string>();
    private List<string> _fileNames = new List<string>();
    private List<string> _fileContainer = new List<string>();
    private List<string> _extensions = new List<string>();
    //List<ChatEntry> chatEntries = new List<ChatEntry>();
    List<LogEntry> chatEntries = new List<LogEntry>();
    List<DocumentEntry> documentEntries = new List<DocumentEntry>();
    List<AnnotationEntry> annotationEntries = new List<AnnotationEntry>();
    List<LogEntry> logEntries = new List<LogEntry>();

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        this.sysInfo = SystemInfo.operatingSystemFamily.ToString();
        this.dataPath = Application.dataPath;

        //Debug.Log(this.sysInfo);
        if (this.sysInfo == "Windows") 
        {
            //Debug.Log(dataPath);
        }

        currentDate = DateTime.UtcNow.ToString("MMddyyyy");
        //Debug.Log("Current UTC date: " + currentDate);
    }
    public override void OnNetworkSpawn()
    {
        //Debug.Log("FILE MANAGER ONLINE: This is spawn");
        enabled = (IsServer && Instance!= null);
        this._extensions.Add(".txt");
        this._extensions.Add(".cs");
        this._extensions.Add(".cpp");
        this._extensions.Add(".py");
        this._extensions.Add(".json");
        if (NetworkManager.Singleton.IsServer)
        {
            this._paths.Add(Application.streamingAssetsPath + "/Logs/ChatTranscript/");
            this._paths.Add(Application.streamingAssetsPath + "/Logs/ChatPayloads/");
            this._paths.Add(Application.streamingAssetsPath + "/Logs/DocumentPayload/");
            this._paths.Add(Application.streamingAssetsPath + "/Logs/AnnotationsPayload/");
            this._paths.Add(Application.streamingAssetsPath + "/Logs/LogPayload/");
            this._fileNames.Add("ChatTranscripts_" + currentDate);
            this._fileNames.Add("ChatPayload_" + currentDate);
            this._fileNames.Add("DocumentPayload_" + currentDate);
            this._fileNames.Add("AnnotationsPayload_" + currentDate);
            this._fileNames.Add("LogPayload_" + currentDate);

            for (int i = 0; i < this._paths.Count; i++)
            {
                // Debug.Log(i);
                if (!CheckFileStorage(i)) 
                {
                    Directory.CreateDirectory(this._paths[i]);
                }
                //Debug.Log(this._fileNames[i]);
            }
        }
        if (IsClient)
        {
            //only create some paths if client
            //Gotta use different paths based on client
            if (this.sysInfo == "Windows")
            {
                this._paths.Add(Application.streamingAssetsPath + "/Saved/Documents/Text/");
                this._paths.Add(Application.streamingAssetsPath + "/Saved/Documents/cs/");
                this._paths.Add(Application.streamingAssetsPath + "/Saved/Documents/cpp/");
                this._paths.Add(Application.streamingAssetsPath + "/Saved/Documents/py/");

            }
            else
            {
                this._paths.Add("~/Downloads/Saved/Documents/Text/");
                this._paths.Add("~/Downloads/Saved/Documents/cs/");
                this._paths.Add("~/Downloads/Saved/Documents/cpp/");
                this._paths.Add("~/Downloads/Saved/Documents/py/");
            }


            for (int i = 0; i < this._paths.Count; i++)
            {
                if (!CheckFileStorage(i)) Directory.CreateDirectory(this._paths[i]);
            }
        }
    }

    #region Checkers
    private bool CheckFileStorage(int x) 
    {
        if (Directory.Exists(_paths[x])) return true;
        return false;
    }

    #endregion

    #region General Methods



    public void ProcessRequests(LogEntry log)
    {
        #region Date check file name update
        /*
         * In the event the date changed, while things were running, we need to create a new file for the new day
        */
        string newDate = DateTime.UtcNow.ToString("MMddyyyy");
        if (newDate != currentDate)
        {
            currentDate = newDate;
            _fileNames[0] = "ChatTranscripts_" + currentDate;
            _fileNames[1] = "ChatPayload_" + currentDate;
            _fileNames[2] = "DocumentPayload_" + currentDate;
            _fileNames[3] = "AnnotationsPayload_" + currentDate;
            _fileNames[4] = "LogPayload_" + currentDate;
        }
        #endregion

        if(log.actionCode == 0)
        {
            string transcript = _paths[log.actionCode] + _fileNames[log.actionCode] + _extensions[log.actionCode];
            if (!File.Exists(transcript)) File.Create(transcript).Close();
            File.AppendAllText(transcript, BuildTranscriptString(log));
            chatEntries.Add(log);
            FileHandler.SaveToJSON(chatEntries, _paths[1] + _fileNames[1] + _extensions[4]);
            logEntries.Add(log);
            FileHandler.SaveToJSON(logEntries, _paths[4] + _fileNames[4] + _extensions[4]);
        }
    }

    // This only handles LOGS on the server side 
    public void ProcessRequest(int code, ChatMessage message = new ChatMessage(), DocumentEntry entry = null, LogEntry log = null)
    {

        /* Codes received should be 0, 1, 2
        *   0 = New chat received -> Update transcript and Chat payload
        *   1 = Edit to document -> Update Document Payload
        */
        if (NetworkManager.Singleton.IsServer)
        {
            // Checking to see if the day has changed for the server.  Log files are meant to have the day in the title
            // to reference when the log was generated.  So we check the array of names whenever we request a process
            // If the date has changed on the server, we update the array and proceed.
            string newDate = DateTime.UtcNow.ToString("MMddyyyy");
            if (newDate != currentDate)
            {
                currentDate = newDate;
                _fileNames[0] = "ChatTranscripts_" + currentDate;
                _fileNames[1] = "ChatPayload_" + currentDate;
                _fileNames[2] = "DocumentPayload_" + currentDate;
                _fileNames[3] = "AnnotationsPayload_" + currentDate;
                _fileNames[4] = "LogPayload_" + currentDate;
            }

            //We need to build the name based on the code.... maybe, this is getting tough
            if(log.actionCode == 0)
            {
                string transcript = _paths[log.actionCode] + _fileNames[log.actionCode];
                if (!File.Exists(transcript)) File.Create(transcript).Close();
                File.AppendAllText(transcript, BuildChatTranscriptString(message));
                DataCollection(code, new ChatMessage(), null, log);
            }
            if (code== 0)
            {
                // First we check if the applicable files exist.  If not we create them
                // string transcript = _paths[code] + _fileNames[code] + _fileNames[code];
                // if (!File.Exists(transcript)) File.Create(transcript).Close();
                // File.AppendAllText(transcript, BuildChatTranscriptString(message));
                // DataCollection(code, message);
            }
            if(code== 1)
            {
                DataCollection(code, new ChatMessage(), entry);
            }

            if(code == 3)
            {
                Debug.Log("ok...");
                DataCollection(code, new ChatMessage(), null, log);
            }
        }
    }

    private string BuildTranscriptString(LogEntry log)
    {
        var transcriptEntry = string.Format("[{0} {1}] {2}({3}) said: {4}\n", log.date, log.time, log.userName, log.userId, log.actionContent);
        return transcriptEntry;
    }

    private string BuildChatTranscriptString(ChatMessage message)
    {
        string processedMessage = "";

        processedMessage = string.Format("[{0} {1}] {2}({3}) said: {4}\n", message._date.st, message._time.st, message._userName.st, message._userId.st, message._message.st);

        return processedMessage;
    }


    // The method here is attempting to be generic.  It can handle any input type, document or chat, and send it to a JSON file
    // Currently we're sending a list of these objects every time we write to JSON.  The idea is to change this so we're only sending 
    // The new item and it's being appended.  
    private void DataCollection(int code, ChatMessage message = new ChatMessage(), DocumentEntry entry = null, LogEntry log = null)
    {
        if(code == 0)
        {
            //chatEntries.Add(new ChatEntry(message._userId.st, message._userName.st, message._date.st, message._time.st, message._message.st));
            FileHandler.SaveToJSON(chatEntries, _paths[code + 1] + _fileNames[code + 1] + _extensions[4]);
        }
        else if(code == 1)
        {
            documentEntries.Add(entry);
            FileHandler.SaveToJSON(documentEntries, _paths[code + 1] + _fileNames[code + 1] + _extensions[4]);
        }
        else if(code == 2)
        {
            Debug.Log("this will be where we send the shit... need to build it though");
        }
        else if(code == 3)
        {
            logEntries.Add(log);
            Debug.Log("Trying here: " + _paths[4] + _fileNames[4] + _extensions[4]);
            FileHandler.SaveToJSON(logEntries, _paths[4] + _fileNames[4] + _extensions[4]);
        }
        

    }

    #endregion

    #region ServerRpcs

    [ServerRpc(RequireOwnership = false)]
    public void SaveLogsServerRpc()
    {
        // Check if the json files exist... if they do, rename them 
        // Debug.Log("We here?");
        // check codes 1 & 2

        ChatMessage message = new ChatMessage();
        message._userId = new NetworkString() { st = "0" };
        message._date = new NetworkString() { st = System.DateTime.UtcNow.ToString("MM-dd-yyyy") };
        message._time = new NetworkString() { st = System.DateTime.UtcNow.ToString("HH:mm:ss") };
        message._userName = new NetworkString() { st = "Server User" };
        message._message = new NetworkString() { st = "Saving Logs" };
        ProcessRequest(0, message);

        DocumentEntry documentEntry = new DocumentEntry(
            DateTime.UtcNow.ToString("MM-dd-yyyy"),
            DateTime.UtcNow.ToString("HH:mm:ss"),
            "0",
            "Server User",
            "Admin",
            777,
            "Request to isolate log processed",
            new Vector3(999,999,999),
            new Vector3(999,999,999),
            true);
        ProcessRequest(1, new ChatMessage(), documentEntry);



        for (int i = 1; i < 3; i++)
        {
            string originalName = _paths[i] + _fileNames[i] + _extensions[4];
            string tempFileName = _paths[i] + _fileNames[i] + _extensions[4];
            int tempNum = 0;
            while (File.Exists(tempFileName))
            {
                tempNum++;
                tempFileName = _paths[i] + _fileNames[i] + "_" + tempNum + _extensions[4];
            }

            File.Move(originalName, tempFileName);
            File.Create(originalName).Close();
        }
    }

    #endregion

    #region ClientRpcs

    [ClientRpc]
    public void EmptyContainerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        Debug.Log("You've been requested to empty the container");
        this._fileContainer = new List<string>();
    }

    [ClientRpc]
    public void BuildFileClientRpc(NetworkString temp, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;
        this._fileContainer.Add(temp.st);
    }

    [ClientRpc]
    public void SaveFileClientRpc(string fileName, int index, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        // NOTE: THE CONTAINER IS EMPTIED RIGHT BEFORE THIS METHOD IS CALLED.  THIS WAY, THE CLIENT SHOULD 
        //       ONLY SAVE WHAT IS VISIBLE ON THE SCREEN AND NOTHING MORE.

        // We don't want to overwrite files on clients so, we check if the file exists.  so, first step, build out the file name
        
        string entirePath = this._paths[index] + fileName + this._extensions[index];

        
        
        // The.... check if it exists
        if (File.Exists(entirePath))
        {
            // If it does, we copy it to a different location.  Now with extension broke out, we can change this.
            // We use seconds in the name to prevent overwriting a previous move
            File.Move(entirePath, this._paths[index] + fileName + DateTime.UtcNow.ToString("HH_mm_ss") + this._extensions[index]);

            // Then create a new file
            File.Create(entirePath).Close();
        }
        else
        {
            // If one does not exist, create a new file
            File.Create(entirePath).Close();
        }
        for (int i = 0; i < _fileContainer.Count; i++)
        {
            // Here we take the current file and dump it into a new file
            File.AppendAllText(entirePath, _fileContainer[i]);
        }

        /* Now we check that the file exists
        *  NOTE: WE DO NOT CHECK THAT THE FILE HAS ANY CONTENT OR VALIDATE ANY CONTENT IN A FILE
        *  WE SIMPLY VERIFY THE FILE EXISTS!!!!
        */
        if (File.Exists(entirePath))
        {
            // If the file exists, display the path to the file to the user
            DocumentManager.Instance.SetConfirmationText(string.Format("A file was created here {0}\n", entirePath));
        }
        else
        {
            // If it doesn't tell them
            DocumentManager.Instance.SetConfirmationText("Better Luck Next Time");
        }
        
    }

    #endregion


    public void LoadADocument(string theName)
    {
        ServerLoadFile.Load(theName);
    }











}
