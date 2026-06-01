using UnityEngine;
using System.Collections.Generic;

public class CubeWorld : MonoBehaviour
{
    public enum ViewDirection { Front, Back, Left, Right, Top, Bottom }

    [System.Serializable]
    public class FaceData
    {
        public ViewDirection direction;
        public RenderTexture paintTexture;
        public Color brushColor;
        public Vector3 cameraPosition;
        public Vector3 cameraUp;
    }

    [Header("Settings")]
    public float cubeSize = 5f;
    public int textureResolution = 1024;
    public float transitionSpeed = 10f;

    [Header("State")]
    public ViewDirection currentView = ViewDirection.Front;
    public bool isSculptMode = false;

    [Header("References")]
    public Material shellMaterial; // Used for the 6 paintable planes
    public Material volumeMaterial; // Used for the raymarched volume

    private Dictionary<ViewDirection, FaceData> faces = new Dictionary<ViewDirection, FaceData>();
    private GameObject shellRoot;
    private GameObject volumeRoot;

    void Start()
    {
        InitializeFaces();
        SetupShell();
        SetupVolume();
        UpdateVisibility();
    }

    void Update()
    {
        HandleInput();
        UpdateCamera();
    }

    void InitializeFaces()
    {
        faces[ViewDirection.Front] = new FaceData { direction = ViewDirection.Front, brushColor = Color.red, cameraPosition = new Vector3(0, 0, -8), cameraUp = Vector3.up };
        faces[ViewDirection.Back] = new FaceData { direction = ViewDirection.Back, brushColor = Color.red, cameraPosition = new Vector3(0, 0, 8), cameraUp = Vector3.up };
        faces[ViewDirection.Left] = new FaceData { direction = ViewDirection.Left, brushColor = Color.green, cameraPosition = new Vector3(-8, 0, 0), cameraUp = Vector3.up };
        faces[ViewDirection.Right] = new FaceData { direction = ViewDirection.Right, brushColor = Color.green, cameraPosition = new Vector3(8, 0, 0), cameraUp = Vector3.up };
        faces[ViewDirection.Top] = new FaceData { direction = ViewDirection.Top, brushColor = Color.blue, cameraPosition = new Vector3(0, 8, 0), cameraUp = Vector3.forward };
        faces[ViewDirection.Bottom] = new FaceData { direction = ViewDirection.Bottom, brushColor = Color.blue, cameraPosition = new Vector3(0, -8, 0), cameraUp = Vector3.back };

        foreach (var face in faces.Values)
        {
            face.paintTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
            face.paintTexture.Create();
            ClearRT(face.paintTexture);
        }
    }

    void SetupShell()
    {
        shellRoot = new GameObject("Shell");
        shellRoot.transform.SetParent(this.transform, false);

        // Primitive Quads face -Z by default.
        // Front: View from (0,0,-8). Plane at (0,0,-2.5). Face -Z.
        CreatePlane(ViewDirection.Front, new Vector3(0, 0, -cubeSize/2), Quaternion.identity);
        // Back: View from (0,0,8). Plane at (0,0,2.5). Face +Z.
        CreatePlane(ViewDirection.Back, new Vector3(0, 0, cubeSize/2), Quaternion.Euler(0, 180, 0));
        // Left: View from (-8,0,0). Plane at (-2.5,0,0). Face -X.
        CreatePlane(ViewDirection.Left, new Vector3(-cubeSize/2, 0, 0), Quaternion.Euler(0, 90, 0));
        // Right: View from (8,0,0). Plane at (2.5,0,0). Face +X.
        CreatePlane(ViewDirection.Right, new Vector3(cubeSize/2, 0, 0), Quaternion.Euler(0, -90, 0));
        // Top: View from (0,8,0). Plane at (0,2.5,0). Face +Y.
        CreatePlane(ViewDirection.Top, new Vector3(0, cubeSize/2, 0), Quaternion.Euler(90, 0, 0));
        // Bottom: View from (0,-8,0). Plane at (0,-2.5,0). Face -Y.
        CreatePlane(ViewDirection.Bottom, new Vector3(0, -cubeSize/2, 0), Quaternion.Euler(-90, 0, 0));
    }

    void CreatePlane(ViewDirection dir, Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Plane_" + dir;
        go.transform.SetParent(shellRoot.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * cubeSize;

        Material mat = new Material(shellMaterial);
        mat.mainTexture = faces[dir].paintTexture;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void SetupVolume()
    {
        volumeRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        volumeRoot.name = "VolumeSculpt";
        volumeRoot.transform.SetParent(this.transform, false);
        volumeRoot.transform.localScale = Vector3.one * (cubeSize + 0.1f);
        
        Material mat = new Material(volumeMaterial);
        mat.SetTexture("_FrontTex", faces[ViewDirection.Front].paintTexture);
        mat.SetTexture("_BackTex", faces[ViewDirection.Back].paintTexture);
        mat.SetTexture("_LeftTex", faces[ViewDirection.Left].paintTexture);
        mat.SetTexture("_RightTex", faces[ViewDirection.Right].paintTexture);
        mat.SetTexture("_TopTex", faces[ViewDirection.Top].paintTexture);
        mat.SetTexture("_BottomTex", faces[ViewDirection.Bottom].paintTexture);
        
        volumeRoot.GetComponent<MeshRenderer>().sharedMaterial = mat;
        volumeRoot.GetComponent<BoxCollider>().enabled = false;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSculptMode = !isSculptMode;
            UpdateVisibility();
        }

        if (!isSculptMode)
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

    void UpdateVisibility()
    {
        shellRoot.SetActive(!isSculptMode);
        volumeRoot.SetActive(isSculptMode);
    }

    void UpdateCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (isSculptMode)
        {
            Vector3 targetPos = new Vector3(8, 8, -8);
            cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position + targetPos, Time.deltaTime * transitionSpeed);
            cam.transform.LookAt(transform.position);
        }
        else
        {
            FaceData data = faces[currentView];
            cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position + data.cameraPosition, Time.deltaTime * transitionSpeed);
            cam.transform.LookAt(transform.position, data.cameraUp);
        }
    }

    public FaceData GetActiveFace() => faces[currentView];

    void ClearRT(RenderTexture rt) {
        RenderTexture.active = rt; GL.Clear(true, true, Color.black); RenderTexture.active = null;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        string mode = isSculptMode ? "SCULPTED VOLUME" : "PAINT MODE (" + currentView + ")";
        GUI.Label(new Rect(20, 20, 600, 30), $"MODE: {mode}", style);
        GUI.Label(new Rect(20, 50, 600, 30), "SPACE: Toggle | WASD/Arrows: Navigate | MOUSE: Draw", style);
    }
}
