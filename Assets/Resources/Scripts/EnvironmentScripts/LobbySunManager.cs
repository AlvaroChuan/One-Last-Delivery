using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using Mirror;

[RequireComponent(typeof(UnityEngine.Light))]
public class LobbySunManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float _cycleDurationMinutes = 10f;
    [SerializeField, Range(0f, 1f)] private float _currentTimeOfDay = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _cycleStopTime = 0.75f;

    [Header("Optimization")]
    [SerializeField, Min(1)] private int _framesBetweenSunUpdates = 10;

    [Header("Skybox Shader")]
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private string _skyColorPropertyName = "_SkyColor";
    [SerializeField] private string _horizonColorPropertyName = "_HorizonColor";

    [Header("Star Shader")]
    [SerializeField] private string _starsIntensityPropertyName = "_StarsIntensity";
    [SerializeField] private float _starsFadeDurationSeconds = 5f;
    [SerializeField] private float _starsMaxIntensity = 2f;

    [Header("Environment Transitions (HDRI & Clouds)")]
    [SerializeField] private float _environmentTransitionDurationSeconds = 5f;
    [SerializeField] private string _hdriBlendPropertyName = "_HDRIBlend";
    [SerializeField] private float _hdriBlendDay = 0.32f;
    [SerializeField] private float _hdriBlendNight = 0.57f;
    [SerializeField] private string _hdriExposurePropertyName = "_HDRIExposure";
    [SerializeField] private float _hdriExposureNight = 2.2f;
    [SerializeField] private string _cloudsColorPropertyName = "_CloudsColor";
    [SerializeField] private Color _cloudsColorDay = new Color(0.596f, 0.596f, 0.596f);
    [SerializeField] private Color _cloudsColorNight = new Color(0.380f, 0.427f, 0.482f);
    [SerializeField] private string _cloudsPosterizePropertyName = "_CloudsPosterize";
    [SerializeField] private float _cloudsPosterizeNight = 8f;

    [Header("Fog Settings")]
    [SerializeField] private float _fogDensityDay = 0.016f;
    [SerializeField] private float _fogDensityNight = 0.033f;
    [SerializeField] private string _fogHeightPropertyName = "_FogHeight";
    [SerializeField] private float _fogHeightDay = 14f;
    [SerializeField] private float _fogHeightNight = 6f;

    [Header("Post Processing Settings")]
    [SerializeField] private Volume _globalVolume;
    [SerializeField] private float _vignetteIntensityDay = 0.29f;
    [SerializeField] private float _vignetteIntensityNight = 0.44f;

    [Header("Height Offset Settings")]
    [SerializeField] private string _heightOffsetPropertyName = "_HeightOffset";
    [SerializeField] private float _heightOffsetLow = 0.4f;
    [SerializeField] private float _heightOffsetHigh = 0.85f;
    [SerializeField, Range(0f, 1f)] private float _heightDropStartTime = 0.0f;
    [SerializeField, Range(0f, 1f)] private float _dawnStartTime = 0.08f;
    [SerializeField, Range(0f, 1f)] private float _dawnEndTime = 0.20f;

    [Header("Day Randomizers")]
    [SerializeField] private float _dayExposureDefault = 1.5f;
    [SerializeField] private float _dayExposureMin = 1.0f;
    [SerializeField] private float _dayExposureMax = 1.5f;
    [SerializeField] private float _exposureChangeDuration = 3f;
    [SerializeField] private float _exposureWaitDuration = 5f;

    [Header("Sky Gradients")]
    [SerializeField] private Gradient _skyColorGradient;
    [SerializeField] private Gradient _horizonColorGradient;
    [SerializeField] private Gradient _directionalLightColorGradient;

    [Header("Glass Materials Settings")]
    [SerializeField] private Material[] _glassMaterials;
    [SerializeField] private string _glassColorPropertyName = "_ColorB";
    [SerializeField] private Color _glassColorDay = Color.white;
    [SerializeField] private Color _glassColorNight = Color.blue;

    private UnityEngine.Light _directionalLight;
    private float _cycleDurationSeconds;
    private float _currentStarsIntensity = 0f;
    private float _environmentTransitionProgress = 0f;
    private float _dynamicDayExposure;
    private float _exposureTimer = 0f;
    private bool _isWaitingExposure = true;
    private float _exposureStartValue;
    private float _exposureTargetValue;
    private bool _isFirstMorning = true;
    private bool _sentNightfallEvent = false;
    private Vignette _vignette;
    private int _sunUpdateFrameCounter = 0;

    private void Start()
    {
        _directionalLight = GetComponent<UnityEngine.Light>();
        _cycleDurationSeconds = _cycleDurationMinutes * 60f;
        _dynamicDayExposure = _dayExposureDefault;

        if (_globalVolume != null && _globalVolume.profile.TryGet(out _vignette))
        {
        }

        if (_glassMaterials != null && _glassMaterials.Length > 0)
        {
            for (int i = 0; i < _glassMaterials.Length; i++)
            {
                if (_glassMaterials[i] != null)
                {
                    _glassMaterials[i].SetColor(_glassColorPropertyName, _glassColorDay);
                }
            }
        }

        UpdateSunRotation(true);
    }

    private void Update()
    {
        UpdateTime();
        UpdateSunRotation();
        UpdateSkyboxColors();
        UpdateStarsIntensity();
        UpdateDaytimeRandomizers();
        UpdateEnvironmentTransitions();
        UpdateHeightOffset();
        UpdateDirectionalLightColor();
    }

    private void UpdateTime()
    {
        if (_currentTimeOfDay < _cycleStopTime)
        {
            _currentTimeOfDay += Time.deltaTime / _cycleDurationSeconds * _cycleStopTime;
            if (_currentTimeOfDay > _cycleStopTime)
            {
                _currentTimeOfDay = _cycleStopTime;
            }
        }
        if (_currentTimeOfDay >= 1f)
        {
            _currentTimeOfDay -= 1f;
        }
    }

    private bool IsNight()
    {
        if (_isFirstMorning)
        {
            if (_currentTimeOfDay >= 0.08f)
            {
                _isFirstMorning = false;
            }
            else
            {
                return false;
            }
        }

        return _currentTimeOfDay >= 0.58f || _currentTimeOfDay < 0.08f;
    }

    private void UpdateSunRotation(bool forceUpdate = false)
    {
        if (!forceUpdate)
        {
            _sunUpdateFrameCounter++;
            if (_sunUpdateFrameCounter < _framesBetweenSunUpdates)
            {
                return;
            }
        }

        _sunUpdateFrameCounter = 0;

        float sunAngle = (_currentTimeOfDay * 360f) - 30f;
        float lightAngle = sunAngle;

        if (IsNight())
        {
            lightAngle -= 180f;
        }

        transform.localRotation = Quaternion.Euler(lightAngle, 170f, 0f);
        Vector3 realSunDirection = Quaternion.Euler(sunAngle, 170f, 0f) * Vector3.forward;
        Shader.SetGlobalVector("_SunDirection", realSunDirection);
    }

    private void UpdateSkyboxColors()
    {
        Color currentHorizonColor = _horizonColorGradient.Evaluate(_currentTimeOfDay);

        if (_skyboxMaterial != null)
        {
            Color currentSkyColor = _skyColorGradient.Evaluate(_currentTimeOfDay);
            _skyboxMaterial.SetColor(_skyColorPropertyName, currentSkyColor);
            _skyboxMaterial.SetColor(_horizonColorPropertyName, currentHorizonColor);
        }
        RenderSettings.fogColor = currentHorizonColor;
    }

    private void UpdateStarsIntensity()
    {
        bool isNight = IsNight();
        float targetIntensity = isNight ? _starsMaxIntensity : 0f;
        float fadeSpeed = _starsMaxIntensity / _starsFadeDurationSeconds;
        _currentStarsIntensity = Mathf.MoveTowards(_currentStarsIntensity, targetIntensity, fadeSpeed * Time.deltaTime);

        if (_skyboxMaterial != null)
        {
            _skyboxMaterial.SetFloat(_starsIntensityPropertyName, _currentStarsIntensity);
        }
    }

    private void UpdateDaytimeRandomizers()
    {
        if (_environmentTransitionProgress == 1f)
        {
            _dynamicDayExposure = _dayExposureDefault;
            _isWaitingExposure = true;
            _exposureTimer = 0f;
        }
        else if (_environmentTransitionProgress == 0f)
        {
            _exposureTimer += Time.deltaTime;
            if (_isWaitingExposure)
            {
                if (_exposureTimer >= _exposureWaitDuration)
                {
                    _isWaitingExposure = false;
                    _exposureTimer = 0f;
                    _exposureStartValue = _dynamicDayExposure;
                    _exposureTargetValue = UnityEngine.Random.Range(_dayExposureMin, _dayExposureMax);
                }
            }
            else
            {
                float t = _exposureTimer / _exposureChangeDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                _dynamicDayExposure = Mathf.Lerp(_exposureStartValue, _exposureTargetValue, smoothT);
                if (t >= 1f)
                {
                    _isWaitingExposure = true;
                    _exposureTimer = 0f;
                }
            }
        }
    }

    private void UpdateEnvironmentTransitions()
    {
        bool isNight = IsNight();
        float fadeStep = Time.deltaTime / _environmentTransitionDurationSeconds;

        if (isNight)
        {
            _environmentTransitionProgress = Mathf.Clamp01(_environmentTransitionProgress + fadeStep);
        }
        else
        {
            _environmentTransitionProgress = Mathf.Clamp01(_environmentTransitionProgress - fadeStep);
        }

        float smoothProgress = Mathf.SmoothStep(0f, 1f, _environmentTransitionProgress);

        if (_skyboxMaterial != null)
        {
            float currentBlend = Mathf.Lerp(_hdriBlendDay, _hdriBlendNight, smoothProgress);
            float currentExposure = Mathf.Lerp(_dynamicDayExposure, _hdriExposureNight, smoothProgress);
            Color currentClouds = Color.Lerp(_cloudsColorDay, _cloudsColorNight, smoothProgress);

            _skyboxMaterial.SetFloat(_hdriBlendPropertyName, currentBlend);
            _skyboxMaterial.SetFloat(_hdriExposurePropertyName, currentExposure);
            _skyboxMaterial.SetColor(_cloudsColorPropertyName, currentClouds);
            _skyboxMaterial.SetFloat(_fogHeightPropertyName, Mathf.Lerp(_fogHeightDay, _fogHeightNight, smoothProgress));
        }

        if (_glassMaterials != null && _glassMaterials.Length > 0)
        {
            Color currentGlassColor = Color.Lerp(_glassColorDay, _glassColorNight, smoothProgress);
            for (int i = 0; i < _glassMaterials.Length; i++)
            {
                if (_glassMaterials[i] != null)
                {
                    _glassMaterials[i].SetColor(_glassColorPropertyName, currentGlassColor);
                }
            }
        }

        RenderSettings.fogDensity = Mathf.Lerp(_fogDensityDay, _fogDensityNight, smoothProgress);

        if (_vignette != null)
        {
            float targetVignette = Mathf.Lerp(_vignetteIntensityDay, _vignetteIntensityNight, smoothProgress);
            _vignette.intensity.Override(targetVignette);
        }
    }

    private void UpdateHeightOffset()
    {
        if (_skyboxMaterial != null)
        {
            float currentHeight = _heightOffsetHigh;
            if (_currentTimeOfDay >= _heightDropStartTime && _currentTimeOfDay < _dawnStartTime)
            {
                float t = (_currentTimeOfDay - _heightDropStartTime) / (_dawnStartTime - _heightDropStartTime);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                currentHeight = Mathf.Lerp(_heightOffsetHigh, _heightOffsetLow, smoothT);
            }
            else if (_currentTimeOfDay >= _dawnStartTime && _currentTimeOfDay <= _dawnEndTime)
            {
                float t = (_currentTimeOfDay - _dawnStartTime) / (_dawnEndTime - _dawnStartTime);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                currentHeight = Mathf.Lerp(_heightOffsetLow, _heightOffsetHigh, smoothT);
            }

            _skyboxMaterial.SetFloat(_heightOffsetPropertyName, currentHeight);
        }
    }

    private void UpdateDirectionalLightColor()
    {
        _directionalLight.color = _directionalLightColorGradient.Evaluate(_currentTimeOfDay);
    }
}