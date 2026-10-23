using System;
using UnityEngine;

public class LightEnemy : MonoBehaviour
{
    [SerializeField] private Transform lightPivot;
    [SerializeField] private SpriteRenderer lightSprite;

    [SerializeField] private float lightLength = 10f;
    [SerializeField] private float lightWidth = 2f;

    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float angle = 45f;
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
        if (lightSprite != null)
        {
            lightSprite.color = normalColor;
        }
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
            detectedPlayer.DamagePlayer(_damagePerSec * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = true;
            detectedPlayer = other.GetComponent<PlayerMovement>();

            if (lightSprite != null)
                lightSprite.color = alertColor;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = false;
            detectedPlayer = null;

            if (lightSprite != null)
                lightSprite.color = normalColor;
        }
    }

    private void OnValidate()
    {
        UpdateLightSize();
    }

    private void UpdateLightSize()
    {
        if (lightSprite != null)
        {
            lightSprite.transform.localScale = new Vector3(lightWidth, lightLength, 1f);
            lightSprite.transform.localPosition = new Vector3(lightLength / 1.8f, 0f, 0f);
        }
    }
}
