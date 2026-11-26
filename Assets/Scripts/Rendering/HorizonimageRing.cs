using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HorizonImageRing : MonoBehaviour
{
    public Material material;
    public Texture2D image;
    public float radius = 92f;
    public float bottomDeg = -3f;
    public float topDeg = 8f;
    public int segments = 360;
    public bool autoRepeat = true;
    public int repeatX = 6;
    public float rotationDeg = 0f;

    void Start()
    {
        float yBottom = radius * Mathf.Tan(bottomDeg * Mathf.Deg2Rad);
        float yTop = radius * Mathf.Tan(topDeg * Mathf.Deg2Rad);

        int rep = repeatX;
        if (autoRepeat && image != null)
        {
            float aspect = (float)image.width / image.height;
            float circumference = 2f * Mathf.PI * radius;
            rep = Mathf.Max(1, Mathf.RoundToInt(circumference / ((yTop - yBottom) * aspect)));
        }

        var verts = new Vector3[(segments + 1) * 2];
        var uvs = new Vector2[(segments + 1) * 2];
        var tris = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float k = (float)i / segments;
            float a = (k * 360f + rotationDeg) * Mathf.Deg2Rad;
            float x = Mathf.Sin(a) * radius;
            float z = Mathf.Cos(a) * radius;
            verts[i * 2] = new Vector3(x, yBottom, z);
            verts[i * 2 + 1] = new Vector3(x, yTop, z);
            uvs[i * 2] = new Vector2(k * rep, 0f);
            uvs[i * 2 + 1] = new Vector2(k * rep, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int v = i * 2, t = i * 6;
            tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
            tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
        }

        var mesh = new Mesh { name = "HorizonImageRing" };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        var mat = new Material(material);
        if (image != null) mat.SetTexture("_MainTex", image);
        GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
