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

    public BoxCollider2D fd;

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
    public Vector2 dashDirection = Vector2.left;

    public Vector2 dashStartPos = new Vector2(0, 0);
    public float dashRange = 5f;
    public float dashTime = 2f;
    

    // AIRTIME AND LANDING
    public float timeAir = -1;
    public float totalTimeAirborne = -1;
    public float landTimer = 1f;

    // RAYCAST
    public float wallDistance = 0.75f;
    public float wallBoxCastDistance = 0.02f;
    public float groundDistance = 3f;
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

            if (isAirborne != value)
            {
                if (value == true)
                {
                    // Entering Airborne State
                    timeAir = Time.time;
                } 
                else 
                {
                    // Exiting Airborne State

                    // Perform additional wall check to verify landing is valid
                    totalTimeAirborne = Time.time - timeAir;
                    Debug.Log(totalTimeAirborne);
                    if(totalTimeAirborne >= landTimer)
                    {
                        if (!isWalled) 
                        {
                            animator.SetTrigger("Land");
                            rb.velocity *= new Vector2(0, 1);
                            StartCoroutine(StopMovementForSeconds(GetAnimationClip("player_land").length - 0.2f));
                        }
                    }
                }   
            }

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
            if (_isWalled != value)
            {
                if (value) rb.velocity *= new Vector2(1, 0);
                 _isWalled = value;
            }
            if (value) timeAir = Time.time;
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
        bool wantsToDash = Input.GetKeyDown(KeyCode.LeftShift);
        bool wantsToJump = Input.GetKeyDown(KeyCode.Space);

        // Movement Types
        if (canMove && !wantsToDash && !wantsToJump || canMove && wantsToDash && dashOnCooldown && !isDashing) 
        {
            // Basic Movement controlled by player input
            if(wantsToMove) 
            { 
                // Normal Move

                if (bc.Cast(wallCheckDirection, wallHits, wallBoxCastDistance) > 0){
                    // Special box cast to determine if next to a wall, but not low enought to qualify as walled
                    if(moveInput > 0 && wallCheckDirection.x > 0 || moveInput < 0 && wallCheckDirection.x < 0)
                    {
                        // Don't allow player to move into wall to suspend themself
                        moveInput = 0;
                    }
                }

                rb.velocity = new Vector2(moveSpeed * moveInput, rb.velocity.y);
            }
            else if (isWalled) 
            {
                // Downward Wall Slide
                rb.velocity = new Vector2(0, horizontalMoveInput < 0 ? horizontalMoveInput * wallSpeed : 0);
            }
        }
        else if (canMove && wantsToDash && !dashOnCooldown && !isDashing)
        {
            // Start Dash
            StartCoroutine(StopMovementForSeconds(dashTime));
            StartCoroutine(StartDashCooldown());
            StartCoroutine(StartDash());
            dashDirection = wallCheckDirection;
            dashStartPos = new Vector2(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y);

        } 
        else if (wantsToJump && !isAirborne && canMove && !isDashing && !jumpOnCooldown)
        {
            // Jump
            animator.SetTrigger("Jump");
            StartCoroutine(StartJumpCooldown());
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        } 
        else if (wantsToJump && isWalled && canMove && !isDashing && isAirborne) 
        {   
            // Wall Jump
            rb.velocity = new Vector2(lateralJumpSpeed * -wallCheckDirection.x, jumpSpeed);
            if (wallCheckDirection.x > 0) facingLeft = true; else facingLeft = false;
            StartCoroutine(StopMovementForSeconds(0.5f));
        }
        else if (isDashing)
        {
            // During Dash, move at dashImpulse velocity until dash range is reached
            if (Math.Abs(dashStartPos.x - gameObject.transform.localPosition.x) >= dashRange)
            {
                rb.velocity = new Vector2(0, 0);
                isDashing = false;
                canMove = true;
            } else {
                 rb.velocity = new Vector2(dashImpulse * dashDirection.x, 0);
            }
           
        }    

        // Is Moving / Facing Direction Check
        if(canMove)
        {
            if (moveInput != 0)
            {
                isMoving = true;
                // Only check for facing direction on player move
                facingLeft = moveInput < 0;
            } 
            else 
            {
                isMoving = false;
            }
        }
        
    }

    void FixedUpdate()
    {
        isAirborne = fd.Cast(Vector2.down, castFilter, groundHits, groundDistance, true) == 0;
        isWalled = fd.Cast(wallCheckDirection, castFilter, wallHits, wallDistance, true) > 0;
        isRising = rb.velocity.y > 0;
    }
}
