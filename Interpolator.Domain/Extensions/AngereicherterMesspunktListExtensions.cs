using Interpolator.Domain.Models;

namespace Interpolator.Domain.Extensions;

public static class MessungListExtensions
{
  public static IEnumerable<Splinepunkt> ToSplinepunkte(
    this IEnumerable<SplineMesspunkt> messListe,
    double offset
  )
  {
    int nbrMessungen = messListe.Count();

    for (int i = 0; i < nbrMessungen; i++)
    {
      SplineMesspunkt messpunkt = messListe.ElementAt(i);
      int nextIndex = i + 1;

      if (nextIndex < nbrMessungen)
      {
        SplineMesspunkt nextMesspunkt = messListe.ElementAt(nextIndex);
        TimeSpan totalDurationBetweenMesspunkte = nextMesspunkt.T - messpunkt.T;
        TimeSpan durationPerInterpolation = totalDurationBetweenMesspunkte * offset;
        double currentX = messpunkt.X;
        DateTime currentT = messpunkt.T;

        while (currentX < nextMesspunkt.X)
        {
          yield return new Splinepunkt(messpunkt, currentX, currentT);

          currentX += offset;
          currentT += durationPerInterpolation;
        }
      }
      else
      {
        // If this is the last point, we can still yield it
        yield return new Splinepunkt(messpunkt, messpunkt.X, messpunkt.T);
      }
    }
  }

  public static IEnumerable<SplineMesspunkt> ToSplineMesspunkte(
    this List<Messpunkt> messpunkte,
    double gewichtung
  )
  // Glättende kubische Spline-Funktion {Helmut Späth: Algorithmen für elementare Ausgleichs-Modelle,
  // Oldenbourg Verlag München Wien 1973, ISBN 3-486-39561-0} Seite 62 Bild U11
  //
  // yk (x)  = Ak * (x - xk)^3 + Bk * (x - xk)^2 + Ck * (x - xk) + Dk
  // x1 < x2 < ...... < xn
  // k  = Index [1]
  //
  // Änderungen
  //03032014_01      H4 -> N3 und H3 -> N4 wurden gewechsel und gemäss Original korrigiert
  // Pos 135           if (N4 != 0)
  // Pos 140             if (N3 != 0)

  {
    var messliste = MesspunktExtensions.ToAngereicherteMesspunkte(messpunkte, gewichtung).ToList();

    int N = (messliste.Count - 1); // Anzahl Messpunkte [1] Startwert = 0
    int N1 = N - 1; // Index-Grenze [1]
    messliste[0].C = 0; // Ck = C (k) Parameter
    messliste[0].D = 0; // Dk = D (k) Parameter
    int N2 = N - 2; // Index-Grenze [1]
    int N3 = N - 3; // Index-Grenze [1]
    int N4 = N - 4; // Index-Grenze [1]
    messliste[0].Y2 = 0; // y''(x) = dy^2/dx^2  2. Ableitung
    messliste[N].Y2 = 0;

    for (int k = 0; k <= N1; k++)
    {
      messliste[k].D = 1 / (messliste[k + 1].X - messliste[k].X);
    }

    messliste[N].D = 0; // Dk = D (k) Parameter
    double A1 = 0; // A1 = A (0) Parameter
    double A2 = 0; // A2 = A (1) Parameter
    double B1 = 0; // B1 = B (0) Parameter
    messliste[0].A = 0; // Ak = A (k) Parameter
    messliste[0].B = 0; // Bk = B (k) Parameter
    messliste[1].B = 0;
    int J1 = 0; // Index [1]
    int J2 = 0; // Index [1]
    double P1 = 1 / (messliste[0].P ?? gewichtung); // Reziprokes Gewicht [1]  P1 = 1 / P(0)
    double P2 = 1 / (messliste[1].P ?? gewichtung); // Reziprokes Gewicht [1]  P2 = 1 / P(1)
    double H = 0;
    double H1 = messliste[0].D;
    double H2 = messliste[1].D;
    double R1 = (messliste[1].Y - messliste[0].Y) * H1;

    for (int k = 0; k <= N2; k++)
    {
      int k1 = k + 1;
      int k2 = k + 2;

      if (k > 0)
      {
        H = B1 - A1 * messliste[J1].A;
      }

      double H3 = messliste[k2].D;
      double P3 = 1 / (messliste[k2].P ?? gewichtung);
      double S = H1 + H2;
      double T = 2 / H1 + 2 / H2 + 6 * (H1 * H1 * P1 + S * S * P2 + H2 * H2 * P3);

      double R2 = (messliste[k2].Y - messliste[k1].Y) * H2;
      double B2 = 1 / H2 - 6 * H2 * (P2 * S + P3 * (H2 + H3));
      double A3 = 6 * P3 * H2 * H3;
      double Z = 1 / (T - A1 * messliste[k].B - H * messliste[k].A);

      if (k <= N3) //03032014_01 N4 > N3
      {
        messliste[k1].A = Z * (B2 - H * messliste[k1].B);
      }

      if (k <= N4) //03032014_01 N3 > N4
      {
        messliste[k2].B = Z * A3;
      }

      double R = 6 * (R2 - R1);

      if (k >= 1)
      {
        R = R - H * messliste[J1].C;
      }

      if (k > 1)
      {
        R = R - A1 * messliste[J2].C;
      }

      messliste[k].C = Z * R;
      J2 = J1;
      J1 = k;
      A1 = A2;
      A2 = A3;
      B1 = B2;
      H1 = H2;
      H2 = H3;
      P1 = P2;
      P2 = P3;
      R1 = R2;
    }

    messliste[N1].Y2 = messliste[N2].C;

    if (N4 != 0)
    {
      messliste[N2].Y2 = messliste[N3].C - messliste[N2].A * messliste[N1].Y2;
      if (N3 != 0)
      {
        for (int j = 0; j <= N4; j++)
        {
          int k = N2 - j;
          int k1 = k + 1;
          int k2 = k + 2;
          messliste[k].Y2 =
            messliste[k - 1].C
            - messliste[k].A * messliste[k1].Y2
            - messliste[k1].B * messliste[k2].Y2;
        }
      }
    }

    H1 = 0;

    for (int k = 0; k <= N1; k++)
    {
      J2 = k + 1;
      messliste[k].C = messliste[k].D;
      H2 = messliste[k].D * (messliste[J2].Y2 - messliste[k].Y2);
      messliste[k].A = H2 / 6;
      messliste[k].D = messliste[k].Y - (H2 - H1) / (messliste[k].P ?? gewichtung);

      messliste[k].B = 0.5 * messliste[k].Y2;
      H1 = H2;
    }

    messliste[N].D = messliste[N].Y + H1 / (messliste[N].P ?? gewichtung);

    for (int k = 0; k <= N1; k++)
    {
      J2 = k + 1;
      H = messliste[k].C;
      messliste[k].C =
        (messliste[J2].D - messliste[k].D) * H - (messliste[J2].Y2 + 2 * messliste[k].Y2) / (6 * H);
    }
    messliste[N].C =
      (messliste[N].D - messliste[N1].D) * H + (2 * messliste[N].Y2 + messliste[N1].Y2) / (6 * H);

    return messliste;
  }
}
