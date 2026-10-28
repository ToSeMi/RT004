using Util;
using System.Text.Json;
using rt004.checkpoint2;
using System.Numerics;
//using rt004.checkpoint4;
using OpenTK.Mathematics;
using rt004.checkpoint4;
//using System.Numerics;


namespace rt004
{

    internal class Program
    {
        static void Main(string[] args)
        {
            // Parameters.
            int wid, hei;
            string fileName;

            string configFileName = "conf.json";
            if (args.Length == 1)
            {
                configFileName = args[0];
            }
            if (args.Length == 3)
            {
                bool b = int.TryParse(args[0], out wid);
                bool b0 = int.TryParse(args[1], out hei);
                fileName = args[2];
                if (!b || !b0)
                {
                    throw new ArgumentException("Wrong input, numbers expected");
                }
            }
            else
            {
                JsonParser jp = new JsonParser(configFileName);
                jp.ReadFile(out wid, out hei, out fileName);
            }

            var yellow = new Phong(new Vector3d(1, 1, 0.2f), 10, 0.1f, 0.8f, 0.2f, 1f);
            var blue = new Phong(new Vector3d(.2f, 0.3f, 1f), 150, 0.1f, 0.5f, 0.5f, 1.5f, true, true);
            var red = new Phong(new Vector3d(.8f, .2f, 0.4f), 10, 0.1f, 0.6f, 0.4f, 1.5f, true, false);
            var white = new Phong(new Vector3d(.9f, .9f, .9f), 80, 0.1f, 0.6f, 0.4f, 1.5f, false, true);

            var whitePlane = new Phong(new Vector3d(.9f, .9f, .9f), 80, 0.1f, 0.6f, 0.4f, 1.5f, false, false);
            // HDR image.
            var pos = new Vector3d(0f, 0f, 0f);
            var dir = new Vector3d(0.00f, 0f, 1);
            var BACKGROUND = new float[] { 0.1f, 0.2f, 0.3f };

            Logger l = new Logger();
            l.ChangeLogger(new FileLog("output.log"));
            #region original scene
          /*  Scene scene = new Scene(BACKGROUND);
            scene.AddLight(new PointLightSource(new Vector3d(5, 5, 5), new Vector3d(1, 1, 1)));
            scene.AddLight(new PointLightSource(new Vector3d(2, -3, 4), new Vector3d(0.1f, 0.1f, 0.1f)));
            scene.AddLight(new AmbientLightSource(0.1f));

            //scene one
            // scene.AddSolid(new InfPlane(new Vector3(0.0f, -2f, 0.0f),whitePlane));
            //    scene.AddSolid(new Sphere(new Vector3d(-1, 1, 3), blue, .25f));
            //    scene.AddSolid(new Sphere(new Vector3d(0, 2, 5f), red, 1));


            //scene two
            //  scene.AddSolid(new Sphere(new Vector3d(10f, 12, 75), red, 10f));
         //   scene.AddSolid(new Sphere(new Vector3d(0, 0, 5), yellow, 1f));
            scene.AddSolid(new InfPlane( whitePlane) {Position = new Vector3d(0, -2, 0)});
            scene.AddSolid(new Sphere(new Vector3d(-2, 2, 5), white, 1f));
            scene.AddSolid(new Sphere(new Vector3d(2.1, 0, 10), yellow, 1f));*/
            #endregion
            SceneGraph sceneGraph = new SceneGraph(BACKGROUND, new Vertex()
            {
                transformMatrix = Matrix4d.Identity,//CreateTranslation(new Vector3d(0, 1, 5)),
                children = new IIntersectable[] {
                    new Vertex() {
                        transformMatrix = Matrix4d.CreateTranslation(0,-2,0),
                        children = new IIntersectable[] {
                            new InfPlane(whitePlane)
                        }
                    },
                    new Vertex() {
                        transformMatrix = Matrix4d.CreateTranslation(-2,2,5),
                          children = new IIntersectable[] {
                            new Sphere(white)
                            }
                        }
                    ,

                new Vertex()
                {
                    transformMatrix = Matrix4d.CreateTranslation(2.1, 0, 10),
                    children = new IIntersectable[] {
                        new Vertex() {
                            transformMatrix = Matrix4d.Scale(2),
                            children = new IIntersectable[] {

                                    new Sphere(white)
                            }
                        }
                                }
                }
                }
            });

            sceneGraph.AddLight(new PointLightSource(new Vector3d(5, 5, 5), new Vector3d(1, 1, 1)));
            sceneGraph.AddLight(new PointLightSource(new Vector3d(2, -3, 4), new Vector3d(0.1f, 0.1f, 0.1f)));
            sceneGraph.AddLight(new AmbientLightSource(0.1f));

            FloatCamera fc = new FloatCamera(pos, dir, 40);
            var sceneFromFile = ObjectCreator.CreateScene("testFile.json");
            if(sceneFromFile == null) {
                Console.WriteLine("FATAL ERROR HAPPENED DURING LOADING THE SCENE! ABORTING...");
                return;
            }
            ImageSynthetizer img = 
                new ImageSynthetizer(wid, hei, fc, sceneFromFile, 10);
               // new ImageSynthetizer(wid, hei, fc, sceneGraph, 10);
            //SceneGraph.BuildSceneFromFile("testFile.json");
            var x = img.RenderSceneParallel();
            x.SavePFM(fileName);
            //  scene.SaveToFile("testFile.json");

        }
    }
}
