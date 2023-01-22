using System;
using Unity.Netcode;

public struct NetworkString : INetworkSerializable, IEquatable<NetworkString>
{
    public string st;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out st);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(st);
        }
    }
    public bool Equals(NetworkString other)
    {
        if (String.Equals(other.st, st, StringComparison.CurrentCultureIgnoreCase))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}