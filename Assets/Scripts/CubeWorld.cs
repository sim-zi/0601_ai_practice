using UnityEngine;
using System.Collections.Generic;

public class CubeWorld : MonoBehaviour
{
    public enum ViewDirection { Front, Back, Left, Right, Top, Bottom }

    [System.Serializable]
    public class FaceData
    {
        public ViewDirection direction;
        public Vector3 cameraPosition;
        public Vector3 cameraUp;
    }

    [Header("Settings")]
    public Vector3 cubeDimensions = new Vector3(15f, 10f, 5f);
    public Vector3Int gridResolution = new Vector3Int(30, 20, 10);
    public float transitionSpeed = 10f;

    [Header("State")]
    public ViewDirection currentView = ViewDirection.Front;
    public bool isSimulationMode = false;

    [Header("Voxel References")]
    public GameObject voxelPrefab;
    private GameObject voxelRoot;

    [Header("Physics Grains")]
    public GameObject grainPrefab;
    public Transform grainSpawnPoint;
    public float grainSpawnRate = 0.05f;
    private float nextSpawnTime;
    private List<GameObject> activeGrains = new List<GameObject>();

    void Start()
    {
        isSimulationMode = false;
        FullSetup();
    }

    public void FullSetup()
    {
        SetupVoxels();
        UpdateVisibility();
    }

