using Interpolator.Domain.Models;

namespace Interpolator.Domain.Extensions;

public static class MessungListExtensions
{
  // Glättende kubische Spline-Funktion {Helmut Späth: Algorithmen für elementare Ausgleichs-Modelle,
  // Oldenbourg Verlag München Wien 1973, ISBN 3-486-39561-0} Seite 62 Bild U11
  //
  // yk (x)  = Ak * (x - xk)^3 + Bk * (x - xk)^2 + Ck * (x - xk) + Dk
  // x1 < x2 < ...... < xn
  // k  = Index [1]
  public static List<AngereicherterMesspunkt> CalculateSpline(this List<AngereicherterMesspunkt> messListe)
  {
    int N = messListe.Count - 1;            // Anzahl Messpunkte [1] Startwert = 0
    int N1 = N - 1;                         // Index-Grenze [1]
    messListe[0].C = 0;                     // Ck = C (k) Parameter
    messListe[0].D = 0;                     // Dk = D (k) Parameter
    int N2 = N - 2;                         // Index-Grenze [1]
    int N3 = N - 3;                         // Index-Grenze [1]
    int N4 = N - 4;                         // Index-Grenze [1]
    messListe[0].Y2 = 0;                    // y''(x) = dy^2/dx^2  2. Ableitung
    messListe[N].Y2 = 0;

    for (int k = 0; k <= N1; k++)
    {
      messListe[k].D = 1 / (messListe[k + 1].X - messListe[k].X);
    }

    messListe[N].D = 0;                     // Dk = D (k) Parameter
    double A1 = 0;                          // A1 = A (0) Parameter
    double A2 = 0;                          // A2 = A (1) Parameter
    double B1 = 0;                          // B1 = B (0) Parameter
    messListe[0].A = 0;                     // Ak = A (k) Parameter
    messListe[0].B = 0;                     // Bk = B (k) Parameter
    messListe[1].B = 0;
    int J1 = 0;                             // Index [1]
    int J2 = 0;                             // Index [1]
    double P1 = 1 / messListe[0].P;         // Reziprokes Gewicht [1]  P1 = 1 / P(0)
    double P2 = 1 / messListe[1].P;         // Reziprokes Gewicht [1]  P2 = 1 / P(1)
    double H = 0;
    double H1 = messListe[0].D;
    double H2 = messListe[1].D;
    double R1 = (messListe[1].Y - messListe[0].Y) * H1;

    for (int k = 0; k <= N2; k++)
    {
      int k1 = k + 1;
      int k2 = k + 2;

      if (k > 0)
      {
        H = B1 - A1 * messListe[J1].A;
      }

      double H3 = messListe[k2].D;
      double P3 = 1 / messListe[k2].P;
      double S = H1 + H2;
      double T = 2 / H1 + 2 / H2 + 6 * (H1 * H1 * P1 + S * S * P2 + H2 * H2 * P3);

      double R2 = (messListe[k2].Y - messListe[k1].Y) * H2;
      double B2 = 1 / H2 - 6 * H2 * (P2 * S + P3 * (H2 + H3));
      double A3 = 6 * P3 * H2 * H3;
      double Z = 1 / (T - A1 * messListe[k].B - H * messListe[k].A);

      if (k <= N3)
      {
        messListe[k1].A = Z * (B2 - H * messListe[k1].B);
      }

      if (k <= N4)
      {
        messListe[k2].B = Z * A3;
      }

      double R = 6 * (R2 - R1);

      if (k >= 1)
      {
        R = R - H * messListe[J1].C;
      }

      if (k > 1)
      {
        R = R - A1 * messListe[J2].C;
      }

      messListe[k].C = Z * R;
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

    messListe[N1].Y2 = messListe[N2].C;

    if (N4 != 0)
    {
      messListe[N2].Y2 = messListe[N3].C - messListe[N2].A * messListe[N1].Y2;
      if (N3 != 0)
      {
        for (int j = 0; j <= N4; j++)
        {
          int k = N2 - j;
          int k1 = k + 1;
          int k2 = k + 2;
          messListe[k].Y2 = messListe[k - 1].C - messListe[k].A * messListe[k1].Y2 - messListe[k1].B * messListe[k2].Y2;
        }
      }
    }

    H1 = 0;

    for (int k = 0; k <= N1; k++)
    {
      J2 = k + 1;
      messListe[k].C = messListe[k].D;
      H2 = messListe[k].D * (messListe[J2].Y2 - messListe[k].Y2);
      messListe[k].A = H2 / 6;
      messListe[k].D = messListe[k].Y - (H2 - H1) / messListe[k].P;

      messListe[k].B = 0.5 * messListe[k].Y2;
      H1 = H2;
    }

    messListe[N].D = messListe[N].Y + H1 / messListe[N].P;

    for (int k = 0; k <= N1; k++)
    {
      J2 = k + 1;
      H = messListe[k].C;
      messListe[k].C = (messListe[J2].D - messListe[k].D) * H - (messListe[J2].Y2 + 2 * messListe[k].Y2) / (6 * H);
    }
    messListe[N].C = (messListe[N].D - messListe[N1].D) * H + (2 * messListe[N].Y2 + messListe[N1].Y2) / (6 * H);

    return messListe;
  }
}