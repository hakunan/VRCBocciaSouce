
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
    public enum BallType
    {
        JackBall,
        Red,
        Blue
    }

public class BallLogic : UdonSharpBehaviour
{
    [SerializeField] InGameManager gameManager;
    [SerializeField] GameObject deadBallVisual;

    [SerializeField,Header("ボールのタイプ")] BallType ballType;

    [UdonSynced] bool _isGround;
    [UdonSynced, FieldChangeCallback(nameof(IsDeadBall))] private bool _isDeadBall;
    public bool IsDeadBall
    {
        private set { _isDeadBall = value; SetDeadBall(value); }
        get { return _isDeadBall; }
    }
    Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void SetDeadBall(bool value)
    {
        deadBallVisual.SetActive(value);
    }
    public override void OnPickup()
    {
        gameManager.GetAllBallOwner();

    }
    public override void OnDrop()
    {
        UseGravity();
        IsDeadBall = true;
        RequestSerialization();
    }
    private void OnCollisionStay(Collision collision)
    {
        if (_isGround || !Networking.IsOwner(gameObject) || !IsBallStopped() || collision.gameObject.name == "Safe") { return; }

        if(collision.gameObject.name == "Floor")
        {
            _isGround = true;
            SendCustomEventDelayedSeconds(nameof(BallLanding), 2f);
        }
    }
    public void BallLanding()
    {
        gameManager.BallLanding((int)ballType, IsDeadBall);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) { return; }

        if(other.gameObject.name == "GroundCollider")
        {
            IsDeadBall = false;
            RequestSerialization();
        }
        else
        {
            IsDeadBall = true;
            RequestSerialization();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) { return; }

        if (other.gameObject.name == "GroundCollider")
        {
            IsDeadBall = true;
            RequestSerialization();
        }
    }
    public void ResetBall()
    {
        if (!Networking.IsOwner(gameObject)) { Networking.SetOwner(Networking.LocalPlayer, gameObject); }
        NoGravity();
        _isGround = false;
        IsDeadBall= false;
        RequestSerialization();
    }
    private void UpdatePickupPermission()
    {
        var pickup = GetComponent<VRC_Pickup>();
        if (_isGround)
        {
            pickup.pickupable = false;
            return;
        }
        VRCPlayerApi player = Networking.LocalPlayer;

        string teamTag = player.GetPlayerTag("Team");

        if (ballType == BallType.JackBall)
        {
            // ジャックボールのとき、先攻チームのみ持てる
            if ((gameManager.IsFirstBallRed && teamTag == "Red") || (!gameManager.IsFirstBallRed && teamTag == "Blue"))
            {
                pickup.pickupable = true;
            }
            else
            {
                pickup.pickupable = false;
            }
        }
        else
        {
            // 通常の赤青ボール
            pickup.pickupable = (ballType.ToString() == teamTag);
        }
    }
    private bool IsBallStopped()
    {
        return rb.velocity.sqrMagnitude < 0.01f;
    }

    public void UseGravity()
    {
        if (rb == null) { return; }
        rb.isKinematic = false;
        rb.useGravity = true;
    }
    public void NoGravity()
    {
        if (rb == null) { return; }

        rb.isKinematic = true;
        rb.useGravity = false;
    }
    public override void OnDeserialization()
    {
        UpdatePickupPermission();
    }
}
