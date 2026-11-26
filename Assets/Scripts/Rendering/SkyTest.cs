using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StarField))]
public class SkyTest : MonoBehaviour
{
    public double latitude = 52.23;
    public double longitude = 21.01;

    struct Entry
    {
        public string name; public double ra, dec; public float mag, bv;
        public Entry(string n, double r, double d, float m, float b) { name = n; ra = r; dec = d; mag = m; bv = b; }
    }

    static readonly Entry[] stars =
    {
        new Entry("Sirius", 6.7525, -16.7161, -1.46f, 0.00f),
        new Entry("Betelgeuse", 5.9195, 7.4071, 0.42f, 1.85f),
        new Entry("Rigel", 5.2423, -8.2017, 0.13f, -0.03f),
        new Entry("Polaris", 2.5303, 89.2641, 1.98f, 0.60f),
        new Entry("Vega", 18.6156, 38.7837, 0.03f, 0.00f),
        new Entry("Arcturus", 14.2610, 19.1824, -0.05f, 1.23f),
        new Entry("Capella", 5.2782, 45.9980, 0.08f, 0.80f),
        new Entry("Aldebaran", 4.5987, 16.5093, 0.85f, 1.54f),
        new Entry("Altair", 19.8464, 8.8683, 0.76f, 0.22f),
        new Entry("Deneb", 20.6905, 45.2803, 1.25f, 0.09f),
    };

    void Awake()
    {
        GetComponent<StarField>().useRandomTest = false;
    }

    void Start()
    {
        DateTime now = DateTime.UtcNow;
        var visible = new List<StarVisual>();

        foreach (var s in stars)
        {
            CoordinateConverter.RaDecToAltAz(s.ra, s.dec, latitude, longitude, now, out double alt, out double az);
            Debug.Log($"{s.name}: alt = {alt:F2}, az = {az:F2}");
            if (alt < 0) continue;

            double altR = alt * Math.PI / 180.0, azR = az * Math.PI / 180.0;
            visible.Add(new StarVisual
            {
                Direction = new Vector3(
                    (float)(Math.Cos(altR) * Math.Sin(azR)),
                    (float)Math.Sin(altR),
                    (float)(Math.Cos(altR) * Math.Cos(azR))),
                Magnitude = s.mag,
                BV = s.bv
            });
        }

        GetComponent<StarField>().Build(visible.ToArray());
    }
}