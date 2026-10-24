using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightEnemy : MonoBehaviour
{
    [SerializeField] private float lightLength = 10f;
    [SerializeField] private float lightWidth = 2f;

    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float angle = 45f;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private Transform lightPivot;
    [SerializeField] private Transform lightColliderTransform; // Sem povlečeš objekt Triangle (zaradi colliderja)
    [SerializeField] private Light2D spotLight;                // Sem povlečeš objekt Light (Light 2D)
    private float startRotation = 0f;

    private Color normalColor = new Color(1f, 1f, 0.5f, 0.3f); // prosojno rumena
    private Color alertColor = new Color(1f, 0.2f, 0.2f, 0.5f);  // rdeča ob alarmu
    private const float _damagePerSec = 30f;
    private bool playerDetected = false;
    private PlayerMovement detectedPlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateLightSize();

        if (spotLight != null)
            spotLight.color = normalColor;
    }

    // Update is called once per frame
    void Update()
    {
        float currentAngle = startRotation + Mathf.Sin(Time.time * speed) * angle;

        if (lightPivot != null)
        {
            lightPivot.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        if (playerDetected && detectedPlayer != null)
        {
            RaycastHit2D wallHit = Physics2D.Linecast(transform.position, detectedPlayer.transform.position, obstacleLayers);

            if (wallHit.collider == null)
            {
                detectedPlayer.DamagePlayer(_damagePerSec * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = true;
            detectedPlayer = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = false;
            detectedPlayer = null;
        }
    }

    private void OnValidate()
    {
        UpdateLightSize();
    }

    private void UpdateLightSize()
    {
        // 1. Povečamo nevidni Triangle collider
        if (lightColliderTransform != null)
        {
            lightColliderTransform.localScale = new Vector3(lightWidth, lightLength, 1f);
            lightColliderTransform.localPosition = new Vector3(lightLength / 1.8f, 0f, 0f);
        }

        // 2. Povečamo pravo 2D luč, da se ujema s trikotnikom
        if (spotLight != null)
        {
            // Dolžina luči
            spotLight.pointLightOuterRadius = lightLength * 0.85f;

            // Iz širine in dolžine izračunamo točen kot snopa v stopinjah
            float spotAngle = 2f * Mathf.Atan2(lightWidth * 0.5f, lightLength) * Mathf.Rad2Deg;
            spotLight.pointLightOuterAngle = spotAngle;
            spotLight.pointLightInnerAngle = spotAngle; // ali spotAngle * 0.8f za mehkejši rob
        }
    }
}
