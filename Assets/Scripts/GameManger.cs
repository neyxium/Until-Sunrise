using UnityEngine;
using System;

public class GameManger : MonoBehaviour
{
    public static GameManger Instance;
    [NonSerialized] public float nightLength = 60f;
    [NonSerialized] public float nightRemaining;
    [NonSerialized] public float elapseNight;
    private GameObject player;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>().gameObject;
        nightRemaining = nightLength;
    }

    // Update is called once per frame
    void Update()
    {
        nightRemaining -= Time.deltaTime;
        elapseNight = 1f - Mathf.Clamp01(nightRemaining / nightLength);
    }
}
