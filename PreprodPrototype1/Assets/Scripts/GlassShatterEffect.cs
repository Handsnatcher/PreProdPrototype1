using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GlassShatterEffect : MonoBehaviour
{
    [Header("Shards")]
    public Material glassShardMaterial;
    public float planeDistance = 0.8f;
    public float shardDepth = 0.003f;
    public int cols = 5;
    public int rows = 4;
    [Range(0f, 0.4f)]
    public float jitter = 0.25f;

    [Header("Shatter Timing")]
    public float crackDuration = 0.4f;
    public float fallStagger = 1.0f;
    public bool edgesFirst = true;

    [Header("Physics")]
    public float fallForce = 1.0f;
    public float torqueStrength = 6f;
    public float shardLifetime = 2f;
    public float fadeDuration = 0.5f;

    [Header("Rendering")]
    public int shardLayer = 3; // assign your Shards layer number here

    [Header("Camera")]
    public Camera targetCamera;

    [Header("Debug")]
    public bool debugKey = true;

    private List<GameObject> activeShards = new List<GameObject>();
    public GameObject shardRoot;
    private Texture2D capturedScreen;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            Debug.LogWarning("GlassShatterEffect: Assign a camera.");
    }

    void Update()
    {
        if (debugKey && Input.GetKeyDown(KeyCode.G))
            TriggerShatter();
    }

    public void TriggerShatter()
    {
        if (targetCamera == null) return;
        ClearShards();
        StartCoroutine(CaptureAndShatter());
    }

    IEnumerator CaptureAndShatter()
    {
        // Wait for the frame to fully render before capturing
        yield return new WaitForEndOfFrame();

        // Capture the screen into a texture
        capturedScreen = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        capturedScreen.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        capturedScreen.Apply();

        yield return StartCoroutine(ShatterSequence());
    }

    IEnumerator ShatterSequence()
    {
        var shards = SpawnShards();

        // Keep the entire effect alive through scene transition
        DontDestroyOnLoad(this.gameObject);
        if (targetCamera != null)
            DontDestroyOnLoad(targetCamera.gameObject);

        yield return new WaitForSeconds(crackDuration);

        if (edgesFirst)
            shards.Sort((a, b) => b.dist.CompareTo(a.dist));
        else
            Shuffle(shards);

        float delay = shards.Count > 0 ? fallStagger / shards.Count : 0f;

        foreach (var s in shards)
        {
            if (s.go == null) continue;
            Rigidbody rb = s.go.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 outward = s.center.magnitude > 0.001f
                ? new Vector3(s.center.x, s.center.y, 0).normalized
                : new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;

            rb.AddForce(outward * fallForce * Random.Range(0.6f, 1.4f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueStrength, ForceMode.Impulse);

            StartCoroutine(FadeAndDestroyShard(s.go, shardLifetime, fadeDuration));
            yield return new WaitForSeconds(delay);
        }
    }

    List<ShardData> SpawnShards()
    {
        float halfH = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * planeDistance;
        float halfW = halfH * targetCamera.aspect;

        shardRoot = new GameObject("ShardRoot");
        shardRoot.transform.SetParent(targetCamera.transform, false);
        shardRoot.transform.localPosition = Vector3.forward * planeDistance;
        shardRoot.transform.localRotation = Quaternion.identity;

        float cellW = (halfW * 2f) / cols;
        float cellH = (halfH * 2f) / rows;

        Vector2[,] grid = new Vector2[cols + 1, rows + 1];
        for (int r = 0; r <= rows; r++)
        {
            for (int c = 0; c <= cols; c++)
            {
                float x = -halfW + c * cellW;
                float y = -halfH + r * cellH;

                bool edgeCol = (c == 0 || c == cols);
                bool edgeRow = (r == 0 || r == rows);

                if (!edgeCol) x += Random.Range(-cellW * jitter, cellW * jitter);
                if (!edgeRow) y += Random.Range(-cellH * jitter, cellH * jitter);

                grid[c, r] = new Vector2(x, y);
            }
        }

        var result = new List<ShardData>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                result.Add(MakeShard(grid[c, r], grid[c + 1, r], grid[c, r + 1], halfW, halfH));
                result.Add(MakeShard(grid[c + 1, r], grid[c + 1, r + 1], grid[c, r + 1], halfW, halfH));
            }
        }

        return result;
    }

    ShardData MakeShard(Vector2 a2, Vector2 b2, Vector2 c2, float halfW, float halfH)
    {
        Vector3 a = new Vector3(a2.x, a2.y, 0);
        Vector3 b = new Vector3(b2.x, b2.y, 0);
        Vector3 c = new Vector3(c2.x, c2.y, 0);
        Vector3 center = (a + b + c) / 3f;
        Vector3 fwd = Vector3.forward * shardDepth * 0.5f;
        Vector3 la = a - center;
        Vector3 lb = b - center;
        Vector3 lc = c - center;

        Vector2 uvA = new Vector2((a2.x + halfW) / (halfW * 2f), (a2.y + halfH) / (halfH * 2f));
        Vector2 uvB = new Vector2((b2.x + halfW) / (halfW * 2f), (b2.y + halfH) / (halfH * 2f));
        Vector2 uvC = new Vector2((c2.x + halfW) / (halfW * 2f), (c2.y + halfH) / (halfH * 2f));

        Vector3[] verts = new Vector3[]
        {
        la - fwd, lb - fwd, lc - fwd,
        la + fwd, lb + fwd, lc + fwd,
        };

        int[] tris = new int[]
        {
        0, 2, 1,  3, 4, 5,
        0, 1, 3,  1, 4, 3,
        1, 2, 4,  2, 5, 4,
        2, 0, 5,  0, 3, 5
        };

        Vector3 fn = Vector3.Cross(lb - la, lc - la).normalized;
        Vector3[] normals = new Vector3[] { -fn, -fn, -fn, fn, fn, fn };
        Vector2[] uvs = new Vector2[] { uvA, uvB, uvC, uvA, uvB, uvC };

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        GameObject go = new GameObject("Shard");
        go.layer = shardLayer;
        go.transform.SetParent(shardRoot.transform, false);
        go.transform.localPosition = center;

        go.AddComponent<MeshFilter>().mesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        Material mat = new Material(glassShardMaterial);
        mat.renderQueue = (int)RenderQueue.Geometry + 100;

        if (capturedScreen != null)
            mat.mainTexture = capturedScreen;

        mr.material = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.mass = 0.02f;
        rb.drag = 0.4f;
        rb.angularDrag = 0.4f;

        activeShards.Add(go);

        float dist = new Vector2(center.x, center.y).magnitude;
        return new ShardData { go = go, center = center, dist = dist };
    }

    IEnumerator FadeAndDestroyShard(GameObject shard, float lifetime, float fade)
    {
        yield return new WaitForSeconds(lifetime);
        if (shard == null) yield break;
        activeShards.Remove(shard);
        Destroy(shard);
    }

    public void ClearShards()
    {
        StopAllCoroutines();
        foreach (var s in activeShards)
            if (s != null) Destroy(s);
        activeShards.Clear();
        if (shardRoot != null) Destroy(shardRoot);
        if (capturedScreen != null) Destroy(capturedScreen);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    struct ShardData
    {
        public GameObject go;
        public Vector3 center;
        public float dist;
    }
}