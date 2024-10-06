using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    // COMPONENTS
    Rigidbody2D rb;
    Animator animator;
    ContactFilter2D castFilter;
    BoxCollider2D bc;

    // BASIC MOVEMENT
    public float moveSpeed = 4;
    public float jumpSpeed = 3;
    public float lateralJumpSpeed = 4;
    public float wallSpeed = 5;
    public bool canMove = true;

    public bool jumpOnCooldown = false;
    public float jumpCooldown = 1f;
    public float wallJumpTime = 0.5f;

    // DASH
    public float dashImpulse = 25f;
    public bool dashOnCooldown = false;
    public float dashCooldown = 2f;
    
    public Vector2 dashStartPos = new Vector2(0, 0);
    public float dashRange = 5f;
    public float dashTime = 2f;
    

    // AIRTIME AND LANDING
    private float timeAir = -1;
    public float landTimer = 2.5f;

    // RAYCAST
    public float wallDistance = 0.02f;
    public float groundDistance = 0.05f;
    RaycastHit2D[] wallHits = new RaycastHit2D[5];
    RaycastHit2D[] groundHits = new RaycastHit2D[5];
    private Vector2 wallCheckDirection => gameObject.transform.localScale.x > 0 ? Vector2.right : Vector2.left;


    private bool _isDashing = false;
    public bool isDashing {
        set
        {
            animator.SetBool("isDashing", value);
            bc.isTrigger = value;
            _isDashing = value;
        }
        
        get
        {
            return _isDashing;
        }
    }

    private bool _isAirborne = false;
    public bool isAirborne {
        set
        {
            animator.SetBool("isAirborne", value);
            _isAirborne = value;
        }

        get
        {
            return _isAirborne;
        }
    }

    private bool _isRising = false;
    public bool isRising {
        set
        {
            animator.SetBool("isRising", value);
            _isRising = value;
        }

        get
        {
            return _isRising;
        }
    }

    private bool _facingLeft = false;
    public bool facingLeft {
        set
        {
            if (value != _facingLeft)
            {
                gameObject.transform.localScale *= new Vector2(-1, 1);
            }

            _facingLeft = value;
        }

        get 
        { 
            return _facingLeft; 
        }
    }

    private bool _isMoving = false;
    public bool isMoving {
        set
        {
            animator.SetBool("isMoving", value);
            _isMoving = value;
        }

        get
        { 
            return _isMoving;
        }
    }

    private bool _isWalled = false;
    public bool isWalled {
        set
        {
            animator.SetBool("isWalled", value);
            rb.gravityScale = value ? 0 : 1;
            _isWalled = value;
        }

        get
        { 
            return _isWalled;
        }
    }

    public AnimationClip GetAnimationClip(string animationName)
    {
        // Retrieves animation clip data, useful for halting movement for duration of anim among other things.
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == animationName)
            {
                return clips[i];
            }
        }

        return null;
    }

    public IEnumerator StopMovementForSeconds(float seconds)
    {
        canMove = false;
        yield return new WaitForSeconds(seconds);
        canMove = true;
    }

    public IEnumerator StartDashCooldown()
    {
        dashOnCooldown = true;
        yield return new WaitForSeconds(dashCooldown);
        dashOnCooldown = false;
    }

    public IEnumerator StartJumpCooldown()
    {
        jumpOnCooldown = true;
        yield return new WaitForSeconds(jumpCooldown);
        jumpOnCooldown = false;
    }

    public IEnumerator StartDash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bc = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        // Basic Movement
        float moveInput = Input.GetAxisRaw("Horizontal");
        float horizontalMoveInput = Input.GetAxisRaw("Vertical");
        bool wantsToMove = Input.GetAxis("Horizontal") != 0;
        bool wantsToDash = Input.GetKey(KeyCode.LeftShift);
        bool wantsToJump = Input.GetKey(KeyCode.Space);

        if (canMove && !wantsToDash && !wantsToJump || canMove && wantsToDash && dashOnCooldown && !isDashing) 
        {
            if(wantsToMove) 
            { 
                rb.velocity = new Vector2(moveSpeed * moveInput, rb.velocity.y);
            }
            else if (isWalled) 
            {
                rb.velocity = new Vector2(0, horizontalMoveInput < 0 ? horizontalMoveInput * wallSpeed : 0);
            }
        }
        else if (canMove && wantsToDash && !dashOnCooldown && !isDashing)
        {
            StartCoroutine(StopMovementForSeconds(dashTime));
            StartCoroutine(StartDashCooldown());
            StartCoroutine(StartDash());
            dashStartPos = new Vector2(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y);

        } 
        else if (wantsToJump && !isAirborne && canMove && !isWalled && !isDashing && !jumpOnCooldown)
        {
            animator.SetTrigger("Jump");
            StartCoroutine(StartJumpCooldown());
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        } 
        else if (wantsToJump && isWalled && canMove && !isDashing && isAirborne) 
        {   
            rb.velocity = new Vector2(lateralJumpSpeed * -wallCheckDirection.x, jumpSpeed);
            if (wallCheckDirection.x > 0) facingLeft = true; else facingLeft = false;
            StartCoroutine(StopMovementForSeconds(0.5f));
        }
        else if (isDashing)
        {
            // We want to end the dash early if we reach the maximum dash range or we hit a wall.
            if (Math.Abs(dashStartPos.x - gameObject.transform.localPosition.x) >= dashRange)
            {
                rb.velocity = new Vector2(0, 0);
                isDashing = false;
                canMove = true;
            } else {
                 rb.velocity = new Vector2(dashImpulse * wallCheckDirection.x, 0);
            }
           
        }
        
        // On landing from a fall, check to use land animation or not.
        if (!isAirborne && bc.Cast(Vector2.down, castFilter, groundHits, groundDistance) == 0)
        {
            // AIRBORNE
            timeAir = Time.time;
        }
        else if (isAirborne && bc.Cast(Vector2.down, castFilter, groundHits, groundDistance) > 0 && !isWalled)
        {
            // LAND

            // If airborne for more than landTimer seconds, play landing animation and halt movement for a little bit.
            if (Time.time - timeAir > landTimer) 
            {
                animator.SetTrigger("Land");
                rb.velocity *= new Vector2(0, 1);
                StartCoroutine(StopMovementForSeconds(GetAnimationClip("player_land").length - 0.2f));
            }

            timeAir = -1;
        }
        else if (isWalled)
        {
            // Reset "land timer" while player is walled.
            timeAir = Time.time;
        }

        // Set bools with character velocity
        isAirborne = bc.Cast(Vector2.down, castFilter, groundHits, groundDistance) == 0;
        isRising = rb.velocity.y > 0;        


        // Set isMoving Bool
        if (moveInput != 0)
        {
            isMoving = true;
            if (moveInput < 0)
            {
                facingLeft = true;
            } else {
                facingLeft = false;
            }
        } 
        else 
        {
            isMoving = false;
        }
    }

    void FixedUpdate()
    {
        isWalled = bc.Cast(wallCheckDirection, castFilter, wallHits, wallDistance) > 0;
    }
}
