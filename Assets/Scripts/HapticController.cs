using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticController : MonoBehaviour
{
    private Coroutine vibrationCoroutine;

    public void Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        if (vibrationCoroutine != null)
            StopCoroutine(vibrationCoroutine);

        vibrationCoroutine = StartCoroutine(
            VibrateCoroutine(lowFrequency, highFrequency, duration)
        );
    }

    private IEnumerator VibrateCoroutine(
        float lowFrequency,
        float highFrequency,
        float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);

        yield return new WaitForSeconds(duration);

        Gamepad.current.SetMotorSpeeds(0f, 0f);

        vibrationCoroutine = null;
    }

    private void OnDisable()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}