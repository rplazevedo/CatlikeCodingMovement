using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)] float maxAcceleration = 10f;
    Vector3 velocity;
    private void Update()
    {
        Vector2 playerInput;
        //Vector3 velocity;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        Vector3 desiredVelocity = new Vector3(playerInput.x, 0.0f, playerInput.y) * maxSpeed;
        float maxSpeedChange = maxAcceleration * Time.deltaTime;
        if (velocity.x < desiredVelocity.x)
        {
            velocity.x = Mathf.Min(velocity.x + maxSpeedChange, desiredVelocity.x);
        }
        else if (velocity.x > desiredVelocity.x)
            {
                velocity.x = Mathf.Min(velocity.x - maxSpeedChange, desiredVelocity.x);
            }

        Vector3 displacement = velocity * Time.deltaTime;
        transform.localPosition += displacement;
    }
}
