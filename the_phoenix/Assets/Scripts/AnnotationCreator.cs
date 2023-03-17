using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class AnnotationCreator : NetworkBehaviour
{




    private GameObject m_PrefabInstance;
    private NetworkObject m_SpawnedNetworkObject;
    private SpriteRenderer backgroundSpriteRenderer;
    private TextMeshPro annotationText;
    private TextMeshPro selectedText;



    public static AnnotationCreator Instance { set; get; }
    public ulong Create(string text, GameObject annoContainer)
    {
        ulong theID = new ulong();

        Debug.Log("we close");

        theID = Setup(text, annoContainer);

        return theID;
    }

    private void Awake()
    {
        backgroundSpriteRenderer = transform.Find("Background").GetComponent<SpriteRenderer>();
        annotationText = transform.Find("AnnoText").GetComponent<TextMeshPro>();
        selectedText = transform.Find("SelectedText").GetComponent<TextMeshPro>();

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        //Setup("Well this will be fun");
    }

    private ulong Setup(string text, GameObject annoContainer)
    {
        ulong theID = new ulong();

        m_PrefabInstance = Instantiate(annoContainer);
        m_PrefabInstance.transform.position = default(Vector3);
        //annotationText.text = text;
        //annotationText.ForceMeshUpdate();
        //selectedText.ForceMeshUpdate();
        //Vector2 padding = new Vector2(2f, 2f);
        //Vector2 annoSize = annotationText.GetRenderedValues(false);
        //Vector2 selectedSize = selectedText.GetRenderedValues(false);

        return theID;
        //backgroundSpriteRenderer.size = annoSize + padding;
    }

}
