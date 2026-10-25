using UnityEngine;
using TMPro;

public class UI_Manager : MonoBehaviour
{
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI dashText;
    private PlayerMovement player;

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();

        if (dashText == null && clockText != null)
        {
            Transform existing = clockText.transform.parent.Find("DashStatus");
            if (existing != null)
            {
                dashText = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject dashObj = Instantiate(clockText.gameObject, clockText.transform.parent);
                dashObj.name = "DashStatus";
                RectTransform rt = dashObj.GetComponent<RectTransform>();
                rt.anchoredPosition = clockText.rectTransform.anchoredPosition + new Vector2(0f, -60f);
                dashText = dashObj.GetComponent<TextMeshProUGUI>();
                dashText.fontSize = 40f;
            }
        }
    }

    void Update()
    {
        if (GameManger.Instance.bFinished) return; // neha izvajanje cikla ce player prek konca
        
        if (GameManger.Instance != null && clockText != null)
        {
            float totalMinutes = GameManger.Instance.elapseNight * 480f;

            int snappedMinutes = Mathf.Clamp(Mathf.FloorToInt(totalMinutes / 15f) * 15, 0, 480);

            int hours = snappedMinutes / 60;
            int minutes = snappedMinutes % 60;

            clockText.SetText("{0}:{1:00} AM", hours, minutes);
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>();
        }

        if (player != null && dashText != null)
        {
            if (player.IsDashReady)
            {
                dashText.SetText("Dash: READY");
                dashText.color = new Color(0.4f, 1f, 0.4f, 1f);
            }
            else
            {
                dashText.SetText("Dash: {0:0.0}s", player.DashCooldownRemaining);
                dashText.color = new Color(1f, 0.6f, 0.2f, 1f);
            }
        }
    }
}
