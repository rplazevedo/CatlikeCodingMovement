using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;    
    }

    private void FixedUpdate()
    {
        body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration);
    }
}
