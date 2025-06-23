
using UdonSharp;
using VRC.SDK3.Data;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class TeamManager : UdonSharpBehaviour
{
    [SerializeField] MenuBoardManager menuBoardManager;
    [SerializeField] GameObject[] objectsToHideInGame;

    [UdonSynced] bool isHideTeamButton = true;
    [UdonSynced] private string _teamJson;
    private DataList _teams = new DataList();
    public override void OnPreSerialization()
    {
        SetHideobject();
        var token = new DataToken(_teams);
        if (VRCJson.TrySerializeToJson(token, JsonExportType.Minify, out var json))
        {
            _teamJson = json.String;
        }
    }
    public override void OnDeserialization()
    {
        SetHideobject();
        if (VRCJson.TryDeserializeFromJson(_teamJson, out var token) && token.TokenType == TokenType.DataList)
        {
            _teams = token.DataList;
            menuBoardManager.SetTeamName(_teams);
        }
    }

    public void AssignTeam(int index, int teamId)
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        while (_teams.Count <= index)
        {
            _teams.Add(0);
        }

        _teams.SetValue(index, teamId);
        RequestSerialization();

        menuBoardManager.SetTeamName(_teams);
    }
    public void AssignRedTeam()
    {
        AssignTeam(Networking.LocalPlayer.playerId, 1);
    }
    public void AssignBlueTeam()
    {
        AssignTeam(Networking.LocalPlayer.playerId, 2);
    }
    public void LeaveTeam()
    {
        AssignTeam(Networking.LocalPlayer.playerId, 0);
    }
    public DataList GetTeams()
    {
        return _teams;
    }
    public void SetHideObjectBool(bool active)
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)){ Networking.SetOwner(Networking.LocalPlayer, gameObject); }
        isHideTeamButton = active;
        RequestSerialization();
    }
    private void SetHideobject()
    {
        foreach (GameObject obj in objectsToHideInGame)
        {
            if (obj != null) obj.SetActive(isHideTeamButton);
        }
    }
}
