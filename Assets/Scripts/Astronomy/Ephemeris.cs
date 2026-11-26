using System;

public enum Body { Sun, Moon, Mercury, Venus, Mars, Jupiter, Saturn }

public static class Ephemeris
{
    const double D2R = Math.PI / 180.0;
    const double EpsJ2000 = 23.43928 * D2R;

    // a, da, e, de, I, dI, L, dL, varpi, dvarpi, Omega, dOmega (JPL, 1800-2050)
    static readonly double[][] Elem =
    {
        new double[] { 0.38709927, 0.00000037, 0.20563593, 0.00001906, 7.00497902, -0.00594749, 252.25032350, 149472.67411175, 77.45779628, 0.16047689, 48.33076593, -0.12534081 },
        new double[] { 0.72333566, 0.00000390, 0.00677672, -0.00004107, 3.39467605, -0.00078890, 181.97909950, 58517.81538729, 131.60246718, 0.00268329, 76.67984255, -0.27769418 },
        new double[] { 1.00000261, 0.00000562, 0.01671123, -0.00004392, -0.00001531, -0.01294668, 100.46457166, 35999.37244981, 102.93768193, 0.32327364, 0.0, 0.0 },
        new double[] { 1.52371034, 0.00001847, 0.09339410, 0.00007882, 1.84969142, -0.00813131, -4.55343205, 19140.30268499, -23.94362959, 0.44441088, 49.55953891, -0.29257343 },
        new double[] { 5.20288700, -0.00011607, 0.04838624, -0.00013253, 1.30439695, -0.00183714, 34.39644051, 3034.74612775, 14.72847983, 0.21252668, 100.47390909, 0.20469106 },
        new double[] { 9.53667594, -0.00125060, 0.05386179, -0.00050991, 2.48599187, 0.00193609, 49.95424423, 1222.49362201, 92.59887831, -0.41897216, 113.66242448, -0.28867794 },
    };
    const int EarthIdx = 2;

    static int Index(Body b)
    {
        switch (b)
        {
            case Body.Mercury: return 0;
            case Body.Venus: return 1;
            case Body.Mars: return 3;
            case Body.Jupiter: return 4;
            default: return 5; // Saturn
        }
    }

    // Terrestrial time (UTC + ~69 s)
    public static double JulianDateTT(DateTime utc)
    {
        return CoordinateConverter.JulianDate(utc) + 69.0 / 86400.0;
    }

    static void Helio(int idx, double T, out double x, out double y, out double z)
    {
        double[] p = Elem[idx];
        double a = p[0] + p[1] * T;
        double e = p[2] + p[3] * T;
        double I = (p[4] + p[5] * T) * D2R;
        double L = p[6] + p[7] * T;
        double varpi = p[8] + p[9] * T;
        double Om = p[10] + p[11] * T;
        double w = (varpi - Om) * D2R;

        double M = (L - varpi) % 360.0;
        if (M > 180.0) M -= 360.0;
        if (M < -180.0) M += 360.0;
        M *= D2R;

        double E = M + e * Math.Sin(M);
        for (int k = 0; k < 10; k++)
            E = E - (E - e * Math.Sin(E) - M) / (1.0 - e * Math.Cos(E));

        double xp = a * (Math.Cos(E) - e);
        double yp = a * Math.Sqrt(1.0 - e * e) * Math.Sin(E);

        double O = Om * D2R;
        double cw = Math.Cos(w), sw = Math.Sin(w);
        double cO = Math.Cos(O), sO = Math.Sin(O);
        double cI = Math.Cos(I), sI = Math.Sin(I);

        x = (cw * cO - sw * sO * cI) * xp + (-sw * cO - cw * sO * cI) * yp;
        y = (cw * sO + sw * cO * cI) * xp + (-sw * sO + cw * cO * cI) * yp;
        z = (sw * sI) * xp + (cw * sI) * yp;
    }

