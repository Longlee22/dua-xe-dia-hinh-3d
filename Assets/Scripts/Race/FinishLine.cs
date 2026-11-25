using UnityEngine;

// Vạch đích: khi xe chạm vào -> báo cho RaceManager
public class FinishLine : MonoBehaviour
{
    public RaceManager manager;

    void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<CarController>();
        if (car != null && manager != null && !car.finished) manager.OnCarFinished(car);
    }
}
