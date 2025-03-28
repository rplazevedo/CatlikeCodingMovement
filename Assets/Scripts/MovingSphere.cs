using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField]
    Transform playerInputSpace = default;

    Vector3 velocity, desiredVelocity;
    Vector3 contactNormal, steepNormal;
    Vector3 upAxis, rightAxis, forwardAxis;
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
        body.useGravity = false;
        OnValidate();
    }
    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        if (playerInputSpace)
        {
            rightAxis = ProjectOnDirectionPlane(playerInputSpace.right, upAxis); ;
            forwardAxis = ProjectOnDirectionPlane(playerInputSpace.forward, upAxis); ;
        }
        else
        {
            rightAxis = ProjectOnDirectionPlane(Vector3.right, upAxis); ;
            forwardAxis = ProjectOnDirectionPlane(Vector3.forward, upAxis); ;
        }
        desiredVelocity = new Vector3(playerInput.x, 0.0f, playerInput.y) * maxSpeed;

        desiredJump |= Input.GetButtonDown("Jump");

        if (Input.GetMouseButtonDown(1)){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void FixedUpdate()
    {
        Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        UpdateState();
        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;
            Jump(gravity);
        }

        velocity += gravity * Time.deltaTime;

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
        Vector3 xAxis = ProjectOnDirectionPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectOnDirectionPlane(forwardAxis, contactNormal);

        float currentX = Vector3.Dot(velocity, xAxis);  
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        
        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    Vector3 ProjectOnDirectionPlane (Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    private void UpdateState()
    {
        stepsSinceLastGrounded++;
        stepsSinceLastJump++;
        velocity = body.velocity;
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = upAxis;
        }
    }

    void Jump(Vector3 gravity)
    {
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else 
        {
            return; 
        }

        jumpPhase++;
        stepsSinceLastJump = 0;

        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);

        if (jumpPerperdicularToGround)
        {
            jumpDirection = (jumpDirection + upAxis).normalized;
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
            float upDot = Vector3.Dot(upAxis, normal);
            if (upDot >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (upDot > -0.01f)
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
            float upDot = Vector3.Dot(upAxis, steepNormal);
            if (upDot >= minGroundDotProduct)
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
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, snapProbeDistance, probeMask))
        {
            return false;
        }
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer))
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
