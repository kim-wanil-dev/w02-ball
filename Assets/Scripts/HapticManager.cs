using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticManager : MonoBehaviour
{

    [SerializeField] private float _hapticChangeSpeed = 2f;


    private Coroutine _hapticCoroutine;

    private float _currentIntensity;
    private float _targetIntensity;



    public void HapticControl(
        float startIntensity,
        float endIntensity,
        float duration)
    {
        if (Gamepad.current == null)
            return;

        if (_hapticCoroutine != null)
            StopCoroutine(_hapticCoroutine);

        _hapticCoroutine = StartCoroutine(
            HapticCoroutine(
                startIntensity,
                endIntensity,
                duration
            )
        );
    }
    public void HapticControl(float intensity)
    {
        _targetIntensity = Mathf.Clamp01(intensity);

        if (_hapticCoroutine == null)
            _hapticCoroutine = StartCoroutine(HapticCoroutine());
    }

    private IEnumerator HapticCoroutine(
        float startIntensity,
        float endIntensity,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Ease In-Out
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            float intensity = Mathf.Lerp(
                startIntensity,
                endIntensity,
                easedT
            );

            intensity = Mathf.Clamp01(intensity);

            Gamepad.current.SetMotorSpeeds(
                intensity,
                intensity
            );

            yield return null;
        }

        Gamepad.current.SetMotorSpeeds(0f, 0f);

        _hapticCoroutine = null;
    }

    private IEnumerator HapticCoroutine()
    {
        while (true)
        {
            _currentIntensity = Mathf.MoveTowards(
                _currentIntensity,
                _targetIntensity,
                _hapticChangeSpeed * Time.deltaTime
            );

            Gamepad.current?.SetMotorSpeeds(
                _currentIntensity,
                _currentIntensity
            );

            // 목표가 0이고 실제 진동도 거의 0이면 종료
            if (_targetIntensity <= 0f &&
                _currentIntensity <= 0.001f)
            {
                break;
            }

            yield return null;
        }

        Gamepad.current?.SetMotorSpeeds(0f, 0f);

        _currentIntensity = 0f;
        _hapticCoroutine = null;
    }

    public void StopHaptic()
    {
        if (_hapticCoroutine != null)
        {
            StopCoroutine(_hapticCoroutine);
            _hapticCoroutine = null;
        }

        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}