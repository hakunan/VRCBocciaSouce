
using UdonSharp;
using VRC.SDK3.Data;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class MenuBoardManager : UdonSharpBehaviour
{
    [SerializeField] TextMeshProUGUI redTeamNameText;
    [SerializeField] TextMeshProUGUI blueTeamNameText;
    [SerializeField] GameObject[] hiddenUIObjectsDuringGame;
    [UdonSynced, FieldChangeCallback(nameof(IsHide))] private bool _isHide;
    public bool IsHide
    {
        private set { _isHide = value; SetVisibilityByState(value); }
        get { return _isHide; }
    }
    private void SetVisibilityByState(bool active)
    {
        foreach(var obj in hiddenUIObjectsDuringGame)
        {
            obj.SetActive(!active);
        }
    }
    public void SetIsHideValue(bool value)
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer,gameObject);
        IsHide= value;
        RequestSerialization();
    }
    public void SetTeamName(DataList teams)
    {
        string red = "";
        string blue = "";

        for (int i = 0; i < teams.Count; i++)
        {
            int team = (int)teams[i].Double;
            if (team == 0) continue;

            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(i);
            if (player == null || string.IsNullOrEmpty(player.displayName)) continue;

            switch (team)
            {
                case 1:
                    red += player.displayName + "\n";
                    break;
                case 2:
                    blue += player.displayName + "\n";
                    break;
            }
        }

        if (redTeamNameText != null) redTeamNameText.text = red;
        if (blueTeamNameText != null) blueTeamNameText.text = blue;
    }

}
