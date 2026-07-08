using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeIndicator : MonoBehaviour
{
    struct DayTime
    {
        public int hour;
        public int minute;
    }
    [Header("Settings")]
    [SerializeField] int _startHour = 18;
    [SerializeField] int _totalHours = 8;
    [Header("References")]
    [SerializeField] TextMeshProUGUI _timeText;
    [SerializeField] Image _dayNightImage;
    [SerializeField] Sprite _nightSprite;
    private DayTime _currentDayTime;
    private WorkdayManager _workdayManager;


    void OnEnable()
    {
        SunManager.OnNightfall += OnNightfall;
    }

    void OnDisable()
    {
        SunManager.OnNightfall -= OnNightfall;
    }

    void Update()
    {
        if (_workdayManager == null)
        {
            _workdayManager = FindAnyObjectByType<WorkdayManager>();
            if (_workdayManager == null) return;
        }

        float workdayProgress = _workdayManager.WorkdayProgress;
        float currentHour = _startHour + (_totalHours * workdayProgress);
        _currentDayTime.hour = Mathf.FloorToInt(currentHour);
        _currentDayTime.minute = Mathf.FloorToInt((currentHour - _currentDayTime.hour) * 60f);
        _currentDayTime.hour = (_currentDayTime.hour + 24) % 24; // Ensure hour is within 0-23 range
        _timeText.text = $"{_currentDayTime.hour:D2}:{_currentDayTime.minute:D2}";
    }

    private void OnNightfall()
    {
        _dayNightImage.sprite = _nightSprite;
    }
}
