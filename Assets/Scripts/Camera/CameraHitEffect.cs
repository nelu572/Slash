using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(CameraFollow))]
public class CameraHitEffect : MonoBehaviour
{
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Player player;
    [SerializeField, Min(0.01f)] private float duration = 0.9f; // HP가 0일 때의 비네트 전체 재생 시간(초)
    [SerializeField, Min(0f)] private float holdDuration = 0.08f; // 피격 직후 비네트가 바로 약해지지 않도록 유지할 시간
    [SerializeField, Min(0.01f)] private float chromaticDuration = 0.25f;
    [SerializeField] private Color vignetteColor = new Color(0.8f, 0.03f, 0.03f);
    [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.45f;
    [SerializeField, Range(0f, 1f)] private float chromaticIntensity = 0.3f;
    [SerializeField, Min(0f)] private float shakeDuration = 0.15f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float highHealthEffectWeight = 0.5f; // HP가 가득할 때 적용할 효과 강도 비율
    [SerializeField, Range(0.1f, 1f)] private float highHealthDurationWeight = 0.65f; // HP가 가득할 때 적용할 재생 시간 비율

    private CameraFollow cameraFollow;
    private VolumeProfile runtimeProfile;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private Color originalVignetteColor;

    private float originalVignetteIntensity;
    private float originalChromaticIntensity;
    private float remainingTime;
    private float effectDuration;
    private float chromaticEffectDuration;
    private float effectWeight;

    private void Awake()
    {
        if (globalVolume == null || player == null)
            return;

        cameraFollow = GetComponent<CameraFollow>();

        // 에셋 원본 대신 플레이 중 생성한 프로필에만 효과를 적용한다.
        runtimeProfile = globalVolume.profile;
        if (runtimeProfile.TryGet(out vignette))
        {
            originalVignetteColor = vignette.color.value;
            originalVignetteIntensity = vignette.intensity.value;
        }
        if (runtimeProfile.TryGet(out chromaticAberration))
            originalChromaticIntensity = chromaticAberration.intensity.value;
    }

    public void Play()
    {
        if (!isActiveAndEnabled || player == null || cameraFollow == null)
            return;

        // 최대 HP가 달라져도 남은 체력 비율에 따라 같은 정도의 효과를 적용한다.
        float healthRatio = Mathf.Clamp01((float)player.CurrentHealth / Mathf.Max(1, player.MaxHealth));
        effectWeight = Mathf.Lerp(highHealthEffectWeight, 1f, 1f - healthRatio);
        float durationWeight = Mathf.Lerp(highHealthDurationWeight, 1f, 1f - healthRatio);
        effectDuration = duration * durationWeight;
        chromaticEffectDuration = chromaticDuration * durationWeight;
        remainingTime = effectDuration;

        ApplyEffect(effectWeight, effectWeight);
        cameraFollow.Shake(shakeDuration * effectWeight, shakeStrength * effectWeight);
    }

    private void Update()
    {
        if (remainingTime <= 0f)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);

        if (remainingTime == 0f)
        {
            ApplyEffect(0f, 0f);
            return;
        }

        // 비네트는 잠깐 유지한 뒤 천천히 줄이고, 색수차는 먼저 사라지게 해 시야를 확보한다.
        float elapsedTime = effectDuration - remainingTime;
        float fadeStart = Mathf.Min(holdDuration, effectDuration * 0.5f);
        float progress = Mathf.InverseLerp(fadeStart, effectDuration, elapsedTime);
        float strength = 1f - Mathf.SmoothStep(0f, 1f, progress);
        float chromaticStrength = 1f - Mathf.SmoothStep(0f, 1f, elapsedTime / chromaticEffectDuration);
        ApplyEffect(strength * effectWeight, chromaticStrength * effectWeight);
    }

    private void ApplyEffect(float strength, float chromaticStrength)
    {
        if (vignette != null)
        {
            vignette.color.value = strength > 0f ? vignetteColor : originalVignetteColor;
            vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, vignetteIntensity, strength);
        }
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.Lerp(originalChromaticIntensity, chromaticIntensity, chromaticStrength);
    }

    private void OnDisable()
    {
        // 효과 도중 카메라나 컴포넌트가 꺼져도 피격 색이 화면에 남지 않게 한다.
        remainingTime = 0f;
        ApplyEffect(0f, 0f);
    }
}
