using System.Collections.Generic;
using Util;
//using System.Numerics;
using OpenTK.Mathematics;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
//using rt004.checkpoint4;
using System.Drawing;
using System.Numerics;

namespace rt004.checkpoint2
{

    #region cameras
    public abstract class Camera : ObjectOnMap
    {
        public Vector3d Position { get; set; }
        Scene? currentScene { set; get; }
        Vector3d Direction { set; get; }
        double frustrum { set; get; }
    }

    /*
	I want to have a perspective camera
	*/
    public class FloatCamera : Camera
    {
        public double Frustrum { set; get; }
        Vector3d direction;

        public Vector3d Direction { get => direction; set => direction = value; }

        public FloatCamera(double x = 0, double y = 0, double z = 0, double a1 = 0, double a2 = 0, double a3 = 0, double frustrum = 0)
        {
            this.direction = new Vector3d(x, y, z);
            this.Position = new Vector3d(a1, a2, a3);
            this.Frustrum = frustrum;
        }
        public FloatCamera( Vector3d pos, Vector3d dir, double frustrum = 0)
        {
            this.Position = pos;
            this.direction = dir;
            this.Frustrum = frustrum;
        }

        private Vector3d upLeft, upRight, downLeft, downRight;
        private double d;

        ///<summary> Does the basic camera setup. It is not in constructor for future possible paralelization</summary>
        public void SetCamera(Logger? logger)
        {
            Vector3d center = this.Position;
            d = this.Direction.Length;
            double fov = this.Frustrum / 2;
            double fovToRadians = fov * (Math.PI / 180);
            var xLength = Math.Tan(fovToRadians) * d;

            Vector3d direction = this.Direction;
            var right = Vector3d.Normalize((double)xLength * -1 * Vector3d.UnitX);

            var up = Vector3d.Cross(right, Vector3d.Normalize(direction));

            var perspectiveCenter = center + direction;
            upLeft = perspectiveCenter + right + up;
            upRight = perspectiveCenter - right + up;
            downLeft = perspectiveCenter + right - up;
            downRight = perspectiveCenter - right - up;
            logger?.DoLog($"center={center}");
            logger?.DoLog($"perspectiveCenter={perspectiveCenter}");
            logger?.DoLog($"direction={this.Direction}");
            logger?.DoLog($"d={d}");
            logger?.DoLog($"fov={fov}");
            logger?.DoLog($"xLength={xLength}");
            logger?.DoLog($"up={up}");
            logger?.DoLog($"upLeft={upLeft}");
            logger?.DoLog($"upRight={upRight}");
            logger?.DoLog($"downLeft={downLeft}");
            logger?.DoLog($"downRight={downRight}");
        }

        public Vector3d GetRay(double x, double y, double height, double width)
        {
            Vector3d top = Vector3d.Lerp(upLeft, upRight, x / Math.Max(height, width));
            Vector3d bottom = Vector3d.Lerp(downLeft, downRight, x / Math.Max(height, width));
            Vector3d ray = Vector3d.Lerp(top, bottom, y / Math.Max(height, width));
            return ray;
        }


    }
    #endregion
}
