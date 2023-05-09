using System.Collections.Generic;
using Util;
using System.Numerics;

namespace rt004.checkpoint2
{

    public class Scene {

        public List<Solid> objects {private set;get;}
        public List<LightSrc> lights {private set; get;}
        float[] backgroundColor;

        public Vector3 BackgroundColor {get => new Vector3(backgroundColor[0], backgroundColor[1],backgroundColor[2]);}

        public Scene(float[] background){
            this.objects = new();
            this.lights = new();
            this.backgroundColor = background;
        }
        public void AddSolid(Solid s)
        {
            objects.Add(s);
        }

        public void AddLight(LightSrc light)
        {
            lights.Add(light);
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

    public class Phong
    {
        Vector3 color;
        public readonly float H;
        public readonly float kA, kD, kS; // kA + kD + kS = 1


        public Phong(Vector3 color, float highlight, float kA, float kD, float kS)
        {

            this.kA = kA;
            this.kD = kD;
            this.kS = kS;
            this.H = highlight;
            this.color = color;
        }

        public Vector3 DiffuseComponent(Vector3 intensity, Vector3 light, Vector3 normal)
        {
            var alpha = Math.Max(Vector3.Dot(light, (normal)), 0);
            return color * kD * alpha;
        }
        public Vector3 AmbientLight()
        {
            return color * kA;
        }
        public Vector3 SpecularComponent(Vector3 light, Vector3 unitV, Vector3 unitR)
        {
            var beta = (float)Math.Pow(Vector3.Dot(unitR, unitV), H);
            return light * kS * Math.Max(beta, 0);
        }

    }
    public abstract class ObjectOnMap
    {
        protected Vector3 position;
        public Vector3 Position { get => position; set => position = value; }
    }
    public abstract class Solid : ObjectOnMap
    {
        public Phong? Model { set; get; }
        public abstract Vector3 Normal(Vector3 pos);
        public abstract bool Intersect(Vector3 position, Vector3 direction, out float T);
    }

    public abstract class LightSrc : ObjectOnMap
    {
        protected Vector3 Intensity;
        public abstract Vector3 ComputeLightContrib(Phong model, Vector3 n, Vector3 l, Vector3 v);
    }


    public class PointLightSource : LightSrc
    {
        public PointLightSource(Vector3 pos, Vector3 intensity)
        {
            this.position = pos;
            this.Intensity = intensity;
        }
        public override Vector3 ComputeLightContrib(Phong model, Vector3 n, Vector3 l, Vector3 v)
        {
            var gamma = Vector3.Dot(n, l);
            var R = Vector3.Normalize(2 * n * Math.Max(gamma, 0) - l); // Unit reflection vector
            var diffuse = model.DiffuseComponent(this.Intensity, l, n);
            var specular = model.SpecularComponent(this.Intensity, v, R);
            var E = diffuse + specular;
            return E * Intensity;
        }
    }
    /*
	I want to have a perspective camera
	*/
    public class FloatCamera : ObjectOnMap
    {
        Scene currentScene;
        Vector3 direction;
        float frustrum;
        int dimension;

        public Vector3 Direction { get => direction; set => position = value; }
        public float Frustrum { get => frustrum; set => frustrum = value; }

        public FloatCamera(Scene scene,float x = 0, float y = 0, float z = 0, float a1 = 0, float a2 = 0, float a3 = 0, float frustrum = 0, int dim = 0)
        {
            this.currentScene = scene;
            this.direction = new Vector3(x, y, z);
            this.position = new Vector3(a1, a2, a3);
            this.frustrum = frustrum;

            this.dimension = dim;
        }
        public FloatCamera(Scene scene,Vector3 pos, Vector3 dir, float frustrum = 0)
        {
            this.currentScene = scene;
            this.position = pos;
            this.direction = dir;
            this.frustrum = frustrum;
        }

        private Vector3 upLeft, upRight, downLeft, downRight;
        private float d;

        ///<summary> Does the basic camera setup. It is not in constructor for future possible paralelization</summary>
        public void SetCamera(Logger? logger)
        {
            Vector3 center = this.Position;
            d = this.Direction.Length();
            float fov = this.Frustrum / 2;
            double fovToRadians = fov * (Math.PI / 180);
            var xLength = (float)Math.Tan(fovToRadians) * d;

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

        bool IsShadowed(Vector3 position, Vector3 direction, Solid currentSolid)
        {
            for (int i = 0; i < currentScene.objects.Count; i++)
            {
                Solid solid = currentScene.objects[i];
                if (solid.Intersect(position, direction, out float t) && solid != currentSolid)
                {
                    return true;
                }

            }
            return false;
        }
        const int RayTracingDepth = 10;
        ///<summary>Renders one pixeel from one raycast</summary>
        public Vector3 RenderPixel(int x, int y, int height, int width,  ref int numOfCasts)
        {

            Vector3 top = Vector3.Lerp(upLeft, upRight, x / (float)Math.Max(height, width));
            Vector3 bottom = Vector3.Lerp(downLeft, downRight, x / (float)Math.Max(height, width));
            Vector3 ray = Vector3.Lerp(top, bottom, y / (float)Math.Max(height, width));
            return RayTrace(this.position,ray, ref numOfCasts);

        }

        Vector3 RayTrace(Vector3 position,Vector3 ray, ref int numOfCasts, int depth = 0)
        {
            bool foundInt = false;
            for (int i = 0; i < currentScene.objects.Count && !foundInt; i++)
            {
                Solid solid = currentScene.objects[i];
                Vector3 color = new Vector3();
                if (solid.Intersect(position, ray, out float t))
                {
                    numOfCasts++;
                    foundInt = true;
                    var pt = position + t * ray;
                    var viewVector = Vector3.Normalize(Position - pt);
                    var normalVector = solid.Normal(pt);
                    foreach (var light in currentScene.lights)
                    {
                        var lightVector = Vector3.Normalize(light.Position - pt);
                        Vector3 contrib = Vector3.Zero;
                        if (!IsShadowed(pt, lightVector, solid))
                            contrib = light.ComputeLightContrib(solid.Model!, normalVector, lightVector, viewVector);

                        color += contrib;
                    }
                    color /= (d * d);

                  //  if (++depth >= RayTracingDepth)
                    {
                        color += solid.Model!.AmbientLight();
                        return color;
                    }
                  //  var gamma = Vector3.Dot(n, l);
                 //   var R = Vector3.Normalize(2 * n * Math.Max(gamma, 0) - l); // Unit reflection vector
                 //   color += RenderPixel();
                }

            }
            return currentScene.BackgroundColor;
        }

    }

    public class InfPlane : Solid
    {
        Vector3 normal;
        public InfPlane(Vector3 pos, Vector3 norm, Phong model)
        {
            this.position = pos;
            this.Model = model;
            this.normal = norm;
        }

        public override Vector3 Normal(Vector3 pos) => normal;

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            var p0 = position;
            var p1 = direction;
            float D = (position - this.position).Length();
            var n = Normal(Vector3.One);
            if ((Vector3.Dot(n, p1)) <= float.Epsilon)
            {
                t = 0;
                return false;
            }
            t = -1 * (Vector3.Dot(n, p0) + D) / (Vector3.Dot(n, p1));

            return t >= 0;

        }

    }

    public class Sphere : Solid
    {
        float radius;
        public Sphere(Vector3 pos, Phong model, float radius)
        {
            this.position = pos;
            this.Model = model;
            this.radius = radius;
        }
        public override Vector3 Normal(Vector3 position) => //Vector3.Normalize(2*(position));
                                                            Vector3.Normalize(position - this.position);
        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            var offset = position - this.position;
            var a = Vector3.Dot(direction, direction);
            var b = 2 * (Vector3.Dot(offset, direction));
            var c = Vector3.Dot(offset, offset) - radius * radius;
            var D = b * b - 4 * (a * c);
            var t0 = (float)(-1 * b - Math.Sqrt(D)) / (2 * a);
            var t1 = (float)(-1 * b + Math.Sqrt(D)) / (2 * a);
            if (t0 >= 0)
                t = t0;
            else if (t1 >= 0)
                t = t1;
            else t = 0;
            return t0 >= 0 || t1 >= 0;
        }

    }

}
