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
            Add(list, "Sun", alt, az, -26.7f, 0.65f, 0.35f, 0.533f);
        }

        {
            Ephemeris.MoonOfDate(jdTT, out double ra, out double dec, out double dist);
            CoordinateConverter.EquatorialOfDateToAltAz(ra, dec, latDeg, lonDeg, utc, out double alt, out double az);
            // topocentric correction (parallax up to ~1 degree)
            alt -= Math.Asin(1.0 / dist) * 180.0 / Math.PI * Math.Cos(alt * Math.PI / 180.0);
            float moonDiam = (float)(2.0 * Math.Asin(0.2725 / dist) * 180.0 / Math.PI);
            Add(list, "Moon", alt, az, -12f, 0.5f, 0.3f, moonDiam);
        }

        AddPlanet(list, Body.Mercury, 0.6f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Venus, 0.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Mars, 1.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Jupiter, 0.5f, jdTT, utc, latDeg, lonDeg);
        AddPlanet(list, Body.Saturn, 1.0f, jdTT, utc, latDeg, lonDeg);
    }

    static void AddPlanet(List<StarVisual> list, Body body, float bv,
                          double jdTT, DateTime utc, double latDeg, double lonDeg)
    {
        Ephemeris.PlanetJ2000(body, jdTT, out double ra, out double dec);
        CoordinateConverter.RaDecToAltAz(ra, dec, latDeg, lonDeg, utc, out double alt, out double az);
        Ephemeris.PlanetAppearance(body, jdTT, out double mag, out double diam);
        // sizeDeg = 0 -> point size is computed from magnitude, exactly like for stars
        Add(list, body.ToString(), alt, az, (float)mag, bv, 0f, (float)diam);
    }

    // The Sun, Moon and planets are drawn by BodySprites (point -> picture), not by StarField
    static void Add(List<StarVisual> list, string name, double alt, double az, float mag, float bv,
                    float sizeDeg, float diameterDeg)
    {
        BodySprites.Place(name, (float)alt, (float)az, diameterDeg, mag, bv, sizeDeg);
    }
}
