
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BallLogic : UdonSharpBehaviour
{
    [SerializeField] InGameManager gameManager;
    [SerializeField] GameObject deadBallVisual;
    [SerializeField, Header("０ならジャックボール１なら赤２なら青")] int ballType;

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
        gameManager.BallLanding(ballType, IsDeadBall);
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
}
