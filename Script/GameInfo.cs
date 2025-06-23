
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using TMPro;
using VRC.Udon;

public class GameInfo : UdonSharpBehaviour
{
    [SerializeField] TextMeshProUGUI endText;
    [SerializeField] TextMeshProUGUI redScoreText;
    [SerializeField] TextMeshProUGUI blueScoreText;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] TextMeshProUGUI redBall;
    [SerializeField] TextMeshProUGUI blueBall;
    [SerializeField] GameObject positionObject;
    [SerializeField] SettingManager seting;

    private string[] messages_ja =
    {
        "赤チームはジャックボールを投げてください",
        "青チームはジャックボールを投げてください",
        "赤チームはボールを投げてください",
        "青チームはボールを投げてください",
        "ジャックボールを再度投げてください",
        "集計中...",
        "赤チーム　得点:",
        "青チーム　得点:",
        "ゲーム終了　引き分け",
        "ゲーム終了　赤チームの勝利",
        "ゲーム終了　青チームの勝利"
    };
    private string[] messages_en =
    {
    "Red Team, please throw the jack ball.",
    "Blue Team, please throw the jack ball.",
    "Red Team, please throw your ball.",
    "Blue Team, please throw your ball.",
    "Please re-throw the jack ball.",
    "Calculating scores...",
    "Red Team Score:",
    "Blue Team Score:",
    "Game Over - Draw",
    "Game Over - Red Team Wins",
    "Game Over - Blue Team Wins"
    };
    private string[] messages_ko =
    {
    "레드팀, 잭볼을 던져주세요.",
    "블루팀, 잭볼을 던져주세요.",
    "레드팀, 공을 던져주세요.",
    "블루팀, 공을 던져주세요.",
    "잭볼을 다시 던져주세요.",
    "점수 계산 중...",
    "레드팀 점수:",
    "블루팀 점수:",
    "게임 종료 - 무승부",
    "게임 종료 - 레드팀 승리",
    "게임 종료 - 블루팀 승리"
    };



    private void Update()
    {
        transform.SetPositionAndRotation(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);
        positionObject.transform.localPosition = seting.GetDisplayPosition();

    }
    public void SetGameInfoMessage(int key, int score = 0)
    {
        string scoreText = (score == 0)? "" : score.ToString();
        switch (seting.GetCurrentLanguage())
        {
            case 0:
                infoText.text = messages_ja[key] + scoreText;
                break;
            case 1:
                infoText.text = messages_en[key] + scoreText;
                break;
            case 2:
                infoText.text = messages_ko[key] + scoreText;
                break;
        }
    }
    public void SetScore(int red,int blue)
    {
        redScoreText.text = red.ToString();
        blueScoreText.text = blue.ToString();
    }
    public void SetBallCount(int red,int blue)
    {
        string textR = "";
        string textB = "";
        for(int i = 0; i < red ; i++)
        {
            textR = textR + "●";
        }
        for (int i = 0; i < blue; i++)
        {
            textB = textB + "●";
        }
        redBall.text = textR;
        blueBall.text = textB;
    }
    public void SetEnd(int current,int max)
    {
        endText.text = current.ToString() +" / " + max.ToString();
    }
}
