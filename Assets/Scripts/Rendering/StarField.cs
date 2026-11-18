using UnityEngine;

public struct StarVisual
{
    public Vector3 Direction;
    public float Magnitude;
    public float BV;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StarField : MonoBehaviour
{
    public bool useRandomTest = true;
    public int randomStarCount = 10000;

    public float radius = 100f;
    public float minSizeDeg = 0.15f;
    public float maxSizeDeg = 0.9f;
    public float faintMagnitude = 6.5f;
    public float brightMagnitude = -1.5f;

    void Start()
    {
        if (useRandomTest)
            Build(GenerateRandomStars(randomStarCount));
    }

    public static Vector3 AltAzToDirection(float altitudeDeg, float azimuthDeg)
    {
        float alt = altitudeDeg * Mathf.Deg2Rad;
        float az = azimuthDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(alt) * Mathf.Sin(az), Mathf.Sin(alt), Mathf.Cos(alt) * Mathf.Cos(az));
    }

    public void Build(StarVisual[] stars)
    {
        int n = stars.Length;
        var vertices = new Vector3[n * 4];
        var colors = new Color[n * 4];
        var uvs = new Vector2[n * 4];
        var triangles = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            Vector3 dir = stars[i].Direction.normalized;
            Vector3 up = Mathf.Abs(dir.y) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(up, dir).normalized;
            Vector3 realUp = Vector3.Cross(dir, right);

            float t = Mathf.InverseLerp(faintMagnitude, brightMagnitude, stars[i].Magnitude);
            float sizeDeg = Mathf.Lerp(minSizeDeg, maxSizeDeg, t);
            float brightness = Mathf.Lerp(0.3f, 1f, t);

            Vector3 center = dir * radius;
            int v = i * 4;
            vertices[v + 0] = center - right * half - realUp * half;
            vertices[v + 1] = center + right * half - realUp * half;
            vertices[v + 2] = center + right * half + realUp * half;
            vertices[v + 3] = center - right * half + realUp * half;

            Color c = BVToColor(stars[i].BV) * brightness;
            c.a = 1f;
            for (int k = 0; k < 4; k++) colors[v + k] = c;

            uvs[v + 0] = new Vector2(0, 0);
            uvs[v + 1] = new Vector2(1, 0);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(0, 1);

            int ti = i * 6;
            triangles[ti + 0] = v; triangles[ti + 1] = v + 2; triangles[ti + 2] = v + 1;
            triangles[ti + 3] = v; triangles[ti + 4] = v + 3; triangles[ti + 5] = v + 2;
        }

        var mesh = new Mesh { name = "StarField", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    static Color BVToColor(float bv)
    {
        Color blue = new Color(0.6f, 0.75f, 1f);
        Color white = Color.white;
        Color orange = new Color(1f, 0.8f, 0.5f);
        Color red = new Color(1f, 0.5f, 0.3f);
        bv = Mathf.Clamp(bv, -0.4f, 2.0f);
        if (bv < 0.6f) return Color.Lerp(blue, white, Mathf.InverseLerp(-0.4f, 0.6f, bv));
        if (bv < 1.2f) return Color.Lerp(white, orange, Mathf.InverseLerp(0.6f, 1.2f, bv));
        return Color.Lerp(orange, red, Mathf.InverseLerp(1.2f, 2.0f, bv));
    }

    static StarVisual[] GenerateRandomStars(int count)
    {
        var stars = new StarVisual[count];
        for (int i = 0; i < count; i++)
        {
            float t = Mathf.Pow(Random.value, 3f);
            stars[i] = new StarVisual
            {
                Direction = Random.onUnitSphere,
                Magnitude = 6.5f - t * 8f,
                BV = Random.Range(-0.3f, 1.8f)
            };
        }
        return stars;
    }
}
