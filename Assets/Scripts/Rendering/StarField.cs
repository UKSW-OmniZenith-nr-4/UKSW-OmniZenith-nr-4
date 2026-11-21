using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct StarVisual
{
    public Vector3 Direction;
    public float Magnitude;
    public float BV;
    public float SizeDeg;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StarField : MonoBehaviour
{
    public float radius = 100f;
    public float minSizeDeg = 0.12f;
    public float maxSizeDeg = 0.45f;
    public float haloScale = 5f;
    public float faintMagnitude = 6.5f;
    public float brightMagnitude = -1.5f;
    public bool useRandomTest = false;

    Mesh mesh;

    void Start()
    {
        if (!useRandomTest) return;

        var list = new List<StarVisual>();
        for (int i = 0; i < 3000; i++)
        {
            list.Add(new StarVisual
            {
                Direction = Random.onUnitSphere,
                Magnitude = Random.Range(-1f, 6.5f),
                BV = Random.Range(-0.2f, 1.6f),
                SizeDeg = 0f
            });
        }
        Build(list);
    }

    public static Vector3 AltAzToDirection(float altDeg, float azDeg)
    {
        float alt = altDeg * Mathf.Deg2Rad;
        float az = azDeg * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(alt) * Mathf.Sin(az),
            Mathf.Sin(alt),
            Mathf.Cos(alt) * Mathf.Cos(az));
    }

    public void Build(IList<StarVisual> stars)
    {
        int n = stars.Count;
        var verts = new Vector3[n * 4];
        var cols = new Color[n * 4];
        var uvs = new Vector2[n * 4];
        var idx = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            Vector3 dir = stars[i].Direction.normalized;

            float t = Mathf.InverseLerp(faintMagnitude, brightMagnitude, stars[i].Magnitude);
            float sizeDeg = Mathf.Lerp(minSizeDeg, maxSizeDeg, t);
            float brightness = Mathf.Lerp(0.55f, 1f, t);
            if (stars[i].SizeDeg > 0f) sizeDeg = stars[i].SizeDeg;

            float half = radius * Mathf.Tan(sizeDeg * haloScale * Mathf.Deg2Rad * 0.5f);

            Vector3 up = Mathf.Abs(dir.y) > 0.99f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(up, dir).normalized;
            Vector3 realUp = Vector3.Cross(dir, right).normalized;

            Vector3 c = dir * radius;
            int v = i * 4;
            verts[v + 0] = c - right * half - realUp * half;
            verts[v + 1] = c + right * half - realUp * half;
            verts[v + 2] = c + right * half + realUp * half;
            verts[v + 3] = c - right * half + realUp * half;

            uvs[v + 0] = new Vector2(0, 0);
            uvs[v + 1] = new Vector2(1, 0);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(0, 1);

            Color col = Saturate(BVToColor(stars[i].BV), 2.5f) * brightness;
            col.a = 1f;
            cols[v + 0] = cols[v + 1] = cols[v + 2] = cols[v + 3] = col;

            int k = i * 6;
            idx[k + 0] = v; idx[k + 1] = v + 2; idx[k + 2] = v + 1;
            idx[k + 3] = v; idx[k + 4] = v + 3; idx[k + 5] = v + 2;
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "StarField" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.colors = cols;
        mesh.uv = uvs;
        mesh.triangles = idx;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 3f);
    }
    static Color Saturate(Color c, float amount)
    {
        float gray = (c.r + c.g + c.b) / 3f;
        return new Color(
            Mathf.Clamp01(gray + (c.r - gray) * amount),
            Mathf.Clamp01(gray + (c.g - gray) * amount),
            Mathf.Clamp01(gray + (c.b - gray) * amount), 1f);
    }
    static Color BVToColor(float bv)
    {
        bv = Mathf.Clamp(bv, -0.4f, 2.0f);
        float temp = 4600f * (1f / (0.92f * bv + 1.7f) + 1f / (0.92f * bv + 0.62f));
        float t = temp / 100f;
        float r, g, b;

        if (t <= 66f) r = 255f;
        else r = 329.698727446f * Mathf.Pow(t - 60f, -0.1332047592f);

        if (t <= 66f) g = 99.4708025861f * Mathf.Log(t) - 161.1195681661f;
        else g = 288.1221695283f * Mathf.Pow(t - 60f, -0.0755148492f);

        if (t >= 66f) b = 255f;
        else if (t <= 19f) b = 0f;
        else b = 138.5177312231f * Mathf.Log(t - 10f) - 305.0447927307f;

        return new Color(
            Mathf.Clamp01(r / 255f),
            Mathf.Clamp01(g / 255f),
            Mathf.Clamp01(b / 255f), 1f);
    }
}
