using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator animator;
    public ContactFilter2D castFilter;

    public float moveSpeed = 4;
    public float jumpSpeed = 3;

    public float timeAir = -1;

    public bool canMove = true;

    public float wallDistance = 0.02f;

    public float groundDistance = 0.05f;

    //Raycast for wall stuff
    RaycastHit2D[] wallHits = new RaycastHit2D[5];
    RaycastHit2D[] groundHits = new RaycastHit2D[5];
    private Vector2 wallCheckDirection => gameObject.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

    BoxCollider2D bc;

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
        if (canMove)
        {
            rb.velocity = new Vector2(moveSpeed * moveInput, rb.velocity.y);
        }
        
        // On landing from a fall, check to use land animation or not.
        if (!isAirborne && bc.Cast(Vector2.down, castFilter, groundHits, groundDistance) == 0)
        {
            // AIRBORNE
            timeAir = Time.time;
            Debug.Log("Airborne Started");
        }
        else if (isAirborne && bc.Cast(Vector2.down, castFilter, groundHits, groundDistance) > 0)
        {
            // LAND
            Debug.Log("Airborne Ended");
            Debug.Log("Time Airborne: " + (Time.time - timeAir).ToString());

            // If airborne for more than 2 seconds, play landing animation and halt movement for a little bit.
            if (Time.time - timeAir > 2.0) 
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

        // Jump
        if (Input.GetKey(KeyCode.Space) && !isAirborne && canMove)
        {
            animator.SetTrigger("Jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        }

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
