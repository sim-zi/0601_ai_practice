using UnityEngine;

public class WorldPainter : MonoBehaviour
{
    public CubeWorld world;
    public Shader brushShader;
    public float brushSize = 0.05f;
    public float brushStrength = 0.05f;

    private Material brushMaterial;
    private RenderTexture tempRT;

    void Start()
    {
        if (brushShader != null)
            brushMaterial = new Material(brushShader);
        
        tempRT = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
    }

    void Update()
    {
        if (world.isSculptMode) return;

        if (Input.GetMouseButton(0))
        {
            Paint();
        }
    }

    void Paint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            CubeWorld.FaceData face = world.GetActiveFace();
            // Check if we hit the plane corresponding to the active face
            if (hit.collider.gameObject.name.Contains(face.direction.ToString()))
            {
                Vector2 uv = hit.textureCoord;
                DrawAt(uv, face);
            }
        }
    }

    void DrawAt(Vector2 uv, CubeWorld.FaceData face)
    {
        if (brushMaterial == null || face.paintTexture == null) return;

        brushMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
        brushMaterial.SetFloat("_BrushSize", brushSize);
        brushMaterial.SetFloat("_BrushStrength", brushStrength);
        brushMaterial.SetColor("_BrushColor", face.brushColor);

        // Blit to apply additive painting
        Graphics.Blit(face.paintTexture, tempRT);
        Graphics.Blit(tempRT, face.paintTexture, brushMaterial);
    }
}
