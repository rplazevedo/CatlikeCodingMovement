using System;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAceleration = 1f;
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;
    [SerializeField, Min(0f)]
    float snapProbeDistance = 1f;
    [SerializeField, Range(0f, 10f)] 
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 1;
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f, maxStairsAngle = 50f;
    [SerializeField]
    bool jumpPerperdicularToGround = true;
    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    Vector3 velocity, desiredVelocity;
    Vector3 contactNormal, steepNormal;
    Rigidbody body;
    bool desiredJump;
    int groundContactCount, steepContactCount;
    int stepsSinceLastGrounded, stepsSinceLastJump;
    bool OnGround => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0; 
    int jumpPhase;
    float minGroundDotProduct, minStairsDotProduct;

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    private float GetMinDot(int layer)
    {
        return (stairsMask & ( 1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
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
            "_Color", OnGround ? Color.black : Color.white 
            );

        if (Input.GetMouseButtonDown(0)){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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
        groundContactCount = steepContactCount =  0;
        contactNormal = steepNormal = Vector3.zero;
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
        stepsSinceLastGrounded++;
        stepsSinceLastJump++;
        velocity = body.velocity;
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
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
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
        }
        else if (jumpPhase < maxAirJumps)
        {
            jumpDirection = contactNormal;
        }
        else 
        {
            return; 
        }

        jumpPhase++;
        stepsSinceLastJump = 0;

        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

        if (jumpPerperdicularToGround)
        {
            float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
            if (alignedSpeed > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            velocity += jumpDirection * jumpSpeed;
        }
        else
        {
            if (velocity.y > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            velocity.y += jumpSpeed;
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
        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {
                steepContactCount += 1; 
                steepNormal += normal;
            }
        }
    }

    private bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    bool SnapToGround ()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, snapProbeDistance, probeMask))
        {
            return false;
        }
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, contactNormal);
        if (dot > 0f)
        {
            velocity= (velocity - contactNormal * dot).normalized * speed;
        }
        return true;
    }
}
