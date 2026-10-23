using UnityEngine;
using TMPro;

public class UI_Manager : MonoBehaviour
{
    public TextMeshProUGUI clockText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManger.Instance != null && clockText != null)
        {
            float totalMinutes = GameManger.Instance.elapseNight * 480f;

            int snappedMinutes = Mathf.Clamp(Mathf.FloorToInt(totalMinutes / 15f) * 15, 0, 480);

            int hours = snappedMinutes / 60;
            int minutes = snappedMinutes % 60;

            clockText.SetText("{0}:{1:00} AM", hours, minutes);
        }
    }
}
