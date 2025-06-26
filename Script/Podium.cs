
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;
using TMPro;
using VRC.Udon;

public class Podium : UdonSharpBehaviour
{
    [SerializeField] TeamManager teamManager;
    [SerializeField] GameObject podiumObject;
    [SerializeField] Transform awardSpawnPoint;
    [SerializeField] TextMeshProUGUI winnerName;
    
    [UdonSynced] int winTeam;
    [UdonSynced] bool isEnd;

    public void EndGame(int team)
    {
        if (!Networking.IsOwner(this.gameObject))
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

        winTeam = team;
        isEnd= true;
        RequestSerialization();
        SetAward();
        SetWinnerName();
        NetworkCalling.SendCustomNetworkEvent((IUdonEventReceiver)this, NetworkEventTarget.All, nameof(HandleGameEndTeleport),team);
    }

    [NetworkCallable]
    public void HandleGameEndTeleport(int winnerTeam)
    {

        VRCPlayerApi player = Networking.LocalPlayer;

        string teamTag = player.GetPlayerTag("Team");
        int teamId = (teamTag == "Red") ? 1 : (teamTag == "Blue") ? 2 : 0;

        if (teamId == winnerTeam && winnerTeam != 0) // 勝利チームで引き分けでない
        {
            Networking.LocalPlayer.TeleportTo(awardSpawnPoint.position, awardSpawnPoint.rotation);
        }
        else
        {
            Networking.LocalPlayer.Respawn(); // ワールドのリスポーン地点に戻す
        }
    }

    private void SetWinnerName()
    {
        string winTeamTag = winTeam == 1 ? "Red" :
                         winTeam == 2 ? "Blue" : null;
        if (string.IsNullOrEmpty(winTeamTag))
        {
            winnerName.text = "";
            return;
        }

        int count = VRCPlayerApi.GetPlayerCount();
        VRCPlayerApi[] players = new VRCPlayerApi[count];
        VRCPlayerApi.GetPlayers(players);

        string text = "";
        foreach (var player in players)
        {
            if (player.GetPlayerTag("Team") == winTeamTag)
            {
                text += player.displayName + " ";
            }
        }

        winnerName.text = text;
    }
    public void DestroyPodium()
    {
        if (!Networking.IsOwner(this.gameObject))
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

        isEnd = false;
        RequestSerialization();
        SetAward();
    }
    public void SetAward()
    {
        podiumObject.SetActive(isEnd);

        if(!isEnd) { return; }

        switch (winTeam)
        {
            case 0:
                podiumObject.GetComponent<MeshRenderer>().material.color = Color.white;
                break;
            case 1:
                podiumObject.GetComponent<MeshRenderer>().material.color = Color.red;
                break;
            case 2:
                podiumObject.GetComponent<MeshRenderer>().material.color = Color.blue;
                break;
        }
    }
    public override void OnDeserialization()
    {
        SetAward();
        SetWinnerName();
    }
}
