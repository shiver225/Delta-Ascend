using UnityEngine;
using TMPro;

public class timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;  // Assign in Inspector
    public bool isCountdown = false;   // Set true for countdown, false for stopwatch
    public float startTime = 30f;      // Starting time (used if countdown)

    private float currentTime;
    private bool isRunning = true;     // Start immediately

    void Start()
    {
        currentTime = isCountdown ? startTime : 0f;
        UpdateTimerText();
    }

    void Update()
    {
        if (!isRunning) return;

        if (isCountdown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                isRunning = false;
                OnTimerEnd();
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        UpdateTimerText();
    }

    public void StopTimer()
    {
        isRunning = false;
        Debug.Log("⏱️ Timer stopped!");
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void OnTimerEnd()
    {
        Debug.Log("? Timer finished!");
        // Optional: play sound, trigger haptic, or change color
    }
}
