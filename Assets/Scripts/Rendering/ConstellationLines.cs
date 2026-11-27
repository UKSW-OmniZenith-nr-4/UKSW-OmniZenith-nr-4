using System;
using System.Collections.Generic;
using UnityEngine;

// Constellation lines (KA-4.05). Segments (HIP A -> HIP B) come from the database
// (table LiniaKonstelacji, Stellarium "western" sky culture). Only the part of a line
// above the horizon is drawn.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConstellationLines : MonoBehaviour
{
    public float radius = 98f;                                   // behind trees (92), in front of stars (100)
    public Color lineColor = new Color(0.45f, 0.65f, 1f, 0.45f);
    public bool visible = true;
    public Rect buttonRect = new Rect(10, 106, 190, 36);
    public int fontSize = 18;

    Mesh mesh;
    Material mat;
    MeshRenderer mr;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        var shader = Shader.Find("OmniZenith/Line");
        if (shader == null)
        {
            Debug.LogError("ConstellationLines: shader OmniZenith/Line not found");
            return;
        }
        mat = new Material(shader);
        mat.SetColor("_Color", lineColor);
        mr.sharedMaterial = mat;
        mr.enabled = visible;
    }

    // Call after the sky is built (and again whenever the city or time changes)
    public void Build(DbManager dbm, double latDeg, double lonDeg, DateTime utc)
    {
        if (dbm == null || dbm.Db == null || mat == null) return;

        var segments = dbm.Db.Table<LiniaKonstelacji>().ToList();
        var needed = new HashSet<int>();
        foreach (var s in segments) { needed.Add(s.HipA); needed.Add(s.HipB); }

        // direction of every star used by the lines
        var dirs = new Dictionary<int, Vector3>();
        foreach (var g in dbm.Db.Table<Gwiazda>().ToList())
        {
            if (g.Hip <= 0 || !needed.Contains(g.Hip)) continue;
            CoordinateConverter.RaDecToAltAz(g.Ra, g.Dec, latDeg, lonDeg, utc, out double alt, out double az);
            dirs[g.Hip] = StarField.AltAzToDirection((float)alt, (float)az);
        }

        var verts = new List<Vector3>();
        var idx = new List<int>();
        int skipped = 0;

        foreach (var s in segments)
        {
            if (!dirs.TryGetValue(s.HipA, out Vector3 a) || !dirs.TryGetValue(s.HipB, out Vector3 b))
            {
                skipped++;
                continue;
            }
            if (!ClipToHorizon(ref a, ref b)) continue;

            idx.Add(verts.Count); verts.Add(a * radius);
            idx.Add(verts.Count); verts.Add(b * radius);
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "ConstellationLines" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetIndices(idx, MeshTopology.Lines, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 3f);

        Debug.Log($"ConstellationLines: segments in DB {segments.Count}, drawn {idx.Count / 2}, skipped (star missing) {skipped}");
    }

    // Keeps only the part of the segment with y >= 0 (above the horizon).
    static bool ClipToHorizon(ref Vector3 a, ref Vector3 b)
    {
        if (a.y < 0f && b.y < 0f) return false;
        if (a.y >= 0f && b.y >= 0f) return true;

        float t = a.y / (a.y - b.y);
        Vector3 p = Vector3.Lerp(a, b, t).normalized;
        p.y = 0f;
        if (a.y < 0f) a = p; else b = p;
        return true;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        if (GUI.Button(buttonRect, "Konstelacje: " + (visible ? "ON" : "OFF"), style))
        {
            visible = !visible;
            if (mr != null) mr.enabled = visible;
        }
    }
}
