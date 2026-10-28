using OpenTK.Mathematics;
using System.Text.Json.Serialization;
namespace rt004.checkpoint2
{
    [Serializable]
    public abstract class LightSrc
    {
        protected Vector3d Intensity;
        public abstract Vector3d ComputeLightContrib(IMaterial model, Vector3d n, Vector3d l, Vector3d v);

    }
    public class AmbientLightSource : LightSrc
    {
        double kA;
        public AmbientLightSource(double kA)
        {
            this.kA = kA;
        }
        public override Vector3d ComputeLightContrib(IMaterial model, Vector3d n, Vector3d l, Vector3d v)
        {
            return kA * model.color;
        }
    }
    [Serializable]
    public class PointLightSource : LightSrc, ObjectOnMap
    {
        public Vector3d Position { set; get; }

        public PointLightSource(Vector3d pos, Vector3d intensity)
        {
            this.Position = pos;
            this.Intensity = intensity;
        }
        [JsonConstructorAttribute]
        public PointLightSource(double iX, double iY, double iZ, double x, double y, double z)
        {
            Position = new Vector3d(x, y, z);
            Intensity = new Vector3d(iX, iY, iZ);
        }

        public override Vector3d ComputeLightContrib(IMaterial model, Vector3d n, Vector3d pt, Vector3d v)
        {
            var l = Vector3d.Normalize(Position - pt);
            var gamma = Vector3d.Dot(n, l);
            var R = Vector3d.Normalize(2 * n * Math.Max(gamma, 0) - l); // Unit reflection vector
            //var diffuse = model.DiffuseComponent(this.Intensity, l, n);
            //var specular = model.SpecularComponent(this.Intensity, v, R, n);
            var E = model.ShadeMaterial(l, n, v, R); //diffuse + specular;
            return E * Intensity;
        }
    }
}