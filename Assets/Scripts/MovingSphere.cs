using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] 
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAceleration = 1f;
    [SerializeField, Range(0f, 10f)] 
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 1;
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;
    [SerializeField]
    bool jumpPerperdicularToGround = true;

    Vector3 velocity, desiredVelocity, contactNormal;
    Rigidbody body;
    bool desiredJump;
    int groundContactCount;
    bool OnGround => groundContactCount > 0;
    int jumpPhase;
    float minGroundDotProduct;

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }
    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        desiredVelocity = new Vector3(playerInput.x, 0.0f, playerInput.y) * maxSpeed;

        desiredJump |= Input.GetButtonDown("Jump");

        GetComponent<Renderer>().material.SetColor(
            "_Color", Color.white * (groundContactCount * 0.25f)
            );
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.velocity = velocity;

        ClearState();
    }

    private void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    void AdjustVelocity ()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);  
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        
        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    Vector3 ProjectOnContactPlane (Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    private void UpdateState()
    {
        velocity = body.velocity;
        if (OnGround)
        {
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase++;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            if (jumpPerperdicularToGround)
            {
                float alignedSpeed = Vector3.Dot(velocity, contactNormal);
                if (alignedSpeed > 0f)
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
                velocity += contactNormal * jumpSpeed;
            }
            else
            {
                if (velocity.y > 0f)
                    jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
                velocity.y += jumpSpeed;
            }
        }   
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateColission(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateColission(collision);
    }

    private void EvaluateColission(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }
}
