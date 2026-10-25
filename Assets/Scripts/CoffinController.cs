using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CoffinController : MonoBehaviour
{
    [Header("Animation & Timing")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector3 playerOffset = Vector3.zero;
    [SerializeField] private float coffinCloseDuration = 3.0f;
    [SerializeField] private string triggerParameterName = "OpenAndClose";
    [SerializeField] private string animationStateName = "Open&Close";

    [Header("Cinematic Camera Zoom")]
    [SerializeField] private bool enableCinematicZoom = true;
    [SerializeField] private float zoomStartDelay = 1.0f;
    [SerializeField] private float targetZoomSize = 3.5f;

    [Header("Win Settings")]
    [SerializeField] private bool logWin = true;

    private bool isTriggered = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            TryEnterCoffin(collision.collider.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            TryEnterCoffin(collider.gameObject);
        }
    }

    public void TryEnterCoffin(GameObject playerGO)
    {
        if (isTriggered) return;
        isTriggered = true;

        PlayerMovement playerMovement = playerGO.GetComponent<PlayerMovement>();
        StartCoroutine(CoffinSequence(playerGO, playerMovement));
    }

    private IEnumerator CoffinSequence(GameObject playerGO, PlayerMovement playerMovement)
    {
        Vector3 targetPos = (spriteRenderer != null ? spriteRenderer.bounds.center : transform.position) + playerOffset;

        if (playerMovement != null)
        {
            playerMovement.EnterCoffin(targetPos);
        }
        else
        {
            playerGO.transform.position = new Vector3(targetPos.x, targetPos.y, playerGO.transform.position.z);
            if (playerGO.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
        }

        var playerRenderers = playerGO.GetComponentsInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            foreach (var r in playerRenderers)
            {
                r.sortingOrder = spriteRenderer.sortingOrder + 1;
            }
        }

        if (animator != null)
        {
            if (HasParameter(animator, triggerParameterName))
            {
                animator.SetTrigger(triggerParameterName);
            }
            else
            {
                animator.Play(animationStateName, 0, 0f);
            }
        }

        if (enableCinematicZoom)
        {
            float zoomDuration = Mathf.Max(0.1f, coffinCloseDuration - zoomStartDelay);
            StartCoroutine(CinematicCameraZoom(zoomStartDelay, zoomDuration));
        }


        yield return new WaitForSeconds(coffinCloseDuration);


        if (playerMovement != null)
        {
            playerMovement.HidePlayerSprite();
        }
        else
        {
            foreach (var r in playerRenderers)
            {
                r.enabled = false;
            }
        }

        if (logWin)
        {
            Debug.Log("GG EZ");
        }

        if (playerMovement != null)
        {
            playerMovement.SetRestartGameCoRout();
        }
    }

    private IEnumerator CinematicCameraZoom(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindAnyObjectByType<Camera>();
        }

        CinemachineCamera vcam = FindAnyObjectByType<CinemachineCamera>();

        float startSize = 5f;
        if (vcam != null)
        {
            startSize = vcam.Lens.OrthographicSize;
            vcam.Target.TrackingTarget = transform;
        }
        else if (mainCam != null)
        {
            startSize = mainCam.orthographicSize;
        }

        Vector3 camStartPos = mainCam != null ? mainCam.transform.position : Vector3.zero;
        Vector3 camTargetPos = new Vector3(transform.position.x, transform.position.y, camStartPos.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentSize = Mathf.Lerp(startSize, targetZoomSize, smoothT);

            if (vcam != null)
            {
                var lens = vcam.Lens;
                lens.OrthographicSize = currentSize;
                vcam.Lens = lens;
            }

            if (mainCam != null)
            {
                mainCam.orthographicSize = currentSize;
                if (vcam == null)
                {
                    mainCam.transform.position = Vector3.Lerp(camStartPos, camTargetPos, smoothT);
                }
            }

            yield return null;
        }

        if (vcam != null)
        {
            var lens = vcam.Lens;
            lens.OrthographicSize = targetZoomSize;
            vcam.Lens = lens;
        }
        if (mainCam != null)
        {
            mainCam.orthographicSize = targetZoomSize;
        }
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}
