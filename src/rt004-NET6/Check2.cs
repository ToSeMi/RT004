using System.Collections.Generic;
using Util;
using System.Numerics;

namespace rt004.checkpoint2
{

    public class ImageSynthetizer
    {
        List<Solid> objects;
        List<LightSrc> lights;
        FloatCamera camera;
        int width, height;
        float[] backgroundColor;

        public ImageSynthetizer(int wid, int hei, FloatCamera fc, float[] bgColor)
        {
            objects = new();
            lights = new();
            this.camera = fc;
            this.width = wid;
            this.height = hei;
            this.backgroundColor = bgColor;
        }

        public void AddSolid(Solid s)
        {
            objects.Add(s);
        }

        public void AddLight(LightSrc light)
        {
            lights.Add(light);
        }
        public FloatImage RenderScene()
        {
            FloatImage fi = new FloatImage(width, height, 3);
            Vector3 center = camera.Position;
            Vector3 direction = Vector3.Normalize(camera.Direction);
            double d = Math.Sqrt(Math.Pow(camera.Direction.X, 2) + Math.Pow(camera.Direction.Y, 2) + Math.Pow(camera.Direction.Z, 2));
            float fov = camera.Frustrum / 2;
            double fovToRadians = fov * (Math.PI / 180);
            var xLength = Math.Tan(fovToRadians) * d;

            var right = (float)xLength * Vector3.UnitX;
            var up = Vector3.Cross(right, direction);

            var upLeft = center + camera.Direction - new Vector3((float)xLength,0, 0) + up;
            var upRight = center + camera.Direction + new Vector3((float)xLength,0, 0) + up;
            var downLeft = center + camera.Direction - new Vector3((float)xLength,0, 0) - up;
            var downRight = center + camera.Direction + new Vector3((float)xLength,0, 0) - up;

            float pixelHeight = 1/(float)xLength;
            var ray = upLeft;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, ray = new Vector3(ray.X + pixelHeight, ray.Y, ray.Z))
                {
					bool foundInt = false;
                    if (ray.X > upRight.X)
                    {
                        ray = new Vector3(upLeft.X, ray.Y + pixelHeight, upLeft.Z);
                    }
                    for (int i = 0; i < objects.Count && !foundInt; i++)
                    {
						Solid solid = objects[i];
                        Vector3 color = new Vector3();
						float t;
                        if (solid.Intersect(camera.Position, ray,out t))
                        {
							foundInt = true;
                            var pt = camera.Position + t * ray;
							
                            foreach (var light in lights)
                            {
                                var contrib = light.ComputeLightContrib(solid, camera.Position);
                                color += (contrib * pt);
                            }
                            color += solid.Model!.AmbientLight();
                            fi.PutPixel(x, y, new float[] { color.X, color.Y, color.Z });
                        }
                        else
                        {
                            fi.PutPixel(x, y, backgroundColor);
                        }
                    }

                }
                ray = new Vector3(upLeft.X, ray.Y + pixelHeight, upLeft.Z);
            }

            return fi;
        }
    }

    public class Phong
    {
        Vector3 color;
        public readonly float H = 0.5479f;
        public readonly float kA = 0.3f, kD = 0.3f, kS = 0.4f; // kA + kD + kS = 1


        public Phong(Vector3 color, float highlight, float kA, float kD, float kS)
        {

            this.kA = kA;
            this.kD = kD;
            this.kS = kS;
            this.H = highlight;
            this.color = color;
        }

        public float DiffuseComponent(Vector3 view, Vector3 normal)
        {
            return Vector3.Dot(normal, color) * kD * Vector3.Dot(view, normal);
        }
        public Vector3 AmbientLight()
        {
            return color * kA;
        }
        public float SpecularComponent(Vector3 light, Vector3 unitV, Vector3 unitR)
        {
            return Vector3.Dot(light, color) * kS * (float)Math.Pow(Vector3.Dot(unitR, unitV), H);
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
        public abstract bool Intersect(Vector3 position, Vector3 direction, out float T);
    }

    public abstract class LightSrc : ObjectOnMap
    {
        protected Vector3 Intensity;
        public abstract Vector3 ComputeLightContrib(Solid s, Vector3 center);
    }


    public class PointLightSource : LightSrc
    {

        public PointLightSource(Vector3 pos, Vector3 intensity)
        {
            this.position = pos;
            this.Intensity = intensity;
        }
        public override Vector3 ComputeLightContrib(Solid s, Vector3 center)
        {
            var E = //model?.AmbientLight() 
              s.Model!.DiffuseComponent(Vector3.Normalize(center), Vector3.Normalize(s.Position))
            + s.Model!.SpecularComponent(this.position, Intensity, s.Position);
            return Intensity * E;
        }
    }
    /*
	I want to have a perspective camera
	*/
    public class FloatCamera : ObjectOnMap
    {

        Vector3 direction;
        float frustrum;
        int dimension;

        public Vector3 Direction { get => direction; set => position = value; }
        public float Frustrum { get => frustrum; set => frustrum = value; }

        public FloatCamera(float x = 0, float y = 0, float z = 0, float a1 = 0, float a2 = 0, float a3 = 0, float frustrum = 0, int dim = 0)
        {
            this.direction = new Vector3(x, y, z);
            this.position = new Vector3(a1, a2, a3);
            this.frustrum = frustrum;

            this.dimension = dim;
        }
        public FloatCamera(Vector3 pos, Vector3 dir, float frustrum = 0)
        {
            this.position = pos;
            this.direction = dir;
            this.frustrum = frustrum;
        }

    }

    public class InfPlane : Solid
    {
        public InfPlane(Vector3 pos, Phong model)
        {
            this.position = pos;
            this.Model = model;
        }

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
			
            var p0 = position;
            var p1 = direction;
            var n = Vector3.Normalize(Position);
            if ((Vector3.Dot(n, p1)) <= 10e-32){
                t = 0;
				return false;
			}
			t = -1 * (Vector3.Dot(n, p0)) / (Vector3.Dot(n, p1));

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

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
			#if false
            var P0 = position;
            var P1 = direction;
            var a = Vector3.Dot(P1, P1);
            var b = Vector3.Dot(P1, P0);
            var c = Vector3.Dot(P0, P0) - 1;
            var D = Math.Pow(b, 2) - 4 * (a * c);
            var t0 = (float)(-1 * b + Math.Sqrt(D)) / (2 * a);
            var t1 = (float)(-1 * b - Math.Sqrt(D)) / (2 * a);
			if(t0 >= 0)
				t = t0;
			else if(t1 >= 0)
				t = t1;
			else t = 0;
			return t0 >=0 || t1 >=0;
			#else
			//t0 = (v · p1)
			//D2 = (v · v) - t02
			//t_D^2 = R2 - D2
			var v = this.position - position;
			var t0 = Vector3.Dot(v,direction);
			var D2 = Vector3.Dot(v,v) - t0*t0;
			var tD2 = radius*radius - D2;
			t = 0;
			if(tD2 < 0) return false;
			if(t0 - Math.Sqrt(tD2) >= 0){
				t = t0 - (float)Math.Sqrt(tD2);
			}else {
				t = t0 + (float)Math.Sqrt(tD2);
			}
			return true;
			#endif
        }

    }

}
