using OpenTK.Mathematics;
using System.Text.Json.Serialization;
namespace rt004.checkpoint2
{
    public record struct Hit
    {
        public Vector3d point;
        public Vector3d normal;
        public IMaterial material;
        public double t;
        public Solid solid;
    }
    public interface ObjectOnMap
    {
        public Vector3d Position { set; get; }
    }
    [Serializable]
    public abstract class Solid : ObjectOnMap, IIntersectable
    {
        public double x { set; get; }
        public double y { set; get; }
        public double z { set; get; }
        public IMaterial Model { set; get; }
        public Vector3d Position { get; set; }

        public abstract Vector3d Normal(Vector3d pos);
        public abstract bool Intersect(Vector3d position, Vector3d direction, out Hit T);

    }
    public class InfPlane : Solid
    {
        public InfPlane(IMaterial model)
        {
            this.Position = Vector3d.Zero;
            this.Model = model;
        }

        public override Vector3d Normal(Vector3d pos = default) => Vector3d.UnitY;

        public override bool Intersect(Vector3d position, Vector3d direction, out Hit hitInfo)
        {
            double denominator = Vector3d.Dot(Normal(), direction);
            if (Math.Abs(denominator) > 10e-6)
            {
                double t = Vector3d.Dot(this.Position - position, Normal()) / denominator;
                hitInfo = new Hit() { t = t, material = this.Model, normal = this.Normal(), solid = this, point = position + t * direction };
                return t > 0;
            }
            hitInfo = default;
            return false;
        }
    }
    public class Sphere : Solid
    {
        public double radius { set; get; }
        public Sphere() { }
        public Sphere(IMaterial model)
        {
            radius = 1;
            x = y = z = 0;
            this.Model = model;
        }
        public Sphere(Vector3d pos, IMaterial model, double radius)
        {
            x = pos.X;
            y = pos.Y;
            z = pos.Z;
            this.Position = pos;
            this.Model = model;
            this.radius = radius;
        }
        [JsonConstructorAttribute]
        public Sphere(double x, double y, double z, double radius, Phong model)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.Position = new Vector3d(x, y, z);
            this.radius = radius;
            this.Model = model;
        }

        public override Vector3d Normal(Vector3d position) => Vector3d.Normalize(position - Position);
        public override bool Intersect(Vector3d position, Vector3d direction, out Hit t)
        {
            var offset = position - this.Position;
            var a = Vector3d.Dot(direction, direction);
            var b = 2 * Vector3d.Dot(offset, direction);
            var c = Vector3d.Dot(offset, offset) - radius * radius;
            double D = b * b - 4 * (a * c);
            if(D < 0) {
                t=default;
                return false;
            }
            double t0 = (-1 * b - Math.Sqrt(D)) / (2 * a);
            double t1 = (-1 * b + Math.Sqrt(D)) / (2 * a);

            t = new Hit() { material = this.Model, solid = this };
            if (t0 > 10e-6)
                t.t = t0;
            else if (t1 > 10e-6)
                t.t = t1;
            else { 
                t = default;
                return false;
            }
            t.normal = Normal(position + t.t * direction);
            t.point = position + t.t * direction;
            return t0 > 10e-6 || t1 > 10e-6;
        }

    }
    public class Cylinder : Solid
    {
        public override bool Intersect(Vector3d position, Vector3d direction, out Hit t)
        {
            Vector3d offset = this.Position - position;
            double a = ImageSynthetizer.square(direction.X) + ImageSynthetizer.square(direction.Y);
            double b = 2*(offset.X * direction.X + offset.Y * direction.Y);
            double c = ImageSynthetizer.square(offset.X) + ImageSynthetizer.square(offset.Y) - 1;
            double D = b * b - 4 * (a * c);
            if(D < 0) {
                t=default;
                return false;
            }
            double t0 = (-1 * b - Math.Sqrt(D)) / (2 * a);
            double t1 = (-1 * b + Math.Sqrt(D)) / (2 * a);

            //todo implement algebraic stuff for Cylinder rendering

            t = new Hit() { material = this.Model, solid = this };
            if (t0 > 10e-6)
                t.t = t0;
            else if (t1 > 10e-6)
                t.t = t1;
            else { 
                t = default;
                return false;
            }
            t.normal = Normal(position + t.t * direction);
            t.point = position + t.t * direction;
            return t0 > 10e-6 || t1 > 10e-6;
        }


        public override Vector3d Normal(Vector3d position) => Vector3d.Normalize(position - Position);
    }
}