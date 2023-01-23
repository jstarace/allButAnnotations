using UnityEngine;
using Cinemachine;
using Unity.Netcode;

public class CinemachineVirtualDynamic : MonoBehaviour
{
    private CinemachineVirtualCamera cinemachineVirtualCamera;
    public static CinemachineVirtualDynamic Instance { set; get; }

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

        this.cinemachineVirtualCamera= GetComponent<CinemachineVirtualCamera>();

    }

    public void FollowPlayer(Transform transform)
    {
        this.cinemachineVirtualCamera.Follow = transform;
        //this.cinemachineVirtualCamera.LookAt = transform;
    }
}
