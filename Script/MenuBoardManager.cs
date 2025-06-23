
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
