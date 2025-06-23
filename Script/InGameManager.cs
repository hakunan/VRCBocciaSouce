
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.InputSystem;

public class InGameManager : UdonSharpBehaviour
{
    [SerializeField] TeamManager teamManager;
    [SerializeField] SettingManager setings;
    [SerializeField] GameInfo gameInfo;
    [SerializeField] Podium podium;
    [SerializeField] ParticleSystem ballParticle;
    [SerializeField] GameSoundManager soundManager;

    [Header("チームごとの出現位置")]
    [SerializeField] Transform redTransform;
    [SerializeField] Transform blueTransform;

    [SerializeField] Transform jackBallRespawnTransform;

    [Header("ボール用オブジェクトブール")]
    [SerializeField] VRCObjectPool jackBallObjectPool;
    [SerializeField] VRCObjectPool redBallObjectPool;
    [SerializeField] VRCObjectPool blueBallObjectPool;

    //　同期変数
    [UdonSynced] int currentEnd;
    [UdonSynced] int maxEnd = 2;

    [UdonSynced] int redScore;
    [UdonSynced] int blueScore;

    [UdonSynced] int redCurrentBall;
    [UdonSynced] int blueCurrentBall;
    const int defoultBallCount = 6;

    [UdonSynced] bool firstBall;//Trueなら赤 falseなら青
    public void GameStart()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) { Networking.SetOwner(Networking.LocalPlayer, gameObject); }

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeleportLocal));
        SendConvertedPlaySe(SeKey.Start);

        teamManager.SetHideObjectBool(false);
        AllBallReturn();

        currentEnd++; //ラウンドを進める
        redCurrentBall = defoultBallCount;
        blueCurrentBall = defoultBallCount;

        if (currentEnd == 1)//　最初ならランダムでスタートを決めて初期化
        {
            firstBall = UnityEngine.Random.value < 0.5f;
            redScore = 0;
            blueScore = 0;
            maxEnd = setings.GetMaxEnd();
            podium.DestroyPodium();
        }
        else
        {
            firstBall = !firstBall;
        }
        RequestSerialization();

        var jackBall=  jackBallObjectPool.TryToSpawn();
        if(jackBall == null) { Debug.Log("ジャックボールが存在しないぜ"); return; };
        if(!Networking.IsOwner(Networking.LocalPlayer, jackBall)) Networking.SetOwner(Networking.LocalPlayer,jackBall);

        if (firstBall)
        {
            jackBall.transform.position = redTransform.position;
            jackBall.GetComponent<BallLogic>().ResetBall();
            SendConvertedGameInfoMessage(GameInfoKey.RedTeamThrowJackBall);
        }
        else
        {
            jackBall.transform.position = blueTransform.position;
            jackBall.GetComponent<BallLogic>().ResetBall();
            SendConvertedGameInfoMessage(GameInfoKey.BlueTeamThrowJackBall);
        }
    }
    public void FirstBallLand()
    {
        if (firstBall)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RedBallSpawn));
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(BlueBallSpawn));
        }
    }
    public void FirstBallMiss()
    {
        if (Networking.IsOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0])) Networking.SetOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0]);

        if (firstBall)
        {
            jackBallObjectPool.Pool[0].transform.position = redTransform.position;
        }
        else
        {
            jackBallObjectPool.Pool[0].transform.position = blueTransform.position;
        }

        SendConvertedGameInfoMessage(GameInfoKey.ThrowJackBallAgain);
        jackBallObjectPool.Pool[0].GetComponent<BallLogic>().ResetBall();
    }
    public void NextBall()
    {
        //ジャックボールがデッドボールならクロスポジションに戻す
        if (jackBallObjectPool.Pool[0].GetComponent<BallLogic>().IsDeadBall) 
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0])) { Networking.SetOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0]); }
            jackBallObjectPool.Pool[0].transform.position = jackBallRespawnTransform.position;
        }

        //どちらも投げ終わっていたらゲーム終了！
        if (redCurrentBall <= 0 && blueCurrentBall <= 0)
        {
            SendConvertedGameInfoMessage(GameInfoKey.Aggregating);
            SendConvertedPlaySe(SeKey.DrumRoll);
            SendCustomEventDelayedSeconds(nameof(FinishEnd), 4f);
            return;
        }
        //一回もボールを投げていないチームがいたらそのチームにボールを渡す処理
        if (redCurrentBall == 6)
        {
            RedBallSpawn();
            return;
        }
        if (blueCurrentBall == 6)
        {
            BlueBallSpawn();
            return;
        }
        BallParticleToClosestBall();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All,nameof(BallParticleToClosestBall));

        bool redCloser = IsRedTeamCloser();

        // 最も近いチームと逆チームのボールが残っていればそちらを出す、なければ相手を出す
        if ((!redCloser && redCurrentBall > 0) || (redCloser && blueCurrentBall <= 0))
        {
            RedBallSpawn();
        }
        else
        {
            BlueBallSpawn();
        }
    }
    public void FinishEnd()
    {
        JudgeWinner(out int team, out int score);

        if (team == 1)
        {
            SendConvertedGameInfoMessage(GameInfoKey.RedGetScore, score);
            redScore+= score;
        }
        else if(team == 2)
        {
            SendConvertedGameInfoMessage(GameInfoKey.BlueGetScore, score);
            blueScore+= score;
        }
        else
        {
            //有効な勝敗がない
        }
        RequestSerialization();

        SendConvertedPlaySe(SeKey.Additional);

        if (currentEnd < maxEnd)
        {
            SendCustomEventDelayedSeconds(nameof(GameStart), 5f);
        }
        else
        {
            SendCustomEventDelayedSeconds(nameof(CompleteGame), 5f);
        }
        
    }
    public void CompleteGame()
    {
        teamManager.SetHideObjectBool(true);
        currentEnd = 0;
        RequestSerialization();

        int winTeam = redScore > blueScore ? 1 : (redScore < blueScore ? 2 : 0);

        podium.EndGame(winTeam);

        switch (winTeam)
        {
            case 0:
                SendConvertedGameInfoMessage(GameInfoKey.Draw);
                break;
            case 1:
                SendConvertedGameInfoMessage(GameInfoKey.RedWin);
                break;
            case 2:
                SendConvertedGameInfoMessage(GameInfoKey.BlueWin);
                break;
        }
    }
    public void BallLanding(int ballType, bool isDeadBall)
    {
        if (ballType == 0)
        {
            if(isDeadBall) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(FirstBallMiss)); }
            else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(FirstBallLand)); }
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(NextBall));
        }
    }
    public void RedBallSpawn()
    {
        var ball = redBallObjectPool.TryToSpawn();
        if (ball == null) { return; }
        ball.GetComponent<BallLogic>().ResetBall();
        redCurrentBall--;
        SendConvertedGameInfoMessage(GameInfoKey.RedTeamThrowBall);
        SendConvertedPlaySe(SeKey.Pop);
    }
    public void BlueBallSpawn()
    {
        var ball = blueBallObjectPool.TryToSpawn();
        if (ball == null) { return; }
        ball.GetComponent<BallLogic>().ResetBall();
        blueCurrentBall--;
        SendConvertedGameInfoMessage(GameInfoKey.BlueTeamThrowBall);
        SendConvertedPlaySe(SeKey.Pop);
    }
    public void TeleportLocal()
    {
        var teams = teamManager.GetTeams();
        if(Networking.LocalPlayer.playerId < teams.Count)
        {
            int team = (int)teams[Networking.LocalPlayer.playerId].Double;
            switch (team)
            {
                case 0:
                    break;
                case 1:
                    Networking.LocalPlayer.TeleportTo(redTransform.position, redTransform.rotation);
                    break;
                case 2:
                     Networking.LocalPlayer.TeleportTo(blueTransform.position, blueTransform.rotation);
                     break;
            }

        }

    }
    public void BallParticleToClosestBall()
    {
        GameObject jackBall = jackBallObjectPool.Pool[0];
        if (jackBall == null || !jackBall.activeSelf)
        {
            Debug.LogWarning("ジャックボールが存在しません");
            return;
        }

        Vector3 jackPos = jackBall.transform.position;

        GameObject closestBall = null;
        float minDist = float.MaxValue;

        // 赤ボールの探索
        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (IsValidBall(redBall))
            {
                float dist = Vector3.Distance(jackPos, redBall.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestBall = redBall;
                }
            }
        }

        // 青ボールの探索
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (IsValidBall(blueBall))
            {
                float dist = Vector3.Distance(jackPos, blueBall.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestBall = blueBall;
                }
            }
        }

        if (closestBall != null)
        {
            ballParticle.transform.position = closestBall.transform.position;
            ballParticle.Play();
        }
        else
        {
            Debug.LogWarning("有効なボールが見つかりませんでした");
        }
    }
    private bool IsRedTeamCloser()
    {
        GameObject jackBall = jackBallObjectPool.Pool[0];
        if (jackBall == null || !jackBall.activeSelf)
        {
            Debug.LogWarning("ジャックボールが存在しません");
            return false;
        }

        Vector3 jackPos = jackBall.transform.position;

        float minRedDist = float.MaxValue;
        float minBlueDist = float.MaxValue;

        // 赤ボールの最短距離
        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (IsValidBall(redBall))
            {
                float dist = Vector3.Distance(jackPos, redBall.transform.position);
                if (dist < minRedDist) minRedDist = dist;
            }
        }

        // 青ボールの最短距離
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (IsValidBall(blueBall))
            {
                float dist = Vector3.Distance(jackPos, blueBall.transform.position);
                if (dist < minBlueDist) minBlueDist = dist;
            }
        }

        // 最小距離を比較
        return minRedDist < minBlueDist;
    }
    public void JudgeWinner(out int winningTeam, out int score)
    {
        winningTeam = 0;
        score = 0;

        GameObject jack = jackBallObjectPool.Pool[0];
        if (jack == null || !jack.activeSelf) return;

        Vector3 jackPos = jack.transform.position;

        // 配列サイズは最大ボール数に合わせる（例：6個）
        float[] redDists = new float[6];
        int redCount = 0;

        float[] blueDists = new float[6];
        int blueCount = 0;

        // 赤ボールの距離
        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (!IsValidBall(redBall)) continue;
            if (redCount >= redDists.Length) break;

            redDists[redCount++] = Vector3.Distance(jackPos, redBall.transform.position);
        }

        // 青ボールの距離
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (!IsValidBall(blueBall)) continue;
            if (blueCount >= blueDists.Length) break;

            blueDists[blueCount++] = Vector3.Distance(jackPos, blueBall.transform.position);
        }

        if (redCount == 0 && blueCount == 0) return;

        // 距離配列を昇順ソート（簡易バブルソート）
        SortDistances(redDists, redCount);
        SortDistances(blueDists, blueCount);

        float closestRed = redCount > 0 ? redDists[0] : float.MaxValue;
        float closestBlue = blueCount > 0 ? blueDists[0] : float.MaxValue;

        if (closestRed < closestBlue)
        {
            winningTeam = 1;
            for (int i = 0; i < redCount; i++)
            {
                if (redDists[i] < closestBlue) score++;
                else break;
            }
        }
        else if (closestBlue < closestRed)
        {
            winningTeam = 2;
            for (int i = 0; i < blueCount; i++)
            {
                if (blueDists[i] < closestRed) score++;
                else break;
            }
        }
    }
    private void SortDistances(float[] arr, int length)
    {
        for (int i = 0; i < length - 1; i++)
        {
            for (int j = 0; j < length - 1 - i; j++)
            {
                if (arr[j] > arr[j + 1])
                {
                    float temp = arr[j];
                    arr[j] = arr[j + 1];
                    arr[j + 1] = temp;
                }
            }
        }
    }

    private bool IsValidBall(GameObject ball)
    {
        if (ball == null || !ball.activeSelf) return false;
        BallLogic logic = ball.GetComponent<BallLogic>();
        return logic != null && !logic.IsDeadBall;
    }
    public void GetAllBallOwner()
    {
        if (jackBallObjectPool.Pool[0].activeSelf && !Networking.IsOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0])) Networking.SetOwner(Networking.LocalPlayer, jackBallObjectPool.Pool[0]);

        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (redBall.activeSelf && !Networking.IsOwner(Networking.LocalPlayer, redBall)) Networking.SetOwner(Networking.LocalPlayer,redBall);
        }
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (blueBall.activeSelf && !Networking.IsOwner(Networking.LocalPlayer, blueBall))Networking.SetOwner(Networking.LocalPlayer, blueBall);
        }
    }
    public void AllBallReturn()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DropAllBall));
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(ReturnObject));
    }
    public void DropAllBall()
    {
        if (jackBallObjectPool.Pool[0].activeSelf && jackBallObjectPool.Pool[0].GetComponent<VRCPickup>().IsHeld) jackBallObjectPool.Pool[0].GetComponent<VRCPickup>().Drop();

        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (redBall.activeSelf && redBall.GetComponent<VRCPickup>().IsHeld) redBall.GetComponent<VRCPickup>().Drop();
        }
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (blueBall.activeSelf && blueBall.GetComponent<VRCPickup>().IsHeld) blueBall.GetComponent<VRCPickup>().Drop();
        }
    }
    public void ReturnObject()
    {
        if (jackBallObjectPool.Pool[0].activeSelf) jackBallObjectPool.Return(jackBallObjectPool.Pool[0]);

        foreach (GameObject redBall in redBallObjectPool.Pool)
        {
            if (redBall.activeSelf) redBallObjectPool.Return(redBall);
        }
        foreach (GameObject blueBall in blueBallObjectPool.Pool)
        {
            if (blueBall.activeSelf) blueBallObjectPool.Return(blueBall);
        }
    }
    public override void OnPreSerialization()
    {
        SetGameInfo();
    }

    public override void OnDeserialization()
    {
        SetGameInfo();
    }
    private void SetGameInfo()
    {
        gameInfo.SetEnd(currentEnd, maxEnd);
        gameInfo.SetBallCount(redCurrentBall,blueCurrentBall);
        gameInfo.SetScore(redScore,blueScore);
    }
    private void SendConvertedGameInfoMessage(GameInfoKey key,int score = 0)
    {
        int index = (int)key;
        NetworkCalling.SendCustomNetworkEvent((IUdonEventReceiver)this, NetworkEventTarget.All, nameof(PrintGameInfoMessage), index,score);
    }
    private void SendConvertedPlaySe(SeKey key)
    {
        int id = (int)key;
        NetworkCalling.SendCustomNetworkEvent((IUdonEventReceiver)this, NetworkEventTarget.All, nameof(PlaySe), id);
    }
    [NetworkCallable]
    public void PrintGameInfoMessage(int key, int score)
    {
        gameInfo.SetGameInfoMessage(key,score);
    }
    [NetworkCallable]
    public void PlaySe(int id)
    {
        soundManager.PlaySeByInt(id);
    }
}
