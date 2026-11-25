using UnityEngine;

// Điều khiển xe bằng WheelCollider + Rigidbody.
// Người chơi: W/S tiến-lùi, A/D lái, Space phanh, Shift Nitro, R dựng lại xe.
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Thông số")]
    public string carName = "Jeep";
    public float motorTorque = 700f;
    public float maxSteer = 32f;
    public float brakeTorque = 3500f;
    public float maxSpeedKmh = 90f;
    public float nitroMultiplier = 1.7f;

    [Header("Bánh xe")]
    public WheelCollider[] wheels = new WheelCollider[4];      // FL, FR, RL, RR
    public Transform[] wheelMeshes = new Transform[4];

    [Header("Trạng thái")]
    public bool isPlayer = true;
    public bool raceActive = true;
    public float throttleIn, steerIn;
    public bool brakeIn, nitroIn;
    public float nitro = 100f;
    public bool finished;
    public float finishTime;
    public string racerName = "Bạn";

    public AudioSource engineSource;
    public ParticleSystem dust;

    public float SpeedKmh { get; private set; }
    public bool NitroActive { get; private set; }
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    public void Setup(float grip)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.6f, 0f);
        if (wheels[0] != null) wheels[0].ConfigureVehicleSubsteps(5f, 12, 15);
        foreach (var w in wheels)
        {
            var f = w.forwardFriction; f.stiffness = grip; w.forwardFriction = f;
            var s = w.sidewaysFriction; s.stiffness = grip; w.sidewaysFriction = s;
        }
    }

    Vector3 Velocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public void ResetMotion()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 v = Velocity();
        SpeedKmh = v.magnitude * 3.6f;
        float fwd = Vector3.Dot(v, transform.forward);

        if (isPlayer)
        {
            throttleIn = InputHelper.Vertical();
            steerIn = Mathf.MoveTowards(steerIn, InputHelper.Horizontal(), 4f * Time.fixedDeltaTime);
            brakeIn = InputHelper.Brake();
            nitroIn = InputHelper.Nitro();
        }
        if (!raceActive) { throttleIn = 0f; steerIn = 0f; brakeIn = true; nitroIn = false; }

        NitroActive = nitroIn && nitro > 0.5f && throttleIn > 0.1f;
        nitro = NitroActive ? Mathf.Max(0f, nitro - 30f * Time.fixedDeltaTime) : Mathf.Min(100f, nitro + 10f * Time.fixedDeltaTime);

        float limit = maxSpeedKmh * (NitroActive ? 1.3f : 1f);
        float motor = throttleIn * motorTorque * (NitroActive ? nitroMultiplier : 1f);
        if (throttleIn > 0f && SpeedKmh > limit) motor = 0f;
        if (throttleIn < 0f && fwd < 0f && SpeedKmh > 35f) motor = 0f;   // giới hạn tốc độ lùi

        float brake = 0f;
        if (brakeIn) brake = brakeTorque;
        else if ((throttleIn < -0.05f && fwd > 1.5f) || (throttleIn > 0.05f && fwd < -1.5f)) { brake = brakeTorque; motor = 0f; }
        else if (Mathf.Abs(throttleIn) < 0.05f) brake = 300f;

        float steer = steerIn * maxSteer * Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(SpeedKmh / maxSpeedKmh));
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].motorTorque = motor;
            wheels[i].brakeTorque = brake;
        }
        wheels[0].steerAngle = steer;
        wheels[1].steerAngle = steer;

        rb.AddForce(-transform.up * SpeedKmh * 8f);   // lực ép xuống giúp bám đường
    }

    void Update()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            Vector3 p; Quaternion q;
            wheels[i].GetWorldPose(out p, out q);
            wheelMeshes[i].SetPositionAndRotation(p, q);
        }

        // Âm thanh động cơ: phát khi xe chạy, cao độ theo tốc độ
        if (engineSource != null)
        {
            if (!engineSource.isPlaying) engineSource.Play();
            float t = Mathf.Clamp01(SpeedKmh / (maxSpeedKmh * 1.3f));
            engineSource.pitch = Mathf.Lerp(0.7f, 2.3f, t) + (NitroActive ? 0.25f : 0f);
            engineSource.volume = (isPlayer ? 0.5f : 0.3f) * Mathf.Lerp(0.5f, 1f, Mathf.Abs(throttleIn));
        }

        // Hiệu ứng bụi
        if (dust != null)
        {
            var em = dust.emission;
            bool grounded = wheels[2].isGrounded || wheels[3].isGrounded;
            float rate = (grounded && SpeedKmh > 4f) ? Mathf.Lerp(8f, 70f, SpeedKmh / maxSpeedKmh) * (NitroActive ? 1.8f : 1f) : 0f;
            em.rateOverTime = rate;
        }

        if (isPlayer && InputHelper.ResetPressed()) ResetUpright();
    }

    public void ResetUpright()
    {
        transform.position += Vector3.up * 2f;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        ResetMotion();
    }
}
