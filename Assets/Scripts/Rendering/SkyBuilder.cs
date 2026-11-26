using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(StarField))]
public class SkyBuilder : MonoBehaviour
{
    public string miasto = "Warsaw";
    public double latitude = 52.23;
    public double longitude = 21.01;
    public float maksMag = 6.5f;

    void Awake()
    {
        GetComponent<StarField>().useRandomTest = false;
    }

    void Start()
    {
        var dbm = FindFirstObjectByType<DbManager>();
        if (dbm == null || dbm.Db == null)
        {
            Debug.LogError("SkyBuilder: brak DbManager w scenie");
            return;
        }

        string nazwaMiasta = miasto;
        var m = dbm.Db.Table<Miasto>()
            .Where(x => x.Nazwa == nazwaMiasta && x.MaWspolrzedne)
            .FirstOrDefault();
        if (m != null)
        {
            latitude = m.Lat;
            longitude = m.Lon;
        }
        Debug.Log($"SkyBuilder: {miasto} lat={latitude} lon={longitude}" + (m == null ? " (domyslne)" : ""));

        float maks = maksMag;
        var gwiazdy = dbm.Db.Table<Gwiazda>()
            .Where(g => g.Mag <= maks && g.Mag > -20f)
            .ToList();

        DateTime now = DateTime.UtcNow;
        var widoczne = new List<StarVisual>(gwiazdy.Count);

        foreach (var g in gwiazdy)
        {
            CoordinateConverter.RaDecToAltAz(g.Ra, g.Dec, latitude, longitude, now, out double alt, out double az);
            //if (g.Nazwa == "Sirius" || g.Nazwa == "Betelgeuse" || g.Nazwa == "Rigel" || g.Nazwa == "Vega" || g.Nazwa == "Capella") //DO SPRAWDZENIA <1 C
            //    Debug.Log($"{g.Nazwa}: alt={alt:F2} az={az:F2}");
            if (alt < 0) continue;

            double a = alt * Math.PI / 180.0, z = az * Math.PI / 180.0;
            widoczne.Add(new StarVisual
            {
                Direction = new Vector3(
                    (float)(Math.Cos(a) * Math.Sin(z)),
                    (float)Math.Sin(a),
                    (float)(Math.Cos(a) * Math.Cos(z))),
                Magnitude = g.Mag,
                BV = 0.6f
            });
        }
        SolarSystemObjects.AddBodies(widoczne, now, latitude, longitude);
        Debug.Log($"SkyBuilder: gwiazd w bazie {gwiazdy.Count}, nad horyzontem {widoczne.Count}");
        GetComponent<StarField>().Build(widoczne.ToArray());
    }
}
