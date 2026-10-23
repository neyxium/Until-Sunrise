using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SunCycle : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Gradient skyColors;
    [SerializeField] private Light2D sunLight;
    [SerializeField] private float nightIntensity = 0.05f;
    [SerializeField] private float dayGlobalIntensity = 0.8f;
    [SerializeField] private float daySunIntensity = 10.0f;
    [SerializeField] private AnimationCurve dawnCurve = new AnimationCurve();
    void Update()
    {
        if (GameManger.Instance == null || sunLight == null || globalLight == null) return;

        float lightPercent = dawnCurve.Evaluate(GameManger.Instance.elapseNight);

        globalLight.intensity = Mathf.Lerp(nightIntensity, dayGlobalIntensity, lightPercent);
        globalLight.color = skyColors.Evaluate(lightPercent);

        sunLight.intensity = Mathf.Lerp(0f, daySunIntensity, lightPercent);
        
        transform.position = new Vector2(transform.position.x, Mathf.Lerp(-10, 0, GameManger.Instance.elapseNight));
    }
}
