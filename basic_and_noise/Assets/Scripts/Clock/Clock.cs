using System;
using UnityEngine;
public class Clock : MonoBehaviour
{
    const float hoursToDegrees = -30f, minutesToDegrees = -6f, secondsToDegrees = -6f;

    [SerializeField]
    Transform hoursPivot,minutesPivot,secondsPivot;

    void Awake() {
        
        // hoursPivot.localRotation = Quaternion.Euler(0, 0, -30);
        TimeSpan time = DateTime.Now.TimeOfDay;
        Debug.Log(time.TotalSeconds);
        hoursPivot.localRotation = 
            Quaternion.Euler(0, 0, hoursToDegrees * (float)time.TotalHours);
        minutesPivot.localRotation =
            Quaternion.Euler(0f, 0f, minutesToDegrees * (float)time.TotalMinutes);
        secondsPivot.localRotation =
            Quaternion.Euler(0f, 0f, secondsToDegrees * (float)time.TotalSeconds);
    }

    void Update()
    {

        TimeSpan time = DateTime.Now.TimeOfDay;
        Debug.Log(time.TotalMinutes);
        hoursPivot.localRotation =
            Quaternion.Euler(0, 0, hoursToDegrees * (float)time.TotalHours);
        minutesPivot.localRotation =
            Quaternion.Euler(0f, 0f, minutesToDegrees * (float)time.TotalMinutes);
        secondsPivot.localRotation =
            Quaternion.Euler(0f, 0f, secondsToDegrees * (float)time.TotalSeconds);
    }
}