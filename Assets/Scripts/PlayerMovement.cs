using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    // Player Variables
    private const float _speed = 7.5f;
    private const float _maxWallSlideSpeed = 2f;
    private const float _jumpPower = 12.5f;
    private const float _airLerpSpeed = 7.5f; // za smoothanje player movementa v zraku
    private const float _fallMutliplier = 2.5f;
    private const float _maxHP = 100f;
    public float playerHP = _maxHP;
    private float defaultGravity;
    private Vector2 wallJumpPower = new Vector2(_jumpPower / 1.25f, _jumpPower);
    private Rigidbody2D rb;
    private Animator anim;
    private float horizontalInput;
    [SerializeField] private Slider healthSlider;

    // Player States
    bool playerGrounded = false;
    private bool playerWallSliding = false;

    // Layers
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask climbableLayers;

    // Raycasting
    const float _rayCastDist = 0.55f;
    private Vector2 boxSize = new Vector2(0.475f, 0.1f);
    private Vector2 boxOffset = new Vector2(0.11f, 0f);

    // Dash
    private const float _dashSpeed = 20f;
    private const float _dashDuration = 0.15f;
    private const float _dashDelay = 2.5f;
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isDashing = false;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownTimer);
    public bool IsDashReady => dashCooldownTimer <= 0f && !isDashing;

    // Jump Buffer
    private const float _jumpBufferTime = 0.1f;
    private float jumpBufferCounter;

    // Coyote Jump
    private const float _coyoteBufferTime = 0.075f;
    private float coyoteTimeCounter;

    // Wall Jump
    private const float _wallJumpDuration = 0.1f;
    private float wallJumpTimer;

    // Knockback
    private Vector2 knockbackPower = new Vector2(15f, 10f);
    private const float _knockbackDuration = 0.2f;
    private float knockbackTimer;

    // Cinemachine Camera
    [Header("Cinemachine Settings")]
    [SerializeField] private bool autoSetupCinemachine = true;
    [SerializeField] private Vector3 cameraDamping = new Vector3(0.5f, 0.7f, 0f);
    [SerializeField] private float lookaheadTime = 0.15f;
    [SerializeField] private float lookaheadSmoothing = 4f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float orthographicSize = 5f;
    
    // menu gltich
    [SerializeField] private GameObject menu;
    private Vector3 camWorldTransform;
    private bool bIsMenuActive = false;
    [SerializeField] private GameObject playerCamera;
    
    //vcam
    private CinemachineCamera vcam;
    
    // ending
    [SerializeField] private float fWaitTimeBeforeGameReset = 3f;

    private void Awake()
    {
        if (autoSetupCinemachine)
        {
            SetupCinemachine();
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale *= _fallMutliplier;
        defaultGravity = rb.gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (healthSlider == null)
        {
            healthSlider = FindAnyObjectByType<Slider>();
        }

        UpdateHealthUI();
    }

    void Update()
    {
        knockbackTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        if (transform.position.y <= -10)
        {
            Restart();
        }

        playerGrounded = IsTouchingGround();

        horizontalInput = 0f;

        PlayerInput();

        if (playerGrounded)
        {
            coyoteTimeCounter = _coyoteBufferTime;
            playerWallSliding = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        playerWallSliding = !playerGrounded && IsTouchingWall() && rb.linearVelocity.y < 0f;

        wallJumpTimer -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f && rb.linearVelocity.y <= 0.01f)
                DoJump();
            else if (!playerGrounded && IsTouchingWall())
                DoWallJump();
        }

        if (rb == null)
        {
            transform.Translate(new Vector3(horizontalInput * _speed * Time.deltaTime, 0f, 0f));
        }

        if (GameManger.Instance.elapseNight > 0.75f)
        {
            DamagePlayer(Time.deltaTime * (0.5f + GameManger.Instance.elapseNight) * 5);
        }

        if (playerHP <= 0f)
        {
            Restart();
        }

        UpdateAnimations();

        if (playerCamera != null && bIsMenuActive)
        {
            playerCamera.transform.position = camWorldTransform;
        } 
    }

    private void UpdateAnimations()
    {
        if (anim == null || rb == null) return;

        bool isWalking = Mathf.Abs(horizontalInput) > 0.1f && playerGrounded && !isDashing;
        bool isJumping = !playerGrounded;

        anim.SetBool("isWalking", isWalking);
        anim.SetBool("isJumping", isJumping);
        anim.SetBool("isDashing", isDashing);
    }

    private bool IsTouchingGround()
    {
        Vector2 origin = (Vector2)transform.position + boxOffset;
        RaycastHit2D hit = Physics2D.BoxCast(origin, boxSize, 0f, Vector2.down, _rayCastDist, groundLayers);
        return hit.collider != null;
    }

    private bool IsTouchingWall()
    {
        return WallDirection() != 0f;
    }

    private float WallDirection()
    {
        if (Physics2D.Raycast(transform.position, Vector2.right, _rayCastDist, climbableLayers).collider != null)
            return 1f;

        if (Physics2D.Raycast(transform.position, Vector2.left, _rayCastDist, climbableLayers).collider != null)
            return -1f;

        return 0f;
    }

    private bool IsPlayerInMiddleOfScreen()
    {
        Camera cam = playerCamera.GetComponent<Camera>();
        if (cam != null)
        {
            float viewportX = cam.WorldToViewportPoint(transform.position).x;
            return viewportX <= 0.58f && viewportX >= 0.42f;
        }

        return false;
    }

    private void SetPlayerPosBasedOnScreen(float finalX)
    {
        Camera cam = playerCamera.GetComponent<Camera>();
        
        if (cam != null)
        {
            float worldX = cam.ViewportToWorldPoint(new Vector2(finalX, 0)).x;
            transform.position = new Vector3(worldX, transform.position.y, transform.position.z);
        }
    }

    private void PlayerInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;

            if (bIsMenuActive && IsPlayerInMiddleOfScreen())
            {
                Debug.Log("IsInMiddleOfScreen");
                SetPlayerPosBasedOnScreen(0.4f);
            }
        }
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
            
            if (bIsMenuActive && IsPlayerInMiddleOfScreen())
            {
                Debug.Log("IsInMiddleOfScreen");
                SetPlayerPosBasedOnScreen(0.6f);
            }
        }

        if (keyboard.spaceKey.isPressed)
        {
            jumpBufferCounter = _jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        if (horizontalInput > 0f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (horizontalInput < 0f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // wasPressedThisFrame = samo enkrat ob pritisku (ne vsak frame, ko držiš tipko)
        if (keyboard.shiftKey.wasPressedThisFrame && dashCooldownTimer <= 0f && !isDashing)
        {
            DoDash();
        }

        if (keyboard.rKey.isPressed)
        {
            Restart();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // dobisi

            if (menu.activeInHierarchy)
            {
                menu.SetActive(false);
                bIsMenuActive = false;
                vcam.Target.TrackingTarget = transform;
            }
            else
            {
                menu.SetActive(true);
                bIsMenuActive = true;
                camWorldTransform = playerCamera.transform.position;
                vcam.Target.TrackingTarget = null;
            }
        }
    }

    private void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocityX, _jumpPower);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void DoWallJump()
    {
        float wallDir = WallDirection();
        if (wallDir == 0f) return;

        rb.linearVelocity = new Vector2(-wallDir * wallJumpPower.x, wallJumpPower.y);

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        wallJumpTimer = _wallJumpDuration;
    }

    private void DoDash()
    {
        float dashDir = Mathf.Sign(transform.localScale.x);

        //rotate player 90 degrress in that direction and then rotate back
        transform.Rotate(0f, 90f * dashDir, 0f);
        Invoke("ResetRotation", 0.1f);

        isDashing = true;
        dashTimer = _dashDuration;
        dashCooldownTimer = _dashDelay;
        rb.linearVelocity = new Vector2(dashDir * _dashSpeed, 3f);
        UpdateAnimations();
    }

    private void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
    }



    void FixedUpdate()
    {
        if (rb != null)
        {
            if (isDashing || wallJumpTimer > 0f || knockbackTimer > 0f) return;

            float yVelocity = rb.linearVelocity.y;

            if (playerWallSliding)
            {
                yVelocity = Mathf.Max(yVelocity, -_maxWallSlideSpeed);
            }

            float targetX = horizontalInput * _speed;

            float xVelocity = playerGrounded ? targetX : Mathf.Lerp(rb.linearVelocity.x, targetX, _airLerpSpeed * Time.deltaTime);

            rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Enemy"))
        {
            TakeHit(other.transform.position, 10f);
        }
        else if (other.collider.CompareTag("Finish"))
        {
            HandleFinishTrigger(other.gameObject);
        }
        else if (other.collider.CompareTag("Death"))
        {
            Restart();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Finish"))
        {
            HandleFinishTrigger(other.gameObject);
        }
    }

    private void HandleFinishTrigger(GameObject finishObj)
    {
        GameManger.Instance.bFinished = true;
        
        Debug.Log("win");
        CoffinController coffin = finishObj.GetComponent<CoffinController>();
        if (coffin == null)
        {
            coffin = finishObj.AddComponent<CoffinController>();
        }
        coffin.TryEnterCoffin(gameObject);
    }

    public void EnterCoffin(Vector3 coffinPosition)
    {
        isDashing = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        transform.position = new Vector3(coffinPosition.x, coffinPosition.y, transform.position.z);
        transform.localScale = Vector3.one;

        if (anim != null)
        {
            anim.Play("PlayerIdle");
            anim.SetBool("isWalking", false);
            anim.SetBool("isJumping", false);
            anim.SetBool("isDashing", false);
        }

        Light2D light2D = GetComponentInChildren<Light2D>();
        if (light2D != null)
        {
            light2D.enabled = false;
        }

        enabled = false;
    }

    public void HidePlayerSprite()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.enabled = false;
        }
    }

    public void SetRestartGameCoRout()
    {
        StartCoroutine(RestartGameCo());
    }

    IEnumerator RestartGameCo()
    {
        yield return new WaitForSeconds(fWaitTimeBeforeGameReset);
        Restart();
    }

    public void DamagePlayer(float damage = 1f)
    {
        playerHP = Mathf.Clamp(playerHP - damage, 0, _maxHP);
        UpdateHealthUI();

        if (playerHP <= 0)
        {
            Debug.Log("ded");
        }
    }

    public void TakeHit(Vector2 sourcePosition, float damage)
    {
        DamagePlayer(damage);
        DoKnockBack(sourcePosition);
    }

    private void DoKnockBack(Vector2 sourcePosition)
    {
        float knockbackDir = Mathf.Sign(transform.position.x - sourcePosition.x);

        rb.linearVelocity = new Vector2(knockbackDir * knockbackPower.x, knockbackPower.y);
        knockbackTimer = _knockbackDuration;
    }


    public void HealPlayer(float healAmount = 1f)
    {
        playerHP = Mathf.Clamp(playerHP + healAmount, 0, _maxHP);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = _maxHP;
            healthSlider.value = playerHP;
        }
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void SetupCinemachine()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindAnyObjectByType<Camera>();
        }

        if (mainCam != null)
        {
            if (mainCam.transform.IsChildOf(transform))
            {
                mainCam.transform.SetParent(null);
                mainCam.transform.localScale = Vector3.one;
                mainCam.transform.rotation = Quaternion.identity;
            }

            if (!mainCam.TryGetComponent<CinemachineBrain>(out var brain))
            {
                brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam == null)
        {
            GameObject camGO = new GameObject("Cinemachine2DCamera");
            camGO.transform.position = new Vector3(transform.position.x, transform.position.y, -cameraDistance);
            vcam = camGO.AddComponent<CinemachineCamera>();
        }

        vcam.Target.TrackingTarget = transform;

        if (mainCam != null)
        {
            var lens = LensSettings.FromCamera(mainCam);
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = orthographicSize > 0f ? orthographicSize : (mainCam.orthographicSize > 0f ? mainCam.orthographicSize : 5f);
            vcam.Lens = lens;
        }

        if (!vcam.TryGetComponent<CinemachinePositionComposer>(out var composer))
        {
            composer = vcam.gameObject.AddComponent<CinemachinePositionComposer>();
        }

        composer.CameraDistance = cameraDistance;
        composer.Damping = cameraDamping;
        composer.Lookahead.Enabled = lookaheadTime > 0f;
        composer.Lookahead.Time = lookaheadTime;
        composer.Lookahead.Smoothing = lookaheadSmoothing;
        composer.Lookahead.IgnoreY = true;
        composer.Composition = ScreenComposerSettings.Default;
        composer.CenterOnActivate = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = playerGrounded ? Color.green : Color.red;

        Vector3 boxPosition = transform.position + (Vector3)boxOffset + Vector3.down * _rayCastDist;

        Gizmos.DrawWireCube(boxPosition, boxSize);
    }
}
