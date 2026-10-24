using Unity.VisualScripting;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    // Basic Enemy Stuff
    private const float _moveSpeed = 3f;
    private const float _patrolDistance = 4f;
    private const float _jumpForce = 10f;
    private const float _minJumpDelay = 4f;
    private const float _maxJumpDelay = 7f;

    // Dash
    private const float _dashPower = 10f;
    private const float _dashDuration = 0.25f;
    private const float _dashCooldown = 2f;

    private float dashTimer;
    private float cooldownTimer;
    private bool isDashing = false;

    private Rigidbody2D rb;
    private float jumpTimer;
    private float startX;
    private int movingDirection = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        startX = transform.position.x;
        ResetJumpTimer();




    }

    void Update()
    {
        jumpTimer -= Time.deltaTime;
        cooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
            return;
        }

        if (jumpTimer <= 0f)
        {
            EnemyJump();
            ResetJumpTimer();
        }

        CheckPatrolBounds();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            if (isDashing) return;
            rb.linearVelocity = new Vector2(movingDirection * _moveSpeed, rb.linearVelocity.y);
        }
    }

    private void CheckPatrolBounds()
    {
        if (movingDirection == 1 && transform.position.x >= startX + _patrolDistance)
        {
            movingDirection = -1;
            FlipSprite();
        }
        else if (movingDirection == -1 && transform.position.x <= startX - _patrolDistance)
        {
            movingDirection = 1;
            FlipSprite();
        }
    }

    private void FlipSprite()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * movingDirection;
        transform.localScale = scale;
    }

    private void EnemyJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, _jumpForce);
    }

    private void ResetJumpTimer()
    {
        jumpTimer = Random.Range(_minJumpDelay, _maxJumpDelay);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player") && cooldownTimer <= 0f)
        {
            Dash(other.collider.transform);
        }
    }

    private void Dash(Transform target)
    {
        isDashing = true;
        dashTimer = _dashDuration;
        cooldownTimer = _dashCooldown;

        float direction = Mathf.Sign(target.position.x - transform.position.x);

        movingDirection = (int)direction;
        FlipSprite();

        // Ohranimo trenutno vertikalno HITROST (prej je bila tu pomotoma pozicija Y)
        rb.linearVelocity = new Vector2(direction * _dashPower, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? new Vector3(startX, transform.position.y, transform.position.z) : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center + Vector3.left * _patrolDistance, center + Vector3.right * _patrolDistance);
        Gizmos.DrawWireSphere(center + Vector3.left * _patrolDistance, 0.2f);
        Gizmos.DrawWireSphere(center + Vector3.right * _patrolDistance, 0.2f);
    }
}
