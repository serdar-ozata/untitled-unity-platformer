using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Level_Component.Hittable;
using UnityEngine;

// ReSharper disable All

public class PlayerController : MonoBehaviour, IHittable {
    public Rigidbody2D rb;

    // TEMP
    private Vector3 _startPos;

    public PolygonCollider2D boundary;

    // END TEMP
    public Transform groundPoint1;
    public Transform groundPoint2;
    public Transform sidePoint;
    public LayerMask groundLayer;
    public LayerMask swingLayer;
    public LayerMask swordLayer;
    public Animator anim;

    public BoxCollider2D bodyCollider;

    [Space] public float fallGravityMultiplier = 0.2f;

    [Space] [Header("Swing")] public float energyLossMultiplier;

    [Space] [Header("Attack Debug")] public bool showAttackCheckBox = false;

    // private variables
    private float _gravityScale;
    private float _oldGravityScale;

    private float _jumpTimer;
    private float _countdownFall;

    private bool _changedDirection;

    private bool _jumpButtonReleased; // to jump higher if the user holds the button
    private bool _jumped; // to prevent the fall glitch before actual jump on edge
    private ContactFilter2D _hitFilter;
    public PlayerConditions Conditions;


    [NonSerialized] public float maxSpeed;

    // Start is called before the first frame update
    void Start() {
        anim.SetBool("Dead", false);
        anim.SetBool("GotHit", false);
        _gravityScale = rb.gravityScale;
        _oldGravityScale = _gravityScale;
        Conditions = new PlayerConditions();
        Conditions.Reset();
        // hit check
        _hitFilter = new ContactFilter2D();
        _hitFilter.layerMask = LayerMask.NameToLayer("Entity");
        _startPos = transform.position;
    }

    // Update is called once per frame
    void Update() {
        // animations initiate
        anim.SetBool("OnAir", !(Conditions.OnGround || Conditions.OnStuckSword || Conditions.IsClingingWall));
        anim.SetBool("IsDashing", Conditions.IsDashing);
        anim.SetBool("ClingToWall", Conditions.IsClingingWall);
        anim.SetBool("Grounded", Conditions.OnGround);

        if (Conditions.IsClimbing) { // climb
            anim.SetInteger("PressVerticalVal", 2);
        }
        else if (!Conditions.IsClimbing && Conditions.IsClingingWall) {
            if (!Conditions.IsSliding) { // cling
                anim.SetInteger("PressVerticalVal", 1);
            }
            else { // slide
                anim.SetInteger("PressVerticalVal", 0);
            }
        }
        else {
            anim.SetInteger("PressVerticalVal", -30);
        }
    }


