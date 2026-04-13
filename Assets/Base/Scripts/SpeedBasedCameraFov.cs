using UnityEngine;

/// <summary>
/// Вешается на объект с Camera: плавно меняет field of view от скорости машины (через GameController.PlayerCar или явное поле).
/// </summary>
[RequireComponent(typeof(Camera))]
public class SpeedBasedCameraFov : MonoBehaviour
{
    [Header("Источник скорости")]
    [Tooltip("Если не задано, используется GameController.PlayerCar")]
    [SerializeField] private CarController carOverride;

    [Header("FOV")]
    [SerializeField] private float minFov = 60f;
    [SerializeField] private float maxFov = 78f;

    [Header("Диапазон скорости (км/ч, как у CarController.SpeedInHour)")]
    [SerializeField] private float speedKphForMinFov;
    [SerializeField] private float speedKphForMaxFov = 140f;

    [Tooltip("Время сглаживания FOV (сек)")]
    [SerializeField] private float smoothTime = 0.25f;

    Camera _camera;
    float _fovVelocity;

    void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (_camera.orthographic)
            return;

        CarController car = carOverride;
        if (car == null && GameController.Instance != null)
            car = GameController.PlayerCar;

        if (car == null)
            return;

        float targetFov = ComputeTargetFov(car.SpeedInHour);
        _camera.fieldOfView = Mathf.SmoothDamp(
            _camera.fieldOfView,
            targetFov,
            ref _fovVelocity,
            Mathf.Max(0.01f, smoothTime));
    }

    float ComputeTargetFov(float speedKph)
    {
        float span = speedKphForMaxFov - speedKphForMinFov;
        if (span <= 0.001f)
            return minFov;

        float t = Mathf.Clamp01((speedKph - speedKphForMinFov) / span);
        return Mathf.Lerp(minFov, maxFov, t);
    }
}
