using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Player Variables
    private const float _speed = 7.5f;
    private const float _maxWallSlideSpeed = 2f;
    private const float _jumpPower = 12.5f;
    private const float _airLerpSpeed = 7.5f; // za smoothanje player movementa v zraku
    private const float _fallMutliplier = 2.5f;
    private float defaultGravity;
    private Vector2 wallJumpPower = new Vector2(_jumpPower / 2, _jumpPower);
    private Rigidbody2D rb;
    private float horizontalInput;

    // Player States
    bool playerGrounded = false;
    private bool playerWallSliding = false;

    // Layers
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    // Raycasting
    const float _rayCastDist = 0.55f;
    private Vector2 boxSize = new Vector2(0.9f, 0.1f);

    // Jump Buffer
    private const float _jumpBufferTime = 0.1f;
    private float jumpBufferCounter;

    // Coyote Jump
    private const float _coyoteBufferTime = 0.075f;
    private float coyoteTimeCounter;

    // Wall Jump
    private const float _wallJumpDuration = 0.1f;
    private float wallJumpTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale *= _fallMutliplier;
    }

    void Update()
    {
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
    }

    private bool IsTouchingGround()
    {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.down, _rayCastDist, groundLayer);
        return hit.collider != null;
    }

    private bool IsTouchingWall()
    {
        return WallDirection() != 0f;
    }

    private float WallDirection()
    {
        if (Physics2D.Raycast(transform.position, Vector2.right, _rayCastDist, wallLayer).collider != null)
            return 1f;

        if (Physics2D.Raycast(transform.position, Vector2.left, _rayCastDist, wallLayer).collider != null)
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

    void FixedUpdate()
    {
        if (rb != null)
        {   
            if (wallJumpTimer > 0f) return;

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

    // Vizualno prikazovanje hitboxa v editorju
    private void OnDrawGizmos()
    {
        Gizmos.color = playerGrounded ? Color.green : Color.red;
        Vector3 boxPosition = transform.position + Vector3.down * _rayCastDist;
        Gizmos.DrawWireCube(boxPosition, boxSize);
    }
}
