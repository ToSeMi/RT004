using Util;
using OpenTK.Mathematics;
using System.Numerics;
namespace rt004.checkpoint2
{
    public class ImageSynthetizer
    {
        AbstractScene scene;
        FloatCamera camera;
        int width, height;
        Logger? logger;
        readonly int RayTracingDepth;

        Random r = new Random(123); //for deterministic purposes
        const int numberOfSamples = 10;
        const double constant = 0.005;

        public ImageSynthetizer(int wid, int hei, FloatCamera fc, AbstractScene abstractScene, int rtDepth = 10, Logger? log = null)
        {
            this.scene = abstractScene;
            this.camera = fc;
            this.width = wid;
            this.height = hei;
            this.logger = log;
            this.RayTracingDepth = rtDepth;
        }
        //adding this one for loading from a file
        /*public ImageSynthetizer(int wid, int hei, FloatCamera fc, double[] bgColor, List<Solid> objects, List<LightSrc> lights) : this(wid, hei, fc, bgColor)
        {
            this.objects = objects;
            this.lights = lights;
        }*/

        ///<summary>Renders the scene with the classic shooting ray technique</summary>
        ///<returns>Image with the perspective</returns>
        public FloatImage RenderScene()
        {
            logger?.DoLog($"Starting at {DateTime.Now}");
            var fi = new FloatImage(width, height, 3);
            camera.SetCamera(logger);
            //  camera.currentScene!.SortSolidsByView(camera.Position);
            int numOfCasts = 0; //DEBUG ONLY
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //This is for debbuding the issues with pixels
                    if (x == 126 && y == 191)
                    {
                        System.Console.WriteLine();
                    }
                    var color = RenderPixel(x, y, height, width, ref numOfCasts);
                    fi.PutPixel(x, y, new float[] { (float)color.X, (float)color.Y, (float)color.Z });

                }
            }
            logger?.DoLog($"numOfCasts={numOfCasts}");
            logger?.DoLog("------------------------------------------");
            return fi;
        }

        public FloatImage RenderSceneParallel()
        {
            logger?.DoLog($"Starting at {DateTime.Now}");
            var fi = new FloatImage(width, height, 3);
            camera.SetCamera(logger);
            int numOfCasts = 0; //DEBUG ONLY
            Parallel.For(0, height,
                y =>
                {
                    Parallel.For(0, width, x =>
                    {
                        var color = RenderPixel(x, y, height, width, ref numOfCasts);
                        fi.PutPixel(x, y, new float[] { (float)color.X, (float)color.Y, (float)color.Z });
                    });
                }
                );
            return fi;
        }
        ///<summary>Renders one pixeel from one raycast</summary>
        public Vector3d RenderPixel(double x, double y, double height, double width, ref int numOfCasts)
        {

            Vector3d ray = camera.GetRay(x, y, height, width);
            Vector3d avg = new Vector3d(0);
            for (int i = 0; i < numberOfSamples; ++i)
            {
                int sgn = Math.Sign(r.Next(-1, 1));
                int sgn0 = Math.Sign(r.Next(-1, 1));
                int sgn1 = Math.Sign(r.Next(-1, 1));
                var scatteredRay = constant * new Vector3d(sgn * r.NextDouble(), sgn0 * r.NextDouble(), sgn1 * r.NextDouble());
                return  RayTrace(camera.Position, ray, ref numOfCasts);
            }
            return avg / numberOfSamples;

        }
        public static double square(double input)
        {
            return input * input;
        }
        Vector3d Refract(Vector3d normalizedDirection, Vector3d normal, double outIndexRefract)
        {
            double n12 = 1 / outIndexRefract;
            double cosAlpha = Math.Min(Vector3d.Dot(normal, -normalizedDirection),1);
            double sinAlpha = Math.Sqrt(1- square(cosAlpha));
            if (n12 * sinAlpha > 1)
            {
                return Reflect(normal, normalizedDirection);
            }
            /*if (cosAlpha > 0)
            {
                n1 = outIndexRefract;
                n2 = inIndexRefract;
                normal = -normal;
            }
            else
            {
                n1 = inIndexRefract;
                n2 = outIndexRefract;
                cosAlpha = -cosAlpha;
            }*/


            double cosBetaSquared = 1 - (square(n12) * (1 - square(cosAlpha))); //1−n12^2⋅(1−(n⋅l)^2)
            double sinBeta = Math.Sqrt(1 - cosBetaSquared);
            
            double cosBeta = Math.Sqrt(cosBetaSquared);
            var perp = n12 * (normalizedDirection + cosAlpha*normal);
            //return perp + Math.Sqrt(Math.Abs(1-perp.LengthSquared))*normal;
            return //Vector3d.Normalize((n12 * cosAlpha - cosBeta) * normal - n12 * normalizedDirection);
                Vector3d.Normalize((n12 * cosAlpha -
                    Math.Sqrt(
                        1 - square(n12) *
                        (1 - square(cosAlpha))
                        )
                    ) *
                    normal - n12 * normalizedDirection);
        }

        Vector3d Reflect(Vector3d normalVector, Vector3d viewVector)
        {
            var gamma = Vector3d.Dot(normalVector, viewVector);
            return Vector3d.Normalize(2 * normalVector * Math.Max(gamma, 0) - viewVector); // Unit reflection vector
        }
        Vector3d RayTrace(Vector3d position, Vector3d ray, ref int numOfCasts, int depth = 0)
        {
            Vector3d color = Vector3d.Zero;

            bool solid = scene.IntersectScene(position, ray, out Hit info);
            if (solid && info.t > 10e-6)
            {
                numOfCasts++;
                var pt = info.point;
                var viewVector = Vector3d.Normalize(camera.Position - pt);
                var normalVector = info.normal;
                foreach (var light in scene.Lights)
                {
                    Vector3d contrib = Vector3d.Zero;
                    if (light is PointLightSource pointLight)
                    {
                        var lightVector = Vector3d.Normalize(pointLight.Position - pt);
                        bool isInShadow = scene.IntersectScene(pt, lightVector, out Hit info0);
                        if (!isInShadow || (isInShadow && info0.t < 10e-6))
                        {
                            contrib = light.ComputeLightContrib(info.material!, normalVector, lightVector, viewVector);
                        }
                    }
                    else
                    {
                        contrib = light.ComputeLightContrib(info.material!, normalVector, pt, viewVector);
                    }
                    color += contrib;
                }
                if (depth + 1 < RayTracingDepth)
                {
                    var normalizedDirection = Vector3d.Normalize(ray);
                    double kr = info.material.GetRefraction(viewVector, normalVector);
                    if (info.material.IsReflective())
                    {
                        var reflected = Reflect(normalVector, normalizedDirection);
                        logger?.DoLog("Reflected:" + reflected);
                        color += kr * RayTrace(pt, reflected, ref numOfCasts, depth + 1);
                    }
                    if (info.material.IsRefractive())
                    {
                        var refracted = Refract(normalizedDirection, normalVector, info.material.RefractionIndex);
                        logger?.DoLog("Refracted: " + refracted);
                        color += (1 - kr) * RayTrace(pt, refracted, ref numOfCasts, depth + 1);
                    }
                    logger?.DoLog("------------------------------");

                }
                return color;
            }
            return scene.BackgroundColor;
        }

    }
}