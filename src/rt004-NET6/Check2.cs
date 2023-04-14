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
		Logger? logger;

        public ImageSynthetizer(int wid, int hei, FloatCamera fc, float[] bgColor, Logger? log = null)
        {
            objects = new();
            lights = new();
            this.camera = fc;
            this.width = wid;
            this.height = hei;
            this.backgroundColor = bgColor;
			this.logger = log;
        }
		//adding this one for loading from a file
		public ImageSynthetizer(int wid, int hei, FloatCamera fc, float[] bgColor, List<Solid> objects, List<LightSrc> lights) : this(wid, hei, fc, bgColor){
			this.objects = objects;
			this.lights = lights;
		}

        public void AddSolid(Solid s)
        {
            objects.Add(s);
        }

        public void AddLight(LightSrc light)
        {
            lights.Add(light);
        }
        float ComputeNorm(Vector3 v){
            return (float)Math.Sqrt(Math.Pow(v.X, 2) + Math.Pow(v.Y, 2) + Math.Pow(v.Z, 2));
        }
        public FloatImage RenderScene()
        {
            logger?.DoLog($"Starting at {DateTime.Now}");
            FloatImage fi = new FloatImage(width, height, 3);
            Vector3 center = camera.Position;
            float d = camera.Direction.Length();
            float fov = camera.Frustrum / 2;
            double fovToRadians = fov * (Math.PI / 180);
            var xLength = (float)Math.Tan(fovToRadians) * d;

            Vector3 direction = camera.Direction ;
            var right = //Vector3.Cross(Vector3.Normalize(camera.Direction),Vector3.UnitX);
						Vector3.Normalize((float)xLength * Vector3.UnitX);
            
			var up = Vector3.Cross(right, Vector3.Normalize(direction));
			
            var perspectiveCenter = center + new Vector3(0,0,d);
            var upLeft = perspectiveCenter - right+ up;
            var upRight = perspectiveCenter + right+ up;
            var downLeft = perspectiveCenter - right- up;
            var downRight = perspectiveCenter + right- up;
            logger?.DoLog($"center={center}");
            logger?.DoLog($"perspectiveCenter={perspectiveCenter}");
            logger?.DoLog($"direction={camera.Direction}");
            logger?.DoLog($"d={d}");
            logger?.DoLog($"fov={fov}");
            logger?.DoLog($"xLength={xLength}");
            logger?.DoLog($"up={up}");
			logger?.DoLog($"upLeft={upLeft}");
			logger?.DoLog($"upRight={upRight}");
			logger?.DoLog($"downLeft={downLeft}");
			logger?.DoLog($"downRight={downRight}");
			int numOfCasts = 0; //DEBUG ONLY
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool foundInt = false;

                    Vector3 top = Vector3.Lerp(upLeft, upRight, x / (float)Math.Max(height,width));
                    Vector3 bottom = Vector3.Lerp(downLeft, downRight, x / (float)Math.Max(height,width));
                    Vector3 ray = Vector3.Lerp(top, bottom, y / (float)Math.Max(height,width));

                    for (int i = 0; i < objects.Count && !foundInt; i++)
                    {
						Solid solid = objects[i];
                        Vector3 color = new Vector3();
                        if (solid.Intersect(camera.Position, ray, out float t))
                        {
							numOfCasts++;
							foundInt = true;
                            var pt = camera.Position + t * ray;							
                            foreach (var light in lights)
                            {
                                var viewVector = Vector3.Normalize(solid.Position - pt);
                                var normalVector = solid.Normal(pt);
                                var lightVector = Vector3.Normalize(light.Position - pt);
                                var contrib = light.ComputeLightContrib(solid.Model!, normalVector, lightVector, viewVector);
                                color += contrib;
                            }
							color /= (d*d);
                            
                            color += solid.Model!.AmbientLight();
                            fi.PutPixel(x, y, new float[] { color.X, color.Y, color.Z });
                        }
                        else
                        {
                            fi.PutPixel(x, y, backgroundColor);
                        }
                    }

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
        public readonly float H ;
        public readonly float kA , kD , kS ; // kA + kD + kS = 1


        public Phong(Vector3 color, float highlight, float kA, float kD, float kS)
        {

            this.kA = kA;
            this.kD = kD;
            this.kS = kS;
            this.H = highlight;
            this.color = color;
        }

        public Vector3 DiffuseComponent( Vector3 intensity ,Vector3 view, Vector3 normal)
        {
            return color * kD * Math.Max(Vector3.Dot(view, (normal)),0);
        }
        public Vector3 AmbientLight()
        {
            return color * kA;
        }
        public Vector3 SpecularComponent(Vector3 light, Vector3 unitV, Vector3 unitR)
        {
            return  light * kS * (float)Math.Pow(Math.Max(Vector3.Dot(unitR, unitV),0), H);
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
        public abstract Vector3 ComputeLightContrib(Phong model,Vector3 n, Vector3 l, Vector3 v);
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
            var R = (2*n*Vector3.Dot(n, l) - l); // Unit reflection vector
            var E = model.DiffuseComponent(this.Intensity,v, n) 
            + model.SpecularComponent(this.Intensity, v, R);
            return Intensity*E;
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

        public override Vector3 Normal(Vector3 pos) => throw new NotImplementedException();

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
			
            var p0 = position;
            var p1 = direction;
            float D =  (position- this.position).Length();
            var n = (Position);
            if ((Vector3.Dot(n, p1)) <= float.Epsilon){
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
        public override Vector3 Normal(Vector3 position) => Vector3.Normalize(new Vector3(2 * position.X, 2* position.Y, 2*position.Z));

        public override bool Intersect(Vector3 position, Vector3 direction, out float t)
        {
            var offset = position - this.position;
            var a = Vector3.Dot(direction,direction);
            var b = 2*(Vector3.Dot(offset,direction));
            var c = Vector3.Dot(offset,offset) - radius*radius;
            var D = b*b - 4 * (a * c);
            var t0 = (float)(-1 * b - Math.Sqrt(D)) / (2 * a);
            var t1 = (float)(-1 * b + Math.Sqrt(D)) / (2 * a);
			if(t0 >= 0 )
				t = t0;
			else if(t1 >= 0)
				t = t1;
			else t = 0;
			return t0 >=0 || t1 >=0;
        }

    }

}
