using UnityEngine;

public class WorldPainter : MonoBehaviour
{
    public CubeWorld world;
    public float brushSize = 0.4f;

    void Update()
    {
        if (world == null) 
        {
            world = Object.FindAnyObjectByType<CubeWorld>();
            if (world == null) return;
        }

        if (world.isSimulationMode) return;

        // Visual debug for the raycast
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 30f, Color.red);

        if (Input.GetMouseButton(0))
        {
            world.TryRemoveVoxel(ray, brushSize);
        }
    }
}