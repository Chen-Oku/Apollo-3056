using UnityEngine;

public class RandomRotation : MonoBehaviour
{

    private Rigidbody rb;
    public float tumble;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Generar una dirección aleatoria normalizada y aplicarle una magnitud aleatoria
        Vector3 angularVelocity = Random.insideUnitSphere * tumble;
        rb.angularVelocity = angularVelocity;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
