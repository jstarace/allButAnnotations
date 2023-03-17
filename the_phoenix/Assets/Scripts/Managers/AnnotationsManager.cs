using Starace.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class AnnotationsManager : NetworkBehaviour
{
    public static AnnotationsManager Instance { set; get; }
    private Dictionary<ulong, ulong> userSelections;
    private Dictionary<ulong, List<ulong>> testSelections;

    [SerializeField] public TMP_InputField annotationInput;

    [SerializeField] private Button createAnnotation;
    [SerializeField] private Button cancelAnnoMenu;
    [SerializeField] private Button submitAnnotation;
    [SerializeField] private Button quitAnnotation;

    public GameObject annotationPanel = null;

    public GameObject rightClickMenu = null;
    public GameObject annotationWindow = null;


    public GameObject PrefabToSpawn;
    private GameObject m_PrefabInstance;
    private NetworkObject m_SpawnedNetworkObject;
    private SpriteRenderer backgroundSpriteRenderer;
    private TextMeshPro annotationText;
    private TextMeshPro selectedText;


    private void Awake()
    {
        ResetWindow();
        Instance = this;

        cancelAnnoMenu.onClick.AddListener(() =>
        {
            ResetWindow();
        });
        quitAnnotation.onClick.AddListener(() =>
        {
            ResetWindow();
        });
        createAnnotation.onClick.AddListener(() =>
        {
            rightClickMenu.SetActive(false);
            annotationWindow.SetActive(true);
            annotationInput.enabled = true;
        });
        submitAnnotation.onClick.AddListener(() =>
        {
            // Debug.Log("Clicked submit");
            var theAnnotation = annotationInput.text;
            CreateAnnotationPackage(theAnnotation);            
            ResetWindow();
        });
    }

    private bool GetSelection()
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        var player = playerObject.GetComponent<PlayerNetwork>();
        return player.IsSelected();
    }

    private void CreateAnnotationPackage(string theAnnotation)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        var player = playerObject.GetComponent<PlayerNetwork>();

        player.ClearSelection();
        ReceiveAnnotationServerRpc(theAnnotation);

    }

    [ServerRpc(RequireOwnership = false)]

    private void ReceiveAnnotationServerRpc(string theAnnotation, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        if(NetworkManager.ConnectedClients.ContainsKey(clientID))
        {
            //Debug.Log(theAnnotation);
            var newAnno = DocumentManager.Instance.GetSelection(clientID);
            string selection = string.Empty;
            foreach (var anno in newAnno)
            {
                selection += DocumentManager.Instance.GetCharacterById(anno);
            }

            var player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID);
            m_PrefabInstance = Instantiate(PrefabToSpawn);

            m_PrefabInstance.transform.position = new Vector3(-125, player.transform.position.y);


            m_SpawnedNetworkObject = m_PrefabInstance.GetComponent<NetworkObject>();
            backgroundSpriteRenderer = m_SpawnedNetworkObject.transform.Find("Background").GetComponent<SpriteRenderer>();
            selectedText = m_SpawnedNetworkObject.transform.Find("SelectedText").GetComponent<TextMeshPro>();
            annotationText = m_SpawnedNetworkObject.transform.Find("AnnoText").GetComponent<TextMeshPro>();
            selectedText.text = selection;
            annotationText.text = theAnnotation;
            m_SpawnedNetworkObject.Spawn();

            

            UpdateThatPieceClientRpc(m_SpawnedNetworkObject, selection, theAnnotation);
            
        }
                
    }

    [ClientRpc]

    private void UpdateThatPieceClientRpc(NetworkObjectReference newAnno, string selection, string theAnnotation)
    {
        var temp = ((GameObject)newAnno);
        var newSelectedText = temp.transform.Find("SelectedText").GetComponent<TextMeshPro>();
        var newAnnotationText = temp.transform.Find("AnnoText").GetComponent<TextMeshPro>();
        newSelectedText.text = selection;
        newAnnotationText.text = theAnnotation;

    }

    public override void OnNetworkSpawn()
    {

        userSelections = new Dictionary<ulong, ulong>();
        testSelections = new Dictionary<ulong, List<ulong>>();
    }

    public void Update()
    {
        if(NetworkManager.Singleton.IsConnectedClient)
        {
            if (Input.GetMouseButtonDown(1))
            {
                annotationPanel.SetActive(true);
                createAnnotation.interactable = GetSelection();
            }
        }       
    }

    public void ToggleHighlight(ulong netID, ulong clientID)
    {
        Debug.Log(netID);

        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
        var tempChild = tempObject.transform.GetChild(0);
        bool state = tempChild.gameObject.activeSelf;
        tempChild.gameObject.SetActive(!state);
        ToggleHighlightClientRpc(netID, !state);
    }

    [ClientRpc]
    private void ToggleHighlightClientRpc(ulong netID, bool state)
    {
        var tempObject = NetworkManager.SpawnManager.SpawnedObjects[netID].gameObject;
        var tempChild = tempObject.transform.GetChild(0);
        //bool state = tempChild.gameObject.activeSelf;
        tempChild.gameObject.SetActive(state);

    }

    private void ResetWindow()
    {
        annotationInput.enabled = false;
        annotationInput.text = string.Empty;
        annotationWindow.SetActive(false);
        rightClickMenu.SetActive(true);
        annotationPanel.SetActive(false);
    }
}
