using System;
using System.Collections.Generic;
using UnityEngine;

public static class SolarSystemObjects
{
    public static void AddBodies(List<StarVisual> list, DateTime utc, double latDeg, double lonDeg)
    {
        double jdTT = Ephemeris.JulianDateTT(utc);

        {
            Ephemeris.SunJ2000(jdTT, out double ra, out double dec);
            CoordinateConverter.RaDecToAltAz(ra, dec, latDeg, lonDeg, utc, out double alt, out double az);
            Add(list, "Sun", alt, az, -26.7f, 0.65f, 2.5f);
        }

        {
            Ephemeris.MoonOfDate(jdTT, out double ra, out double dec, out double dist);
            CoordinateConverter.EquatorialOfDateToAltAz(ra, dec, latDeg, lonDeg, utc, out double alt, out double az);
            // topocentric correction (parallax up to ~1 degree)
            alt -= Math.Asin(1.0 / dist) * 180.0 / Math.PI * Math.Cos(alt * Math.PI / 180.0);
            Add(list, "Moon", alt, az, -12f, 0.5f, 1.2f);
        }

        AddPlanet(list, Body.Mercury, 0.5f, 0.6f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Venus, -4.2f, 0.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Mars, 1.0f, 1.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Jupiter, -2.2f, 0.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Saturn, 0.6f, 1.0f, jdTT, utc, latDeg, lonDeg);
    }

    static void AddPlanet(List<StarVisual> list, Body body, float mag, float bv,
                          double jdTT, DateTime utc, double latDeg, double lonDeg)
    {
        Ephemeris.PlanetJ2000(body, jdTT, out double ra, out double dec);
        CoordinateConverter.RaDecToAltAz(ra, dec, latDeg, lonDeg, utc, out double alt, out double az);
        Add(list, body.ToString(), alt, az, mag, bv, 0f);
    }

    static void Add(List<StarVisual> list, string name, double alt, double az, float mag, float bv, float sizeDeg)
    {
        Debug.Log($"{name}: alt={alt:F2} az={az:F2}");
        if (alt < 0) return;
        list.Add(new StarVisual
        {
            Direction = StarField.AltAzToDirection((float)alt, (float)az),
            Magnitude = mag,
            BV = bv,
            SizeDeg = sizeDeg
        });
    }
}
