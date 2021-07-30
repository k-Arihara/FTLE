using System;
using System.Numerics;


/*
Using MAC Grid
*/

namespace FTLE
{
  public class FTLE
  {
    const int FORWARD = 1;
    const int BACKWARD = -1;

    const int fileNum = 1000;

    const int velResolution = 32;
    const int ftleResolution = 64;

    const int direction = BACKWARD;

    const int delta_t = 1;
    const int integral_T = 5;
    const float perturbation = 0.1f;

    Vector3[][,,] velocityField;
    float[][,,] ftleField;

    int x_min, x_max, y_min, y_max;

    public FTLE()
    {
      velocityField = new Vector3[fileNum][,,];
      ftleField = new float[fileNum][,,];
      x_min = 0; y_min = 0;
      x_max = ftleResolution - 1; y_max = ftleResolution - 1;

      for (int n = 0; n < fileNum; n++)
      {
        string path = string.Format("./data1/vel-{0}.txt", n);
        velocityField[n] = FileIO.ReadFile(path);
      }

      int time = 500;
      ftleField[time] = FTLE2D(time);

      FileIO.WriteFile2D(time, ftleField[time]);
    }

    float[,,] FTLE2D(int t)
    {
      float[,,] ftle = new float[ftleResolution, ftleResolution, 1];
      Vector3[,,] pos = new Vector3[ftleResolution, ftleResolution, 1];
      Vector3[,,] new_pos = new Vector3[ftleResolution, ftleResolution, 1];
      bool[,,] leftDomain = new bool[ftleResolution, ftleResolution, 1];
      bool[,,] calcFTLE = new bool[ftleResolution, ftleResolution, 1];
      for (int ix = 0; ix < ftleResolution; ix++)
      {
        for (int iy = 0; iy < ftleResolution; iy++)
        {
          pos[ix, iy, 0] = new Vector3(ix, iy, 0);
          new_pos[ix, iy, 0] = new Vector3(ix, iy, 0);
          leftDomain[ix, iy, 0] = false;
          calcFTLE[ix, iy, 0] = false;
        }
      }

      for (int t_integration = 0; t_integration < integral_T - delta_t; t_integration = t_integration + delta_t)
      {
        int t0 = t + t_integration * direction;
        int t1 = t0 + delta_t * direction;

        if (t0 < 0) continue;

        for (int x = 0; x < ftleResolution; x++)
        {
          for (int y = 0; y < ftleResolution; y++)
          {
            new_pos[x, y, 0] = Trace2D(t0, t1, delta_t, pos[x, y, 0]);
            // Console.WriteLine(new_pos[x, y, 0]);
          }
        }

        for (int x = 0; x < ftleResolution; x++)
        {
          for (int y = 0; y < ftleResolution; y++)
          {
            if (leftDomain[x, y, 0] == false)
            {
              if (((new_pos[x, y, 0].X - x_min) * (new_pos[x, y, 0].X - x_max)) >= 0 || ((new_pos[x, y, 0].Y - y_min) * (new_pos[x, y, 0].Y - y_max)) >= 0)
              {
                leftDomain[x, y, 0] = true;
                if (calcFTLE[x, y, 0] == false)
                {
                  ftle[x, y, 0] = CalculateFTLE(pos, x, y);
                  calcFTLE[x, y, 0] = true;

                  if (x > x_min && calcFTLE[x - 1, y, 0] == false)
                  {
                    ftle[x - 1, y, 0] = CalculateFTLE(pos, x - 1, y);
                    calcFTLE[x - 1, y, 0] = true;
                  }
                  if (x < x_max && calcFTLE[x + 1, y, 0] == false)
                  {
                    ftle[x + 1, y, 0] = CalculateFTLE(pos, x + 1, y);
                    calcFTLE[x + 1, y, 0] = true;
                  }
                  if (y > y_min && calcFTLE[x, y - 1, 0] == false)
                  {
                    ftle[x, y - 1, 0] = CalculateFTLE(pos, x, y - 1);
                    calcFTLE[x, y - 1, 0] = true;
                  }
                  if (y < y_max && calcFTLE[x, y + 1, 0] == false)
                  {
                    ftle[x, y + 1, 0] = CalculateFTLE(pos, x, y + 1);
                    calcFTLE[x, y + 1, 0] = true;
                  }
                }
              }
            }
            else
            {
              new_pos[x, y, 0] = pos[x, y, 0];
            }
          }
        }

        for (int x = 0; x < ftleResolution; x++)
        {
          for (int y = 0; y < ftleResolution; y++)
          {
            if (leftDomain[x, y, 0] == false)
            {
              pos[x, y, 0] = new_pos[x, y, 0];
            }
          }
        }
      }

      for (int x = 0; x < ftleResolution; x++)
      {
        for (int y = 0; y < ftleResolution; y++)
        {
          if (calcFTLE[x,y,0] == false)
          {
            ftle[x, y, 0] = CalculateFTLE(pos, x, y);
            calcFTLE[x, y, 0] = true;
          }
        }
      }
      return ftle;
    }