    public void SetupVoxels()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == "VoxelRoot")
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        voxelRoot = new GameObject("VoxelRoot");
        voxelRoot.transform.SetParent(this.transform, false);

        Vector3 voxelSize = new Vector3(
            cubeDimensions.x / gridResolution.x,
            cubeDimensions.y / gridResolution.y,
            cubeDimensions.z / gridResolution.z
        );
        
        Vector3 offset = cubeDimensions / 2f - voxelSize / 2f;

        for (int x = 0; x < gridResolution.x; x++)
        {
            for (int y = 0; y < gridResolution.y; y++)
            {
                for (int z = 0; z < gridResolution.z; z++)
                {
                    Vector3 pos = new Vector3(x * voxelSize.x, y * voxelSize.y, z * voxelSize.z) - offset;
                    GameObject v = Instantiate(voxelPrefab, voxelRoot.transform);
                    v.name = "Voxel"; 
                    v.transform.localPosition = pos;
                    v.transform.localScale = voxelSize * 0.99f;
                }
            }
        }
    }

    public void TryRemoveVoxel(Ray ray, float brushSize)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Collider[] hits = Physics.OverlapSphere(hit.point, brushSize);
            foreach (var h in hits)
            {
                GameObject go = h.gameObject;
                if (go.name == "Voxel" || (go.transform.parent != null && go.transform.parent.name == "VoxelRoot"))
                {
                    go.SetActive(false);
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
        }
    }

    void Update()
    {
        HandleInput();
        UpdateCamera();
        HandleGrainEmission();
    }

    void HandleGrainEmission()
    {
        if (isSimulationMode && grainPrefab != null && grainSpawnPoint != null)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnGrain();
                nextSpawnTime = Time.time + grainSpawnRate;
            }
        }
    }

    void SpawnGrain()
    {
        Vector3 pos = grainSpawnPoint.position + Random.insideUnitSphere * 0.7f;
        GameObject grain = Instantiate(grainPrefab, pos, Quaternion.identity);
        activeGrains.Add(grain);
        if (activeGrains.Count > 1000)
        {
            GameObject oldest = activeGrains[0];
            activeGrains.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSimulationMode = !isSimulationMode;
            UpdateVisibility();
        }

        if (!isSimulationMode)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Navigate(Vector2.up);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Navigate(Vector2.down);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Navigate(Vector2.left);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Navigate(Vector2.right);
        }
    }

    void Navigate(Vector2 dir)
    {
        if (currentView == ViewDirection.Front) {
            if (dir == Vector2.up) currentView = ViewDirection.Top;
            else if (dir == Vector2.down) currentView = ViewDirection.Bottom;
            else if (dir == Vector2.left) currentView = ViewDirection.Left;
            else if (dir == Vector2.right) currentView = ViewDirection.Right;
        } else if (currentView == ViewDirection.Back) {
            if (dir == Vector2.up) currentView = ViewDirection.Top;
            else if (dir == Vector2.down) currentView = ViewDirection.Bottom;
            else if (dir == Vector2.left) currentView = ViewDirection.Right;
            else if (dir == Vector2.right) currentView = ViewDirection.Left;
        } else if (currentView == ViewDirection.Top) {
            if (dir == Vector2.up) currentView = ViewDirection.Back;
            else if (dir == Vector2.down) currentView = ViewDirection.Front;
            else if (dir == Vector2.left) currentView = ViewDirection.Left;
            else if (dir == Vector2.right) currentView = ViewDirection.Right;
        } else if (currentView == ViewDirection.Bottom) {
            if (dir == Vector2.up) currentView = ViewDirection.Front;
            else if (dir == Vector2.down) currentView = ViewDirection.Back;
            else if (dir == Vector2.left) currentView = ViewDirection.Left;
            else if (dir == Vector2.right) currentView = ViewDirection.Right;
        } else if (currentView == ViewDirection.Left) {
            if (dir == Vector2.up) currentView = ViewDirection.Top;
            else if (dir == Vector2.down) currentView = ViewDirection.Bottom;
            else if (dir == Vector2.left) currentView = ViewDirection.Back;
            else if (dir == Vector2.right) currentView = ViewDirection.Front;
        } else if (currentView == ViewDirection.Right) {
            if (dir == Vector2.up) currentView = ViewDirection.Top;
            else if (dir == Vector2.down) currentView = ViewDirection.Bottom;
            else if (dir == Vector2.left) currentView = ViewDirection.Front;
            else if (dir == Vector2.right) currentView = ViewDirection.Back;
        }
    }

    public void UpdateVisibility()
    {
        if (!isSimulationMode)
        {
            foreach (var g in activeGrains) if (g != null) Destroy(g);
            activeGrains.Clear();
        }
    }

    void UpdateCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (isSimulationMode)
        {
            // Position camera further back and higher for a cinematic view of the large structure
            Vector3 targetPos = new Vector3(cubeDimensions.x * 0.8f, cubeDimensions.y * 0.8f, -cubeDimensions.z * 1.5f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position + targetPos, Time.deltaTime * transitionSpeed);
            cam.transform.LookAt(transform.position);
        }
        else
        {
            FaceData data = GetActiveFace();
            if (data != null) {
                cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position + data.cameraPosition, Time.deltaTime * transitionSpeed);
                cam.transform.LookAt(transform.position, data.cameraUp);
            }
        }
    }

    public FaceData GetActiveFace() 
    {
        FaceData data = new FaceData { direction = currentView };
        // Increase margin based on dimensions to ensure the whole face is visible
        float margin = Mathf.Max(cubeDimensions.x, cubeDimensions.y, cubeDimensions.z) * 0.8f + 5f;
        
        switch (currentView)
        {
            case ViewDirection.Front: data.cameraPosition = new Vector3(0, 0, -cubeDimensions.z / 2f - margin); data.cameraUp = Vector3.up; break;
            case ViewDirection.Back: data.cameraPosition = new Vector3(0, 0, cubeDimensions.z / 2f + margin); data.cameraUp = Vector3.up; break;
            case ViewDirection.Left: data.cameraPosition = new Vector3(-cubeDimensions.x / 2f - margin, 0, 0); data.cameraUp = Vector3.up; break;
            case ViewDirection.Right: data.cameraPosition = new Vector3(cubeDimensions.x / 2f + margin, 0, 0); data.cameraUp = Vector3.up; break;
            case ViewDirection.Top: data.cameraPosition = new Vector3(0, cubeDimensions.y / 2f + margin, 0); data.cameraUp = Vector3.forward; break;
            case ViewDirection.Bottom: data.cameraPosition = new Vector3(0, -cubeDimensions.y / 2f - margin, 0); data.cameraUp = Vector3.back; break;
        }
        return data;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        string mode = isSimulationMode ? "PLAY MODE (Simulation)" : "EDIT MODE (Sculpting) - " + currentView;
        GUI.Label(new Rect(20, 20, 600, 30), $"MODE: {mode}", style);
        GUI.Label(new Rect(20, 50, 600, 30), "SPACE: Toggle | WASD/Arrows: Navigate | MOUSE: Draw", style);
    }
}