using Photon.Pun;

public class NetworkManager
{
    public void Init()
    {
        PhotonNetwork.GameVersion = $"v{GameManager.Instance.Version}";
        PhotonNetwork.ConnectUsingSettings();
    }
}