    static void ToRaDec(double x, double y, double z, double eps, out double raHours, out double decDeg)
    {
        double ye = Math.Cos(eps) * y - Math.Sin(eps) * z;
        double ze = Math.Sin(eps) * y + Math.Cos(eps) * z;
        double r = Math.Sqrt(x * x + ye * ye + ze * ze);
        double ra = Math.Atan2(ye, x);
        if (ra < 0) ra += 2.0 * Math.PI;
        raHours = ra / D2R / 15.0;
        decDeg = Math.Asin(ze / r) / D2R;
    }

    // RA (hours) / Dec (deg), equinox J2000
    public static void SunJ2000(double jdTT, out double raHours, out double decDeg)
    {
        double T = (jdTT - 2451545.0) / 36525.0;
        Helio(EarthIdx, T, out double ex, out double ey, out double ez);
        ToRaDec(-ex, -ey, -ez, EpsJ2000, out raHours, out decDeg);
    }

    // RA (hours) / Dec (deg), equinox J2000, geocentric
    public static void PlanetJ2000(Body b, double jdTT, out double raHours, out double decDeg)
    {
        double T = (jdTT - 2451545.0) / 36525.0;
        Helio(Index(b), T, out double px, out double py, out double pz);
        Helio(EarthIdx, T, out double ex, out double ey, out double ez);
        ToRaDec(px - ex, py - ey, pz - ez, EpsJ2000, out raHours, out decDeg);
    }

    // Apparent magnitude and angular disc diameter [deg] of a planet (Astronomical Almanac formulas)
    public static void PlanetAppearance(Body b, double jdTT, out double mag, out double diameterDeg)
    {
        double T = (jdTT - 2451545.0) / 36525.0;
        Helio(Index(b), T, out double px, out double py, out double pz);
        Helio(EarthIdx, T, out double ex, out double ey, out double ez);

        double r = Math.Sqrt(px * px + py * py + pz * pz);
        double R = Math.Sqrt(ex * ex + ey * ey + ez * ez);
        double gx = px - ex, gy = py - ey, gz = pz - ez;
        double d = Math.Sqrt(gx * gx + gy * gy + gz * gz);

        double cosI = (r * r + d * d - R * R) / (2.0 * r * d);
        double i = Math.Acos(Math.Max(-1.0, Math.Min(1.0, cosI))) / D2R; // phase angle
        double lg = 5.0 * Math.Log10(r * d);

        double d0; // diameter in arcseconds at 1 AU
        switch (b)
        {
            case Body.Mercury:
                mag = -0.42 + lg + 0.0380 * i - 0.000273 * i * i + 0.000002 * i * i * i;
                d0 = 6.74; break;
            case Body.Venus:
                mag = -4.40 + lg + 0.0009 * i + 0.000239 * i * i - 0.00000065 * i * i * i;
                d0 = 16.69; break;
            case Body.Mars:
                mag = -1.52 + lg + 0.016 * i;
                d0 = 9.35; break;
            case Body.Jupiter:
                mag = -9.40 + lg + 0.005 * i;
                d0 = 197.1; break;
            default: // Saturn (with ring tilt correction)
            {
                double ye = Math.Cos(EpsJ2000) * gy - Math.Sin(EpsJ2000) * gz;
                double ze = Math.Sin(EpsJ2000) * gy + Math.Cos(EpsJ2000) * gz;
                double ra = Math.Atan2(ye, gx);
                double dec = Math.Asin(ze / d);
                double raP = 40.589 * D2R, decP = 83.537 * D2R; // Saturn pole (J2000)
                double sinB = -(Math.Sin(decP) * Math.Sin(dec) + Math.Cos(decP) * Math.Cos(dec) * Math.Cos(raP - ra));
                double sb = Math.Abs(sinB);
                mag = -8.88 + lg - 2.60 * sb + 1.25 * sb * sb;
                d0 = 166.2; break;
            }
        }
        diameterDeg = d0 / d / 3600.0;
    }