    public void ChangeDirection(float relativeSpeed) {
        float speed = rb.velocity.x - relativeSpeed;
        if (speed < -0.1f) {
            Conditions.IsFacingRight = false;
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (speed > 0.1f) {
            Conditions.IsFacingRight = true;
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void FixedUpdate() {
        // multiply gravity when falling
        if (!(Conditions.IsClingingWall || Conditions.IsDashing)) {
            if (rb.velocity.y < 0)
                rb.gravityScale = _gravityScale * fallGravityMultiplier;
            else
                rb.gravityScale = _gravityScale;
            if (Conditions.IsSwinging || Conditions.IsExposedToFan) {
                rb.velocity *= new Vector2(1f, (1 - Time.deltaTime) * energyLossMultiplier);
            }
        }
    }


    public void MoveHorizontal(float targetSpeed, float velPower, float acceleration, float deceleration,
        float airAcceleration, float maxSpeedAccelerationMultiplier, float relativeSpeed = 0f) {
        //movement sideways
        float velocityX = rb.velocity.x - relativeSpeed;
        float speedDif = targetSpeed - velocityX;
        float accelRate;
        if (Mathf.Abs(targetSpeed) > 0.01f) {
            accelRate = Conditions.OnGround ? acceleration : airAcceleration;
            // if (Mathf.Abs(rb.velocity.x) > maxSpeed && maxSpeed - Mathf.Abs(targetSpeed) > 0.01f) {
            //     accelRate *= maxSpeedAccelerationMultiplier;
            //     Debug.Log("trigger");
            // }
        }
        else {
            accelRate = deceleration;
        }

        float sign = Mathf.Sign(speedDif);
        if (velocityX * targetSpeed > Mathf.Epsilon && Mathf.Abs(targetSpeed) < Mathf.Abs(velocityX)) {
            accelRate *= maxSpeedAccelerationMultiplier;
        }

        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * sign;
        rb.AddForce(Vector2.right * movement * Time.deltaTime);
    }

    public void JumpHold(float jumpHoldMultiplier) {
        if (rb.velocity.y > 0) {
            rb.AddForce(new Vector2(0f, _gravityScale * jumpHoldMultiplier * Time.deltaTime));
        }
    }

    public void Jump(float jumpForce) {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }


    #region Swing

    public Vector2 FindSwingPoint(float maxSwingAngle, int halfSwingVecCount, float swingVecLength, float rayDuration,
        bool showSwingRays) {
        // the initial start angle of the vector, depends on player's direction
        float startAngle = Conditions.IsFacingRight ? Mathf.PI / 4f : -Mathf.PI / 4f;
        // top point of the player
        Bounds bounds = bodyCollider.bounds;
        Vector2 topPoint = new Vector3(bounds.center.x, bounds.max.y);

        // Ray checks
        float angleIncrement = maxSwingAngle / halfSwingVecCount;
        RaycastHit2D hit = Physics2D.Raycast(topPoint, new Vector2(Mathf.Tan(startAngle), 1f),
            swingVecLength, swingLayer);
        if (showSwingRays) {
            Debug.DrawRay(topPoint,
                new Vector2(Mathf.Sin(startAngle), Mathf.Cos(startAngle)) *
                swingVecLength, Color.green, rayDuration);
        }

        if (Mathf.Abs(hit.point.magnitude) > 0.0001f) return hit.point;

        for (int i = 1; i <= halfSwingVecCount; i++) {
            if (showSwingRays) {
                float variation = i * angleIncrement * Mathf.PI / 180f;
                Debug.DrawRay(topPoint,
                    new Vector2(Mathf.Sin(startAngle + variation), Mathf.Cos(startAngle + variation)) *
                    swingVecLength, Color.green, rayDuration);
                Debug.DrawRay(topPoint,
                    new Vector2(Mathf.Sin(startAngle - variation), Mathf.Cos(startAngle - variation)) *
                    swingVecLength, Color.green, rayDuration);
            }

            hit = Physics2D.Raycast(topPoint,
                new Vector2(Mathf.Tan(startAngle + i * angleIncrement * Mathf.PI / 180f), 1f),
                swingVecLength, swingLayer);
            if (Mathf.Abs(hit.point.magnitude) > 0.0001f)
                return hit.point;
            hit = Physics2D.Raycast(topPoint,
                new Vector2(Mathf.Tan(startAngle - i * angleIncrement * Mathf.PI / 180f), 1f),
                swingVecLength, swingLayer);
            if (Mathf.Abs(hit.point.magnitude) > 0.0001f)
                return hit.point;
        }

        return new Vector2(0f, 0f);
    }

    public void Swing(Vector2 swingPoint, float scarfLength, float scarfForceMultiplier, bool showSwingRay,
        float earlyForceLength, float horizontalForceMagnitude, float horizontalVelocityLimit) {
        Vector2 playerPosition = transform.position;
        Vector2 ray = swingPoint - playerPosition;
        if (showSwingRay) {
            Debug.DrawLine(transform.position, swingPoint, Color.red);
            Debug.DrawRay(transform.position, Vector2.Perpendicular(ray).normalized * 5f, Color.blue);
        }

        // we need a better equation, something better than F = SFM * (EFL + Δx))
        float forceMagnitude = (earlyForceLength + Mathf.Pow(ray.magnitude - scarfLength, 3)) * scarfForceMultiplier;
        if (forceMagnitude > 0f) {
            rb.AddForce(forceMagnitude * ray.normalized * Time.deltaTime);
        }

        if (horizontalVelocityLimit > rb.velocity.x) {
            rb.AddForce(-horizontalForceMagnitude * Vector2.Perpendicular(ray).normalized * Time.deltaTime);
        }
    }

    public float CalculateScarfLength(Vector2 swingPoint, float minStiffnessAngle, float stiffnessMultiplier,
        float minLoosenessAngle, float loosenessMultiplier, float minScarfLength) {
        Vector2 playerPosition = transform.position;
        Vector2 ray = swingPoint - playerPosition;
        // normal ray - perpendicular() rotates clock wise. That's why I'm inversing the vector if player is facing right
        // otherwise ray would be opposite to velocity
        Vector2 normalRay = Conditions.IsFacingRight ? -Vector2.Perpendicular(ray) : Vector2.Perpendicular(ray);
        float length = ray.magnitude;
        float angle = Vector2.SignedAngle(rb.velocity, normalRay);
        if (angle > minStiffnessAngle) {
            length -= Mathf.Sqrt(angle) * stiffnessMultiplier;
        }
        else if (angle < -minLoosenessAngle) {
            length += Mathf.Sqrt(-angle) * loosenessMultiplier;
        }

        return length > minScarfLength ? length : minScarfLength;
    }

    #endregion

    public void SetGravity(float clingGravity) {
        rb.gravityScale = clingGravity;
        _gravityScale = clingGravity;
    }

    public void StopMovement() {
        rb.velocity = Vector2.zero;
    }

    public void ReduceMovement(float amount) {
        rb.velocity = new Vector2(GetVelocity().x * amount, GetVelocity().y);
    }

    public void RevertGravity() {
        rb.gravityScale = _oldGravityScale;
        _gravityScale = _oldGravityScale;
    }

    public void SlideWall(float slideDownSpeed) {
        rb.velocity = new Vector2(rb.velocity.x, slideDownSpeed);
    }


    public void VectorJump(Vector2 ray) {
        rb.AddForce(ray, ForceMode2D.Impulse);
    }

    public void ForceHorizontal(float dashForce) {
        rb.AddForce(Vector2.right * dashForce, ForceMode2D.Impulse);
    }

    public Vector2 GetVelocity() {
        return rb.velocity;
    }

    public void MultiplyMovement(float oppositeSpeedLimiter, float speedLimiter, Vector2 direction) {
        // dot product determines whether direction and velocity are opposite or not
        float dot = Vector2.Dot(rb.velocity, direction);
        //Debug.Log("Before: " + rb.velocity);
        if (dot > 0) { // reduce speed
            rb.velocity -= (rb.velocity * direction * speedLimiter);
        }
        else if (dot < 0) { // revert and reduce speed
            rb.velocity -= (rb.velocity * direction * oppositeSpeedLimiter);
        }
        //Debug.Log("After: " + rb.velocity);
    }

    public void MoveByVelocity(Vector2 velocity) {
        rb.velocity += velocity;
    }

    public void AddVelocity(Vector2 velocity) {
        rb.velocity += velocity;
    }

    public void MoveByTranslation(Vector2 direction) {
        transform.Translate(direction, Space.Self);
    }

    public void SetSpeedAnimation(Vector2 relativeSpeed) {
        anim.SetFloat("SpeedX", Mathf.Abs(rb.velocity.x - relativeSpeed.x));
        anim.SetFloat("SpeedY", rb.velocity.y - relativeSpeed.y);
    }

    public void ApplyForce(Vector2 force) {
        rb.AddForce(force);
    }


    public int Hit(Vector2 hitFrom) {
        // TODO kill the player put animation etc
        transform.position = _startPos;
        rb.velocity = Vector2.zero;
        return 0;
    }


    #region collision check

    public bool IsOnSword() {
        return Physics2D.OverlapCircle(groundPoint1.position, 0.2f, swordLayer) &&
               Physics2D.OverlapCircle(groundPoint2.position, 0.2f, swordLayer);
    }

    [NotNull]
    public List<Collider2D> CheckBoxCollision(Vector2 pos, Vector2 size) {
        List<Collider2D> cols = new List<Collider2D>();
        if (showAttackCheckBox) {
            Debug.DrawLine(new(pos.x - size.x, pos.y), new(pos.x + size.x, pos.y), Color.green, 3f);
            Debug.DrawLine(new(pos.x, pos.y - size.y), new(pos.x, pos.y + size.y), Color.green, 3f);
        }

        Physics2D.OverlapBox(pos, size, 0f, _hitFilter, cols);
        return cols;
    }

    public bool isOnGround() {
        return Physics2D.OverlapCircle(groundPoint1.position, 0.2f, groundLayer) &&
               Physics2D.OverlapCircle(groundPoint2.position, 0.2f, groundLayer);
    }

    public bool isClinging() {
        var col = Physics2D.OverlapCircle(sidePoint.position, 0.2f, groundLayer);
        return col is not null && !col.CompareTag("NonClimbable");
    }

    #endregion

    #region friction

    public void GroundFriction(float frictionAmount, float relativeSpeed) {
        float amount = Mathf.Min(Mathf.Abs(rb.velocity.x - relativeSpeed), Mathf.Abs(frictionAmount));
        amount *= Mathf.Sign(rb.velocity.x - relativeSpeed);
        rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
    }

    public void AirFriction(float k) {
        rb.velocity *= new Vector2(1f, k * (1f - Time.deltaTime));
    }

    #endregion
}