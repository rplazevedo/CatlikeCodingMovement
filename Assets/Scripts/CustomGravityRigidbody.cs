using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    [SerializeField]
    bool floatToSleep = false;

    private Rigidbody body;
    private Material material;
    private float floatDelay;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        material = GetComponent<MeshRenderer>().material;
        body.useGravity = false;    
    }

    private void FixedUpdate()
    {
        if (floatToSleep)
        {
            if (body.IsSleeping())
            {
                floatDelay = 0f;
                return;
            }

            if (body.velocity.sqrMagnitude < 0.0001f)
            {
                floatDelay += Time.deltaTime;
                if (floatDelay >= 1f)
                {
                    return;
                }
            }
            else
            {
                floatDelay = 0f;
            }
        }
        body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration);
    }

    private void Update()
    {
        if (material != null)
        {
            material.SetFloat("_FloatDelay", floatDelay);
            material.SetInt("_IsSleeping", body.IsSleeping() ? 1 : 0); // Pass sleeping state to shader
        }
    }
}
