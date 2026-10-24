using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private const float _dashSpeed = 20f;      // hitrost med dashem
    private const float _dashDuration = 0.15f; // koliko časa traja dash (20 * 0.15 = 3 enote)
    private const float _dashDelay = 2.5f;     // cooldown med dashi
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isDashing = false;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale *= _fallMutliplier;
        defaultGravity = rb.gravityScale;

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

        if(playerHP <= 0f)
        {
            Restart();
        }

        UpdateAnimations();
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

    private void PlayerInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
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
        // Dash v smer, v katero lik gleda
        float dashDir = Mathf.Sign(transform.localScale.x);

        isDashing = true;
        dashTimer = _dashDuration;
        dashCooldownTimer = _dashDelay;
        rb.linearVelocity = new Vector2(dashDir * _dashSpeed, 3f);
        UpdateAnimations();
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
            Debug.Log("win");
        }
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

    // En vhod za vse zadetke: damage + knockback, ampak največ enkrat na _invincibilityDuration
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
    private void OnDrawGizmos()
    {
        Gizmos.color = playerGrounded ? Color.green : Color.red;

        Vector3 boxPosition = transform.position + (Vector3)boxOffset + Vector3.down * _rayCastDist;

        Gizmos.DrawWireCube(boxPosition, boxSize);
    }
}
