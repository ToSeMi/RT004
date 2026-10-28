using OpenTK.Mathematics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Util;
namespace rt004.checkpoint2
{
    public interface IMaterial
    {
        Vector3d color { get; }
        Vector3d ShadeMaterial(Vector3d light, Vector3d normal, Vector3d view, Vector3d unitR);
        bool IsReflective();
        bool IsRefractive();
        double GetRefraction(Vector3d viewVector, Vector3d normalVector);
        double RefractionIndex { set; get; }
    }

    [Serializable]
    public class Phong : IMaterial
    {
        public Vector3d color { get => new Vector3d(color1, color2, color3); }
        public double color1 { set; get; }
        public double color2 { set; get; }
        public double color3 { set; get; }
        public double H { private set; get; }
        public double kD { private set; get; }
        public double kS { private set; get; }
        public double RefractionIndex { set; get; }
        bool refl, refr;

        public Phong(Vector3d color, double highlight, double kA, double kD, double kS, double indexOfRefraction)
        {
            this.kD = kD;
            this.kS = kS;
            this.H = highlight;
            this.color1 = color.X;
            this.color2 = color.Y;
            this.color3 = color.Z;
            this.RefractionIndex = indexOfRefraction;
            refl = indexOfRefraction > 1f;
            refr = indexOfRefraction > 1f;
        }

        public Phong(Vector3d color, double highlight, double kA, double kD, double kS, double indexOfRefraction, bool reflective, bool refractive) : this(color, highlight, kA, kD, kS, indexOfRefraction)
        {
            refl = reflective;
            refr = refractive;
        }
        [JsonConstructorAttribute]
        public Phong(double color1, double color2, double color3, double h, double kA, double kD, double kS, double refractionIndex)
        {
            this.color1 = color1;
            this.color2 = color2;
            this.color3 = color3;
            H = h;
            this.kD = kD;
            this.kS = kS;
            RefractionIndex = refractionIndex;
            refl = RefractionIndex > 1f;
            refr = RefractionIndex > 1f;
        }

        public Vector3d ShadeMaterial(Vector3d light, Vector3d normal, Vector3d view, Vector3d unitR)
        {
            return DiffuseComponent(light, normal) + SpecularComponent(light, view, unitR, normal);
        }

        Vector3d DiffuseComponent(Vector3d light, Vector3d normal)
        {
            var alpha = Math.Max(Vector3d.Dot(light, normal), 0);
            return color * kD * alpha;
        }

        public double GetRefraction(Vector3d viewVector, Vector3d normalVector)
        {
            double nom = ImageSynthetizer.square(1 - RefractionIndex);
            double denom = ImageSynthetizer.square(1 + RefractionIndex);
            double c = nom / denom;
            double dotProduct = Vector3d.Dot(viewVector, normalVector);

            return c + (1 - c) * Math.Pow(1 - Math.Max(dotProduct, 0), 5);
        }

        double SchlickAprox(Vector3d V, Vector3d H, Vector3d l, Vector3d n)
        {
            var c = Math.Max(Vector3d.Dot(V, H), 0);
            return c + (1 - c) * Math.Pow(1 - Math.Max(Vector3d.Dot(l, n), 0), 5);
        }
        Vector3d SpecularComponent(Vector3d light, Vector3d unitV, Vector3d unitR, Vector3d normal)
        {
            var addition = light + unitV;
            var h = Vector3d.Normalize(addition);

            var schl = SchlickAprox(unitV, h, light, normal);
            var beta = Math.Pow(Vector3d.Dot(unitR, unitV), H);
            return light * kS * Math.Max(schl * beta, 0);
        }

        public bool IsReflective()
        {
            return refl;
        }

        public bool IsRefractive()
        {
            return refr;
        }
    }
    public struct MaterialInfo
    {
        public Vector3d inVector;
        public Vector3d outVector;
        
    }
}