using System;
using Unity.Netcode;

public class NetworkStore : NetworkBehaviour
{
    public NetworkVariable<int> player_x = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> player_y = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<NetworkString> username = new NetworkVariable<NetworkString>(new NetworkString()
    {
        st = ""
    });

}



