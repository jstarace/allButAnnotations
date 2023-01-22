using UnityEngine;
using System;
using Unity.Netcode;

public struct ChatMessage: INetworkSerializable
{
    public NetworkString _userId;
    public NetworkString _date;
    public NetworkString _time;
    public NetworkString _userName;
    public NetworkString _message;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _userId);
        serializer.SerializeValue(ref _date);
        serializer.SerializeValue(ref _time);
        serializer.SerializeValue(ref _userName);
        serializer.SerializeValue(ref _message);
    }
}