    float CalculateFTLE(Vector3[,,] flowmap, int ix, int iy)
    {
      if (((ix - 1) * (ix - x_max)) < 0 && ((iy - 1) * (iy - y_max) < 0))
      {
        float a00 = (flowmap[ix + 1, iy, 0].X - flowmap[ix - 1, iy, 0].X) / 2;
        float a01 = (flowmap[ix, iy + 1, 0].X - flowmap[ix, iy - 1, 0].X) / 2;
        float a10 = (flowmap[ix + 1, iy, 0].Y - flowmap[ix - 1, iy, 0].Y) / 2;
        float a11 = (flowmap[ix, iy + 1, 0].Y - flowmap[ix, iy - 1, 0].Y) / 2;

        float[,] tensor2D = new float[2, 2];
        tensor2D[0, 0] = (float)Math.Pow(a00, 2) + (float)Math.Pow(a01, 2);
        tensor2D[1, 0] = tensor2D[0, 1] = a00 * a01 + a10 * a11;
        tensor2D[1, 1] = (float)Math.Pow(a11, 2) + (float)Math.Pow(a10, 2);

        double result = (float)Math.Log(Eigen.GetMaxEigenValue2x2(tensor2D)) / Math.Abs(integral_T / delta_t);
        
        if(result != double.NaN) return (float)result;
        else return -1;
      }
      else
      {
        return 0;
      }
    }

    Vector3 Trace2D(int t_start, int t_end, float h, Vector3 pos)
    {
      int delta = (t_end - t_start);

      // Console.WriteLine(pos);
      // Vector3 velocity = Lerp2D(t_start, pos);
      Vector3 velocity = RungeKutta(t_start, pos);
      return pos + velocity * direction * delta * h;

      Vector3 RungeKutta(int t, Vector3 pos)
      {
        float dt = delta_t * direction;
        Vector3 k1 = Lerp2D(t, pos);
        Vector3 x2 = pos + k1 * dt / 2;
        Vector3 k2 = Lerp2D((int)(t + dt / 2), x2);
        Vector3 x3 = pos + k2 * dt / 2;
        Vector3 k3 = Lerp2D((int)(t + dt / 2), x3);
        Vector3 x4 = pos + k3 * dt;
        Vector3 k4 = Lerp2D((int)(t + dt), x4);
        return (k1 + k2 + k3 + k4) / 6;
      }
    }

