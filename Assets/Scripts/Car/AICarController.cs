using UnityEngine;

// Xe đối thủ do máy tính điều khiển: bám theo tim đường đua.
[RequireComponent(typeof(CarController))]
public class AICarController : MonoBehaviour
{
    public float speedFactor = 0.8f;    // % tốc độ tối đa của xe
    public float laneOffset = 0f;
    public float lookAhead = 18f;

    CarController car;
    float stuckTimer, flipTimer;

    void Awake() { car = GetComponent<CarController>(); car.isPlayer = false; }

    void FixedUpdate()
    {
        if (car.finished) { car.throttleIn = 0f; car.steerIn = 0f; car.brakeIn = true; return; }
        car.brakeIn = false; car.nitroIn = false;

        float z = transform.position.z;
        float tz = Mathf.Min(z + lookAhead + car.SpeedKmh * 0.15f, TerrainGenerator.SizeZ - 10f);
        Vector3 target = new Vector3(TerrainGenerator.CenterX(tz) + laneOffset, transform.position.y, tz);
        Vector3 local = transform.InverseTransformPoint(target);
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;

        car.steerIn = Mathf.Clamp(angle / 25f, -1f, 1f);
        float targetSpeed = car.maxSpeedKmh * speedFactor * (Mathf.Abs(angle) > 25f ? 0.6f : 1f);
        car.throttleIn = car.SpeedKmh < targetSpeed ? 1f : 0f;

        // Kẹt hoặc lật -> đặt lại lên đường
        if (car.raceActive && car.SpeedKmh < 3f) stuckTimer += Time.fixedDeltaTime; else stuckTimer = 0f;
        if (Vector3.Dot(transform.up, Vector3.up) < 0.4f) flipTimer += Time.fixedDeltaTime; else flipTimer = 0f;
        if (stuckTimer > 3f || flipTimer > 2f) Respawn();
    }

    void Respawn()
    {
        stuckTimer = flipTimer = 0f;
        float z = transform.position.z + 3f;
        Vector3 p = new Vector3(TerrainGenerator.CenterX(z) + laneOffset, TerrainGenerator.GroundY(TerrainGenerator.CenterX(z), z) + 1.5f, z);
        transform.SetPositionAndRotation(p, Quaternion.Euler(0f, TerrainGenerator.HeadingDeg(z), 0f));
        car.ResetMotion();
    }
}