    static double Rev(double x)
    {
        x %= 360.0;
        if (x < 0) x += 360.0;
        return x;
    }

    static double S(double deg) { return Math.Sin(deg * D2R); }
    static double C(double deg) { return Math.Cos(deg * D2R); }

    // Geocentric, equinox of date. Distance in Earth radii.
    public static void MoonOfDate(double jdTT, out double raHours, out double decDeg, out double distEarthRadii)
    {
        double days = jdTT - 2451543.5;
        double N = Rev(125.1228 - 0.0529538083 * days);
        const double inc = 5.1454;
        double w = Rev(318.0634 + 0.1643573223 * days);
        const double a = 60.2666;
        const double e = 0.054900;
        double M = Rev(115.3654 + 13.0649929509 * days);
        double Ms = Rev(356.0470 + 0.9856002585 * days);
        double ws = 282.9404 + 4.70935e-5 * days;
        double Ls = Rev(ws + Ms);
        double Lm = Rev(N + w + M);
        double D = Rev(Lm - Ls);
        double F = Rev(Lm - N);

        double Mr = M * D2R;
        double E = Mr + e * Math.Sin(Mr) * (1.0 + e * Math.Cos(Mr));
        for (int k = 0; k < 10; k++)
            E = E - (E - e * Math.Sin(E) - Mr) / (1.0 - e * Math.Cos(E));

        double xv = a * (Math.Cos(E) - e);
        double yv = a * Math.Sqrt(1.0 - e * e) * Math.Sin(E);
        double v = Math.Atan2(yv, xv) / D2R;
        double r = Math.Sqrt(xv * xv + yv * yv);

        double vw = (v + w) * D2R, Nr = N * D2R, ir = inc * D2R;
        double xh = r * (Math.Cos(Nr) * Math.Cos(vw) - Math.Sin(Nr) * Math.Sin(vw) * Math.Cos(ir));
        double yh = r * (Math.Sin(Nr) * Math.Cos(vw) + Math.Cos(Nr) * Math.Sin(vw) * Math.Cos(ir));
        double zh = r * Math.Sin(vw) * Math.Sin(ir);

        double lon = Math.Atan2(yh, xh) / D2R;
        double lat = Math.Atan2(zh, Math.Sqrt(xh * xh + yh * yh)) / D2R;
        double dist = r;

        lon += -1.274 * S(M - 2 * D) + 0.658 * S(2 * D) - 0.186 * S(Ms) - 0.059 * S(2 * M - 2 * D)
             - 0.057 * S(M - 2 * D + Ms) + 0.053 * S(M + 2 * D) + 0.046 * S(2 * D - Ms) + 0.041 * S(M - Ms)
             - 0.035 * S(D) - 0.031 * S(M + Ms) - 0.015 * S(2 * F - 2 * D) + 0.011 * S(M - 4 * D);
        lat += -0.173 * S(F - 2 * D) - 0.055 * S(M - F - 2 * D) - 0.046 * S(M + F - 2 * D)
             + 0.033 * S(F + 2 * D) + 0.017 * S(2 * M + F);
        dist += -0.58 * C(M - 2 * D) - 0.46 * C(2 * D);

        double T = (jdTT - 2451545.0) / 36525.0;
        double eps = (23.439291 - 0.0130042 * T) * D2R;
        double lr = Rev(lon) * D2R, br = lat * D2R;
        double x = Math.Cos(br) * Math.Cos(lr);
        double y = Math.Cos(br) * Math.Sin(lr);
        double z = Math.Sin(br);
        double ye = Math.Cos(eps) * y - Math.Sin(eps) * z;
        double ze = Math.Sin(eps) * y + Math.Cos(eps) * z;
        double ra = Math.Atan2(ye, x);
        if (ra < 0) ra += 2.0 * Math.PI;

        raHours = ra / D2R / 15.0;
        decDeg = Math.Asin(ze) / D2R;
        distEarthRadii = dist;
    }
}
