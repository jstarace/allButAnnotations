using UnityEngine;
using System;

[System.Serializable]

public class ChatEntry
{
    [SerializeField] public string userId;
    [SerializeField] public string userName;
    [SerializeField] public string date;
    [SerializeField] public string time;
    [SerializeField] public string message;

    public ChatEntry(string userId, string userName, string date, string time, string message)
    {
        this.userId = userId;
        this.userName = userName;
        this.date = date;
        this.time = time;
        this.message = message;
    }
}