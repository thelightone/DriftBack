using UnityEngine;

/// <summary>
/// Триггер финиша: пересечение коллайдером игрока (Rigidbody на машине).
/// Разместите в конце трассы, collider — Is Trigger, ширина по дороге.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (RaceFlowManager.Instance == null)
            return;

        var car = other.GetComponentInParent<CarController>();
        if (car == null && other.attachedRigidbody != null)
            car = other.attachedRigidbody.GetComponent<CarController>();

        if (car == null)
            return;

        RaceFlowManager.Instance.OnPlayerFinished(car);
    }
}
