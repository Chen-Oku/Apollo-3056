using UnityEngine;

public class Mover : MonoBehaviour
{
    public float speed;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.linearVelocity = transform.forward * speed;
    }

}