    Vector3 Lerp2D(int t, Vector3 pos)
    {
      float x = pos.X / (ftleResolution - 1) * (velResolution - 1);
      float y = pos.Y / (ftleResolution - 1) * (velResolution - 1);
      int x0 = (int)Math.Round(x);
      int y0 = (int)Math.Round(y);
      int neiborPoint = GetNeighborPoint(x0, y0);

      // Console.WriteLine("x : {0}   y : {1}", x, y);
      // Console.WriteLine("x0 : {0}    y0 : {1}", x0, y0);
      // Console.WriteLine("p:{0}", neiborPoint);
      if (neiborPoint == 3)
      {
        int x1, y1;
        if ((x - x0) < 0)
          x1 = x0 - 1;
        else
          x1 = x0 + 1;

        if ((y - y0) < 0)
          y1 = y0 - 1;
        else
          y1 = y0 + 1;

        Vector3 v00 = velocityField[t][y0, x0, 0];
        Vector3 v10 = velocityField[t][y0, x1, 0];
        Vector3 v01 = velocityField[t][y1, x0, 0];

        float delta_x = Math.Abs(x - x1);
        float delta_y = Math.Abs(y - y1);
        float dx = Math.Abs(x1 - x0);
        float dy = Math.Abs(y1 - y0);

        return v00 + (v10 - v00) / dx * delta_x + (v01 - v00) / dy * delta_y;
      }

      if (neiborPoint == 2)
      {
        int x1, y1;
        if (x0 <= 0 || (velResolution - 1) <= x0)
        {
          if (x0 <= 0)
          {
            x0 = 0;
            x1 = 1;
          }
          else
          {
            x0 = velResolution - 1;
            x1 = velResolution - 2;
          }

          if ((y0 - y) < 0)
            y1 = y0 + 1;
          else
            y1 = y0 - 1;
        }
        else
        {
          if (y0 <= 0)
          {
            y0 = 0;
            y1 = 1;
          }
          else
          {
            y0 = velResolution - 1;
            y1 = velResolution - 2;
          }

          if ((x0 - x) < 0)
            x1 = x0 + 1;
          else
            x1 = x0 - 1;
        }
        Vector3 v00 = velocityField[t][x0, y0, 0];
        Vector3 v01 = velocityField[t][x0, y1, 0];
        Vector3 v10 = velocityField[t][x1, y0, 0];

        float delta_x = Math.Abs(x - x1);
        float delta_y = Math.Abs(y - y1);
        float dx = Math.Abs(x1 - x0);
        float dy = Math.Abs(y1 - y0);

        // Console.WriteLine("x0 : {0}, y0 : {1}", x0, y0);
        // Console.WriteLine("pos.X : {0}, x1 : {1}", pos.X, x1);
        // Console.WriteLine("pos.Y : {0}, y1 : {1}", pos.Y, y1);
        // Console.WriteLine(v01);
        // Console.WriteLine("x0:{0} y0:{1} x1:{2} y1:{3}", x0, y0, x1, y1);

        // return v00 + (v00 - v10) / dx * delta_x + (v00 - v01) / dy * delta_y;
        return Vector3.Zero;
      }

      // if (neiborPoint == 1)
      else
      {
        int x1, y1;
        if (x0 <= 0)
        {
          x0 = 0;
          x1 = 1;
        }
        else
        {
          x0 = velResolution - 1;
          x1 = velResolution - 2;
        }
        if (y0 <= 0)
        {
          y0 = 0;
          y1 = 1;
        }
        else
        {
          y0 = velResolution - 1;
          y1 = velResolution - 2;
        }

        Vector3 v00 = velocityField[t][x0, y0, 0];
        Vector3 v01 = velocityField[t][x0, y1, 0];
        Vector3 v10 = velocityField[t][x1, y0, 0];

        float delta_x = Math.Abs(x - x1);
        float delta_y = Math.Abs(y - y1);
        float dx = Math.Abs(x1 - x0);
        float dy = Math.Abs(y1 - y0);

        // return v00 + (v00 - v10) / dx * delta_x + (v00 - v01) / dy * delta_y;
        return Vector3.Zero;
      }
      // return Vector3.Zero;

      /*
      velocity field
      1 2 2 ... 2 2 1
      2 3 3 ... 3 3 2
      2 3 3 ... 3 3 2
      . . . ... . . .
      2 3 3 ... 3 3 2
      2 3 3 ... 3 3 2
      1 2 2 ... 2 2 1
      */
      int GetNeighborPoint(int x0, int y0)
      {
        if (0 < x0 && x0 < (velResolution - 1) && 0 < y0 && y0 < (velResolution - 1)) return 3;
        else if ((0 < x0 && x0 < (velResolution - 1)) || 0 < y0 && y0 < (velResolution - 1)) return 2;
        else return 1;
      }
    }
  }
}
