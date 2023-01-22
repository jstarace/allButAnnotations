using UnityEngine;
using System;

[System.Serializable]

public class DocumentEntry
{
    [SerializeField] public string date;
    [SerializeField] public string time;
    [SerializeField] public string userId;
    [SerializeField] public string userName;
    [SerializeField] public string userType;
    [SerializeField] public int inputCode;
    [SerializeField] public string inputKey;
    [SerializeField] public Vector3 currentPos;
    [SerializeField] public Vector3 targetPos;
    [SerializeField] public bool successful;


    public DocumentEntry(
        string date,
        string time,
        string userId, 
        string userName,
        string userType,
        int inputCode,
        string inputKey,
        Vector3 currentPos,
        Vector3 targetPos,
        bool successful

        )
    {
        this.date = date;
        this.time = time;
        this.userId = userId;
        this.userName = userName;
        this.userType = userType;
        this.inputCode = inputCode;
        this.inputKey = inputKey;
        this.currentPos = currentPos;
        this.targetPos = targetPos;
        this.successful = successful;
    }
}
