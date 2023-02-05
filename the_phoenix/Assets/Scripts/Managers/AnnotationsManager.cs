using Starace.Utils;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnnotationsManager : NetworkBehaviour
{
    public static AnnotationsManager Instance { set; get; }

    public GameObject PrefabToSpawn;
    private GameObject m_PrefabInstance;
    private NetworkObject m_SpawnedNetworkObject;
    private MeshFilter m_SpawnedMeshFilter;

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;
        if (enabled)
        {
            if(Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }
        if(!enabled || Instance == null) return;
        Debug.Log("kk... only loading when network starts");
    }

    private void Update()
    {

    }

    public void CreateHighlight(Vector3 location)
    {
        Debug.Log("Requested creation here: " + location);

        // We're going to make sure we can get a position that always lines up with a list.
        // It's been pretty simple so far, because it was all locked down... now that it's a free world
        // We have to make sure that the value always equals what may be a position in the list.

        Mesh mesh = new Mesh();
        Utilities.GetMesh(location, out mesh);
        m_PrefabInstance = Instantiate(PrefabToSpawn);
        m_SpawnedMeshFilter =  m_PrefabInstance.GetComponent<MeshFilter>();
        m_SpawnedMeshFilter.mesh = mesh;
        m_SpawnedNetworkObject = m_PrefabInstance.GetComponent<NetworkObject>();
        m_SpawnedNetworkObject.Spawn();
        AddMeshClientRpc(m_PrefabInstance, location);
    }

    [ClientRpc]

    private void AddMeshClientRpc(NetworkObjectReference meshFilter, Vector3 location)
    {
        var tempObject = ((GameObject)meshFilter);
        var tempFilter = tempObject.GetComponent<MeshFilter>();
        
        Mesh mesh = new Mesh();
        Utilities.GetMesh(location, out mesh);
        tempFilter.mesh = mesh;

        Debug.Log(location);

    }
}
