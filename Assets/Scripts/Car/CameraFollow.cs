using UnityEngine;

// Camera luôn bám theo và nhìn vào xe, di chuyển mượt.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -8f);
    public float posSmooth = 6f, rotSmooth = 8f, lookHeight = 1.2f;

    public void Snap()
    {
        if (target == null) return;
        Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        transform.position = target.position + yaw * offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3 desired = target.position + yaw * offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-posSmooth * Time.deltaTime));

        var t = Terrain.activeTerrain;
        if (t != null)
        {
            float gy = t.SampleHeight(transform.position) + t.transform.position.y + 1f;
            if (transform.position.y < gy) { var p = transform.position; p.y = gy; transform.position = p; }
        }

        Vector3 look = target.position + Vector3.up * lookHeight - transform.position;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-rotSmooth * Time.deltaTime));
    }
}
