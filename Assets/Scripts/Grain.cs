using UnityEngine;

public class Grain : MonoBehaviour
{
    void Start()
    {
        // Randomize size slightly
        transform.localScale = Vector3.one * Random.Range(0.12f, 0.25f);
        
        // Ensure this grain is on Layer 2 (optional, but good for keeping it separate)
        gameObject.layer = 2;
    }

    void Update()
    {
        // Kill if fallen too far
        if (transform.position.y < -15) Destroy(gameObject);
    }
}
