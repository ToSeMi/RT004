using System.Collections.Generic;
using Util;
using System.Numerics;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace rt004.checkpoint2
{

    public class Scene
    {

        public List<Solid> objects { private set; get; }
        public List<LightSrc> lights { private set; get; }
        float[] backgroundColor;

        public Vector3 BackgroundColor { get => new Vector3(backgroundColor[0], backgroundColor[1], backgroundColor[2]); }

        public Scene(float[] background)
        {
            this.objects = new();
            this.lights = new();
            this.backgroundColor = background;
        }
        public void AddSolid(Solid s)
        {
            objects.Add(s);
        }

        internal void SortSolidsByView(Vector3 viewVector) {
            objects.Sort(new SolidComparer(viewVector));
        }

        public void AddLight(LightSrc light)
        {
            lights.Add(light);
        }

        public void SaveToFile(string fileName) {
            string savable = JsonSerializer.Serialize(this);
            File.WriteAllText(fileName,savable);
        }

        class SolidComparer : IComparer<Solid>
        {
            Vector3 viewPosition;
            public SolidComparer(Vector3 viewPos) {
                this.viewPosition = viewPos;
            }
            public int Compare(Solid? x, Solid? y)
            {
                var xL = (x!.Position - viewPosition).Length();
                var yL = (y!.Position - viewPosition).Length();

                return xL.CompareTo(yL); 
            }
        }
    }
    public class ImageSynthetizer
    {
        FloatCamera camera;
        int width, height;
        Logger? logger;

        public ImageSynthetizer(int wid, int hei, FloatCamera fc, Logger? log = null)
        {
            this.camera = fc;
            this.width = wid;
            this.height = hei;
            this.logger = log;
        }
        //adding this one for loading from a file
        /*public ImageSynthetizer(int wid, int hei, FloatCamera fc, float[] bgColor, List<Solid> objects, List<LightSrc> lights) : this(wid, hei, fc, bgColor)
        {
            this.objects = objects;
            this.lights = lights;
        }*/

        ///<summary>Renders the scene with the classic shooting ray technique</summary>
        ///<returns>Image with the perspective</returns>
        public FloatImage RenderScene()
        {
            logger?.DoLog($"Starting at {DateTime.Now}");
            FloatImage fi = new FloatImage(width, height, 3);
            camera.SetCamera(logger);
            camera.currentScene!.SortSolidsByView(camera.Position);
            int numOfCasts = 0; //DEBUG ONLY
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = camera.RenderPixel(x, y, height, width, ref numOfCasts);
                    fi.PutPixel(x, y, new float[] { color.X, color.Y, color.Z });

                }
            }
            logger?.DoLog($"numOfCasts={numOfCasts}");
            logger?.DoLog("------------------------------------------");
            return fi;
        }
    }

    [Serializable]
    public class Phong
    {
        Vector3 color {get => new Vector3(color1, color2, color3);}
        public float color1 { set; get;}
        public float color2 { set; get;}
        public float color3 { set; get;}
        public float H {private set;get;}
        public float kA {private set;get;} 
        public float kD {private set;get;} 
        public float kS {private set;get;} 
        public float RefractionIndex {private set;get;}
        bool refl, refr;
        
        public Phong(Vector3 color, float highlight, float kA, float kD, float kS, float indexOfRefraction)
        {

            this.kA = kA;
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

        public Phong(Vector3 color, float highlight, float kA, float kD, float kS, float indexOfRefraction, bool reflective, bool refractive) : this(color,highlight,kA,kD,kS,indexOfRefraction) {
            refl = reflective;
            refr = refractive;
        }
        [JsonConstructorAttribute]
        public Phong(float color1, float color2, float color3, float h, float kA, float kD, float kS, float refractionIndex)
        {
            this.color1 = color1;
            this.color2 = color2;
            this.color3 = color3;
            H = h;
            this.kA = kA;
            this.kD = kD;
            this.kS = kS;
            RefractionIndex = refractionIndex;
            refl = RefractionIndex > 1f;
            refr = RefractionIndex > 1f;
        }

        public Vector3 DiffuseComponent(Vector3 intensity, Vector3 light, Vector3 normal)
        {
            var alpha = MathF.Max(Vector3.Dot(light, (normal)), 0);
            return color * kD * alpha;
        }
        public Vector3 AmbientLight()
        {
            return color * kA;
        }

        public float Shade(Vector3 viewVector, Vector3 normalVector) {
            var nom = (RefractionIndex - 1) * (RefractionIndex -1);
            var denom = (RefractionIndex + 1) * (RefractionIndex +1);
            var c = nom/denom;
            return c + (1 - c) * MathF.Pow(1 - Vector3.Dot(viewVector, normalVector), 5);
        }

        float SchlickAprox(Vector3 V, Vector3 H, Vector3 l, Vector3 n)
        {
            var c = MathF.Max(Vector3.Dot(V, H),0);
            return c + (1 - c) * MathF.Pow(1 - MathF.Max(Vector3.Dot(l, n),0), 5);
        }
        public Vector3 SpecularComponent(Vector3 light, Vector3 unitV, Vector3 unitR, Vector3 normal)
        {
            var addition = light + unitV;
            var h = Vector3.Normalize(addition);

            var schl = SchlickAprox(unitV, h, light, normal);
            var beta = MathF.Pow(Vector3.Dot(unitR, unitV), H);
            return light * kS * MathF.Max(schl*beta, 0);
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
    public abstract class ObjectOnMap
    {
        public Vector3 Position;
    }
    [Serializable]
    public abstract class Solid : ObjectOnMap
    {
        public  float x {set;get;}
        public  float y {set;get;}
        public float z {set;get;}
        public Phong? Model { set; get; }
        public abstract Vector3 Normal(Vector3 pos);
        public abstract bool Intersect(Vector3 position, Vector3 direction, out float T);

    }
    [Serializable]
    public abstract class LightSrc : ObjectOnMap 
    {
        protected Vector3 Intensity;
        public abstract Vector3 ComputeLightContrib(Phong model, Vector3 n, Vector3 l, Vector3 v);

    }

    [Serializable]
    public class PointLightSource : LightSrc
    {
        
        public float iX {set;get;}
        public float iY{set;get;}
        public float iZ{set;get;}
        public float x{set;get;}
        public float y{set;get;}
        public float z{set;get;}
        public PointLightSource(Vector3 pos, Vector3 intensity)
        {
            this.Position = pos;
            this.Intensity = intensity;
            x=pos.X;
            y=pos.Y;
            z=pos.Z;
            iX=intensity.X;
            iY=intensity.Y;
            iZ=intensity.Z;
        }
        [JsonConstructorAttribute]
        public PointLightSource(float iX, float iY, float iZ, float x, float y, float z)
        {
            this.iX = iX;
            this.iY = iY;
            this.iZ = iZ;
            this.x = x;
            this.y = y;
            this.z = z;
            Position =new Vector3(x,y,z);
            Intensity = new Vector3(iX,iY,iZ);
        }

        public override Vector3 ComputeLightContrib(Phong model, Vector3 n, Vector3 l, Vector3 v)
        {
            var gamma = Vector3.Dot(n, l);
            var R = Vector3.Normalize(2 * n * MathF.Max(gamma, 0) - l); // Unit reflection vector
            var diffuse = model.DiffuseComponent(this.Intensity, l, n);
            var specular = model.SpecularComponent(this.Intensity, v, R, n);
            var E = diffuse + specular;
            return E * Intensity;
        }
    }

    public abstract class Camera : ObjectOnMap
    {
        Scene? currentScene { set; get; }
        Vector3 Direction { set; get; }
        float frustrum { set; get; }
    }

    /*
	I want to have a perspective camera
	*/
    public class FloatCamera : Camera
    {
        public Scene? currentScene { set; get; }
        public float frustrum { set; get; }
        Vector3 direction;
        readonly int RayTracingDepth;

        public Vector3 Direction { get => direction; set => direction = value; }
        public float Frustrum { get => frustrum; set => frustrum = value; }

        public FloatCamera(Scene scene, float x = 0, float y = 0, float z = 0, float a1 = 0, float a2 = 0, float a3 = 0, float frustrum = 0, int rtDepth = 10)
        {
            this.currentScene = scene;
            this.direction = new Vector3(x, y, z);
            this.Position = new Vector3(a1, a2, a3);
            this.frustrum = frustrum;
            this.RayTracingDepth = rtDepth;
        }
        public FloatCamera(Scene scene, Vector3 pos, Vector3 dir, float frustrum = 0,int rtDepth = 10)
        {
            this.currentScene = scene;
            this.Position = pos;
            this.direction = dir;
            this.frustrum = frustrum;
            this.RayTracingDepth = rtDepth;
        }

        private Vector3 upLeft, upRight, downLeft, downRight;
        private float d;
        
        ///<summary> Does the basic camera setup. It is not in constructor for future possible paralelization</summary>
        public void SetCamera(Logger? logger)
        {
            Vector3 center = this.Position;
            d = this.Direction.Length();
            float fov = this.Frustrum / 2;
            float fovToRadians = fov * (MathF.PI / 180);
            var xLength = MathF.Tan(fovToRadians) * d;

            Vector3 direction = this.Direction;
            var right = Vector3.Normalize((float)xLength * -1 * Vector3.UnitX);

            var up = Vector3.Cross(right, Vector3.Normalize(direction));

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
        Random r = new Random(123); //for deterministic purposes
        const int numberOfSamples =480;
        bool IsShadowed(Vector3 position, Vector3 direction, Solid currentSolid)
        {
            foreach (var solid in currentScene!.objects)
            {
                if (solid.Intersect(position, direction, out float t) && solid != currentSolid)
                {
                    return true;
                }

            }
            return false;
        }
        const float constant = 0.01f;
        ///<summary>Renders one pixeel from one raycast</summary>
        public Vector3 RenderPixel(int x, int y, int height, int width, ref int numOfCasts)
        {
            Vector3 top = Vector3.Lerp(upLeft, upRight, x / MathF.Max(height, width));
            Vector3 bottom = Vector3.Lerp(downLeft, downRight, x / MathF.Max(height, width));
            Vector3 ray = Vector3.Lerp(top, bottom, y / MathF.Max(height, width));
            Vector3 avg = new Vector3();
            for(int i = 0; i < numberOfSamples; ++i) {
                avg += RayTrace(this.Position, ray+ constant * new Vector3((float)r.NextDouble(), 
                                                (float)r.NextDouble(), 
                                                (float)r.NextDouble()), ref numOfCasts);
            }
            return avg/numberOfSamples;

        }
        float pow(float input) {
            return input*input;
        }
        Vector3 refract(Vector3 light, Vector3 normal, float outIndexRefract)
        {
            float cosAlpha = Math.Clamp(Vector3.Dot(light, normal),-1,1) ;
            float inIndexRefract = 1;

            float n12 =  outIndexRefract / inIndexRefract; 

            if(n12<1)return Vector3.Zero;

            float sinBeta = MathF.Sqrt(1 - (pow(n12) * (1-pow(cosAlpha)))); //1−n12^2⋅(1−(n⋅l)^2)
             
            if (sinBeta < 0) return Vector3.Zero;
            return (n12 * cosAlpha -sinBeta)* normal - n12*light;
        }


        Vector3 reflect(Vector3 normalVector, Vector3 viewVector) {
            var gamma = Vector3.Dot(normalVector, viewVector);
            return Vector3.Normalize(2 * normalVector * MathF.Max(gamma, 0) - viewVector); // Unit reflection vector
        }
        Vector3 RayTrace(Vector3 position, Vector3 ray, ref int numOfCasts, int depth = 0, Solid? currentSolid = null)
        {
            foreach (var solid in currentScene!.objects)
            {
                Vector3 color = new Vector3();
                if (solid.Intersect(position, ray, out float t) && solid != currentSolid)
                {
                    numOfCasts++;
                    var pt = position + t * ray;
                    var viewVector = Vector3.Normalize(Position - pt);
                    var normalVector = solid.Normal(pt);
                    foreach (var light in currentScene.lights)
                    {
                        var lightVector = Vector3.Normalize(light.Position - pt);
                        Vector3 contrib = Vector3.Zero;
                        if (!IsShadowed(pt, lightVector, solid) || currentSolid != null)
                            contrib = light.ComputeLightContrib(solid.Model!, normalVector, lightVector, viewVector);

                        color += contrib ;

                    }
                    color += solid.Model!.AmbientLight();
                    var kr = solid.Model!.Shade(viewVector,normalVector);
                      
                    if (depth + 1 < RayTracingDepth)
                    {
                        if(solid.Model.IsReflective()) {

                            var reflected = reflect(normalVector,viewVector);
                            color +=  kr* RayTrace(pt, reflected, ref numOfCasts, depth + 1, solid);
                        }
                        if(solid.Model.IsRefractive()) {
                            var refracted = refract(viewVector,normalVector,solid.Model!.RefractionIndex);
                            color += (1-kr) * RayTrace(pt,refracted,ref numOfCasts,depth + 1,solid);
                        }
                        
                    }
                    return color/ (0.5f* d * d + 0.4f*d + 0.1f);
                }

            }
            return currentScene.BackgroundColor;
        }

    }
    [Serializable]
    public class InfPlane : Solid
    {
        public float nx {set;get;}
        public float ny {set;get;}
        public float nz {set;get;}
        public Vector3 normal {get => new Vector3(nx,ny,nz);}
        public InfPlane(Vector3 pos, Vector3 norm, Phong model)
        {
            this.Position = pos;
            this.Model = model;
            x=pos.X;
            y=pos.Y;
            z=pos.Z;
            nx = norm.X;
            ny = norm.Y;
            nz = norm.Z;
        }
        [JsonConstructorAttribute]
        public InfPlane(float x, float y, float z, float nx,float ny, float nz, Phong model) {
            this.x = x;
            this.y=y;
            this.z=z;
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
            this.Model = model;
            Position = new Vector3(x,y,z);
        }

        public override Vector3 Normal(Vector3 pos) => normal;

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            float denominator = Vector3.Dot(normal,direction);
            if (MathF.Abs(denominator) > 0.0000001f)
            {
                t = Vector3.Dot(this.Position - position, normal) / denominator;
                return t >= 0;
            }
            t = 0;
            return false;
        }
    }
    [Serializable]
    public class Sphere : Solid
    {
        public float radius {set;get;}
        public Sphere() {}
        public Sphere(Vector3 pos, Phong model, float radius)
        {
            x=pos.X;
            y=pos.Y;
            z=pos.Z;
            this.Position = pos;
            this.Model = model;
            this.radius = radius;
        }
        [JsonConstructorAttribute]
        public Sphere(float x, float y, float z, float radius, Phong model)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.Position = new Vector3(x,y,z);
            this.radius = radius;
            this.Model = model;
        }

        public override Vector3 Normal(Vector3 position) => Vector3.Normalize(position - this.Position);
        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            var offset = position - this.Position;
            var a = Vector3.Dot(direction, direction);
            var b = 2 * (Vector3.Dot(offset, direction));
            var c = Vector3.Dot(offset, offset) - radius * radius;
            var D = b * b - 4 * (a * c);
            var t0 = (float)(-1 * b - MathF.Sqrt(D)) / (2 * a);
            var t1 = (float)(-1 * b + MathF.Sqrt(D)) / (2 * a);
            if (t0 >= 0)
                t = t0;
            else if (t1 >= 0)
                t = t1;
            else t = 0;
            return t0 >= 0 || t1 >= 0;
        }

    }

}
