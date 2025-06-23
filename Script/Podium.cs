
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
        var teams = teamManager.GetTeams();

        // 安全チェック
        if (Networking.LocalPlayer == null || Networking.LocalPlayer.playerId >= teams.Count)
        {
            return;
        }

        int myTeam = (int)teams[Networking.LocalPlayer.playerId].Double;

        if (myTeam == winnerTeam && winnerTeam != 0) // 勝利チームで引き分けでない
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
        var teams = teamManager.GetTeams();
        string text = "";

        for (int i = 0; i < teams.Count; i++)
        {
            int t = (int)teams[i].Double;
            if (t == winTeam)
            {
                var player = VRCPlayerApi.GetPlayerById(i);
                if (player != null)
                {
                    text += player.displayName + " ";
                }
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
