using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DesktopBallSystem : UdonSharpBehaviour
{
    public LineRenderer lineRenderer;
    public float shootForce = 10f;
    public int simulationSteps = 30;
    public float simulationStepTime = 0.1f;

    private Rigidbody rb;
    private bool isHeld = false;
    private bool hasShot = false;
    private Vector3 currentDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError("LineRenderer が設定されていません！");
                return;
            }
        }

        lineRenderer.enabled = false;
        rb.useGravity = false;
    }

    public override void OnPickup()
    {
        isHeld = true;
        hasShot = false;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    public override void OnDrop()
    {
        isHeld = false;
        lineRenderer.enabled = false;
    }
    void Update()
    {
        if (!isHeld || hasShot) return;

        VRCPlayerApi player = Networking.LocalPlayer;
        if (player == null) return;

        if (player.IsUserInVR())
        {
            // VRユーザー：右手の方向
            Vector3 forward = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Vector3.forward;
            Vector3 right = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Vector3.right;
            currentDirection = (forward + right).normalized;
        }
        else
        {
            // デスクトップ：カメラの forward 方向
            Quaternion headRot = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            currentDirection = headRot * Vector3.forward;
        }

        Vector3[] points = SimulateTrajectory(transform.position, currentDirection * shootForce);
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.enabled = true;
    }


    void FixedUpdate()
    {
        if (hasShot)
        {
            Debug.DrawRay(transform.position, rb.velocity, Color.red, 0.1f);
        }
    }


    public override void OnPickupUseDown()
    {
        if (!isHeld || hasShot) return;

        GetComponent<VRC_Pickup>().Drop();
        // 発射処理
        rb.useGravity = true;
        rb.isKinematic= false;
        rb.AddForce(currentDirection * shootForce, ForceMode.VelocityChange);

        hasShot = true;
        lineRenderer.enabled = false;
    }

    Vector3[] SimulateTrajectory(Vector3 startPos, Vector3 startVel)
    {
        Vector3[] points = new Vector3[simulationSteps];
        Vector3 pos = startPos;
        Vector3 vel = startVel;

        for (int i = 0; i < simulationSteps; i++)
        {
            points[i] = pos;
            vel += Physics.gravity * simulationStepTime;
            pos += vel * simulationStepTime;
        }

        return points;
    }
}
