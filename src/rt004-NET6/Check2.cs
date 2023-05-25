using System.Collections.Generic;
using Util;
using System.Numerics;
using System;

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
        public readonly float ReflectionIndex;

        public Phong(Vector3 color, float highlight, float kA, float kD, float kS, float indexOfReflection)
        {

            this.kA = kA;
            this.kD = kD;
            this.kS = kS;
            this.H = highlight;
            this.color = color;
            this.ReflectionIndex = indexOfReflection;
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

        float FresnelEquationsSchlickAprox(Vector3 V, Vector3 H, Vector3 l, Vector3 n)
        {
            var c = Vector3.Dot(V, H);
            return c + (1 - c) * MathF.Pow(1 - Vector3.Dot(l, n), 5);
        }
        public Vector3 SpecularComponent(Vector3 light, Vector3 unitV, Vector3 unitR, Vector3 normal)
        {
            var h = (light + unitV) / (light + unitV).Length();

            var schl = FresnelEquationsSchlickAprox(unitV, h, light, normal);
            var beta = MathF.Pow(Vector3.Dot(unitR, unitV), H);
            return light * kS * schl * MathF.Max(beta, 0);
        }

    }
    public interface ObjectOnMap
    {
        public Vector3 Position { get; set; }
    }
    public abstract class Solid : ObjectOnMap
    {
        public Vector3 Position { get; set; }
        public Phong? Model { set; get; }
        public abstract Vector3 Normal(Vector3 pos);
        public abstract bool Intersect(Vector3 position, Vector3 direction, out float T);
    }

    public abstract class LightSrc : ObjectOnMap
    {
        public Vector3 Position { get; set; }
        protected Vector3 Intensity;
        public abstract Vector3 ComputeLightContrib(Phong model, Vector3 n, Vector3 l, Vector3 v);
    }


    public class PointLightSource : LightSrc
    {
        public PointLightSource(Vector3 pos, Vector3 intensity)
        {
            this.Position = pos;
            this.Intensity = intensity;
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

    public interface Camera : ObjectOnMap
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
        public Vector3 Position { set; get; }
        public Scene? currentScene { set; get; }
        public float frustrum { set; get; }
        Vector3 direction;

        public Vector3 Direction { get => direction; set => direction = value; }
        public float Frustrum { get => frustrum; set => frustrum = value; }

        public FloatCamera(Scene scene, float x = 0, float y = 0, float z = 0, float a1 = 0, float a2 = 0, float a3 = 0, float frustrum = 0)
        {
            this.currentScene = scene;
            this.direction = new Vector3(x, y, z);
            this.Position = new Vector3(a1, a2, a3);
            this.frustrum = frustrum;

        }
        public FloatCamera(Scene scene, Vector3 pos, Vector3 dir, float frustrum = 0)
        {
            this.currentScene = scene;
            this.Position = pos;
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

        bool IsShadowed(Vector3 position, Vector3 direction, Solid currentSolid)
        {
            for (int i = 0; i < currentScene!.objects.Count; i++)
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
        public Vector3 RenderPixel(int x, int y, int height, int width, ref int numOfCasts)
        {
            if (x == 307 && y == 247)
            {
                System.Console.WriteLine("breakpoint time");
            }
            Vector3 top = Vector3.Lerp(upLeft, upRight, x / MathF.Max(height, width));
            Vector3 bottom = Vector3.Lerp(downLeft, downRight, x / MathF.Max(height, width));
            Vector3 ray = Vector3.Lerp(top, bottom, y / MathF.Max(height, width));
            return RayTrace(this.Position, ray, ref numOfCasts);

        }
        Vector3 refract(Vector3 light, Vector3 normal, float outIndexRefract)
        {
            float cosAlpha = Vector3.Dot(light, normal);
            float inIndexRefract = 1;
            if (cosAlpha < 0)
            {
                cosAlpha = -cosAlpha;
            }
            else
            {
                float temp = inIndexRefract;
                inIndexRefract = outIndexRefract;
                outIndexRefract = temp; ;
            }
            float n12 = outIndexRefract / inIndexRefract;
            float sinBeta = MathF.Sqrt(1 - n12 * n12 * cosAlpha * cosAlpha);
            if (sinBeta < 0) return Vector3.Zero;
            return n12 * light + (n12 * cosAlpha - sinBeta) * normal;
        }
        float fresnel(Vector3 light, Vector3 normal, float outIndexRefract)
        {
            float cosAlpha = Vector3.Dot(light, normal);
            float inIndexRefract = 1;
            if (cosAlpha < 0)
            {
                cosAlpha = -cosAlpha;
            }
            else
            {
                float temp = inIndexRefract;
                inIndexRefract = outIndexRefract;
                outIndexRefract = temp; ;
            }
            float sinBeta = outIndexRefract / inIndexRefract * MathF.Sqrt(MathF.Max(0, 1 - cosAlpha * cosAlpha));
            if (sinBeta >= 1f) return 1;
            float cosBeta = MathF.Sqrt(1 - sinBeta * sinBeta);
            float Rs = ((outIndexRefract * cosAlpha) - (inIndexRefract * cosBeta)) / ((outIndexRefract * cosAlpha) + (inIndexRefract * cosBeta));
            float Rp = ((inIndexRefract * cosAlpha) - (outIndexRefract * cosBeta)) / ((inIndexRefract * cosAlpha) + (outIndexRefract * cosBeta));

            return (Rs * Rs + Rp * Rp) / 2;
        }

        Vector3 RayTrace(Vector3 position, Vector3 ray, ref int numOfCasts, int depth = 0, Solid? currentSolid = null)
        {
            bool foundInt = false;
            for (int i = 0; i < currentScene!.objects.Count && !foundInt; i++)
            {
                Solid solid = currentScene.objects[i];
                Vector3 color = new Vector3();
                if (solid.Intersect(position, ray, out float t) && solid != currentSolid)
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

                        color += contrib / (d * d);

                    }

                    var gamma = Vector3.Dot(normalVector, viewVector);
                    var R = Vector3.Normalize(2 * normalVector * MathF.Max(gamma, 0) - viewVector); // Unit reflection vector
                    color += solid.Model!.AmbientLight();
                    var kr = fresnel(viewVector,normalVector,solid.Model!.ReflectionIndex);
                    if (depth + 1 < RayTracingDepth)
                    {
                        color +=  kr* RayTrace(pt, R, ref numOfCasts, depth + 1, solid) + 
                                  (1-kr)*  RayTrace(pt,refract(viewVector,normalVector,solid.Model!.ReflectionIndex),ref numOfCasts,depth+1,solid);
                    }
                    return color;
                }

            }
            if (depth == 0) return currentScene.BackgroundColor;
            return Vector3.Zero;
        }

    }

    public class InfPlane : Solid
    {
        Vector3 normal;
        public InfPlane(Vector3 pos, Vector3 norm, Phong model)
        {
            this.Position = pos;
            this.Model = model;
            this.normal = norm;
        }

        public override Vector3 Normal(Vector3 pos) => normal;

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            var p0 = position;
            var p1 = direction;
            float D = (position - this.Position).Length();
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
            this.Position = pos;
            this.Model = model;
            this.radius = radius;
        }
        public override Vector3 Normal(Vector3 position) => //Vector3.Normalize(2*(position));
                                                            Vector3.Normalize(position - this.Position);
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
