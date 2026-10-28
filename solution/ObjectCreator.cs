using rt004;
using rt004.checkpoint2;
using OpenTK.Mathematics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace rt004.checkpoint4
{

    /*         {
     "type":0,  
     "x":10,"y":12,"z":75,
     "Model_id":3,
     "Solid_type_id":1
 },
 {
     "Model_id":3,
     "type":1,
     "transform_matrix":"translate(0,1,2)",
     "children":[0,1,2,3]
 }*/
    enum MatrixCommand { IDENTITY, TRANSLATE, ROTATE, SCALE };
    public class ObjectCreator
    {
        public static SceneFlatRepreGraph? CreateScene(string path)
        {
            SceneFlatRepreGraph? sceneGraph = null;
            string? json_text = TryLoadJsonText(path);
            if(json_text == null) {
                return null;
            }
            var node = JsonNode.Parse(json_text);
            if (node == null)
            {
                System.Console.Error.WriteLine("THIS FILE COULD NOT BE LOADED! PLEASE, CHECK WHETHER YOUR FILE IS CORRECT. ABORTING...");
                return null;
            }
            var materials = node["materials"]?.AsArray();
            var objects = node["objects"]?.AsArray();
            var lights = node["lights"]?.AsArray();
            var background_color = node["BackgroundColor"]?.AsArray();
            var root_id = (int?)node["root_object_id"];
            if (materials == null || objects == null || lights == null || background_color == null || root_id == null)
            {
                System.Console.Error.WriteLine("COULD NOT EITHER FIND DEFINITION OF MATERIALS, OBJECTS, LIGHTS, ROOT_ID OR BACKGROUND COLOR! ABORTING...");
                return null;
            }

            float[] background_color_decomposed = { (float)background_color[0], (float)background_color[1], (float)background_color[2] };
            List<Phong> materials_decomposed = new();
            List<IIntersectable> solids_decomposed = new();
            List<LightSrc> light_decomposed = new();
            
            if (DecomposeMaterials(materials, materials_decomposed)&&
                TryDecomposeObjects(objects, materials_decomposed, solids_decomposed) &&
                TryDecomposeLights(lights, light_decomposed))
            {
                sceneGraph = new SceneFlatRepreGraph(light_decomposed, solids_decomposed, background_color_decomposed, (int)root_id);
                return sceneGraph;
            }
            return null;
        }

        private static bool TryDecomposeLights(JsonArray lights, List<LightSrc> light_decomposed)
        {
            foreach (var x in lights)
            {
                if (x is null)
                {
                    System.Console.Error.WriteLine("WRONG LIGHT DEFINITION DETECTED! ABORTING...");
                    return false;
                }
                string? typeOfLight = (string?)x["type"];
                if (typeOfLight is null)
                {
                    System.Console.Error.WriteLine("TYPE OF LIGHT NOT FOUND! ABORTING...");
                    return false;
                }
                if (typeOfLight == "point")
                {
                    var jsonPosition = x["position"]?.AsArray();
                    var jsonIntensity = x["intensity"]?.AsArray();
                    if (jsonIntensity == null || jsonPosition == null)
                    {
                        System.Console.Error.WriteLine("POSITION OR INTENSITY NOT FOUND! ABORTING...");
                        return false;
                    }

                    var intensity = new Vector3d((double)jsonIntensity[0]!, (double)jsonIntensity[1]!, (double)jsonIntensity[2]!);
                    var position = new Vector3d((double)jsonPosition[0]!, (double)jsonPosition[1]!, (double)jsonPosition[2]!);
                    light_decomposed.Add(new PointLightSource(position, intensity));
                }
                else if (typeOfLight == "ambient")
                {
                    var kA = (double?)x["kA"];
                    if (kA is null)
                    {
                        System.Console.Error.WriteLine("AMBIENT CONSTANT NOT FOUND! ABORTING...");
                        return false;
                    }
                    light_decomposed.Add(new AmbientLightSource((double)kA));
                }
                else
                {
                    System.Console.Error.WriteLine("ERROR, DO NOT KNOW TYPE" + typeOfLight + ". ABORTING...");
                    return false;
                }
            }
            return true;
        }

        private static bool TryDecomposeObjects(JsonArray objects, List<Phong> materials_decomposed, List<IIntersectable> solids_decomposed)
        {
            foreach (var x in objects)
            {
                int? type = (int?)x?["type"];
                if(type == null) {
                    System.Console.Error.WriteLine("TYPE NOT FOUND! ABORTING...");
                    return false;
                }
                if (type == 0)
                {
                    Solid? newObject = null;
                    var jsonSolidIndex = x["Solid_type_id"];
                    var jsonModelIndex = x["Model_id"];
                    if (jsonModelIndex == null || jsonSolidIndex == null)
                    {
                        System.Console.WriteLine("WRONG FORMAT! SOLID ID OR MODEL ID NOT FOUND!");
                        return false;
                    }
                    int modelIndex = (int)jsonModelIndex;
                    int solidIndex = (int)jsonSolidIndex;
                    Phong model = materials_decomposed[modelIndex];
                    if (solidIndex == 0)
                    {
                        newObject = new InfPlane(model);
                    }
                    else if (solidIndex == 1)
                    {
                        newObject = new Sphere(model);
                    }
                    else
                    {
                        System.Console.WriteLine($"WRONG SOLID ID \"{solidIndex}\"! CANNOT CONTINUE, ABORTING...");
                        return false;

                    }
                    if (newObject != null)
                    {
                        solids_decomposed.Add(newObject);
                    }
                }
                else if (type == 1)
                {
                    InnerFlatVertex inv = new();

                    var listOfDescendants = x["children"].AsArray();
                    inv.children = new int[listOfDescendants.Count];
                    for (int i = 0; i < inv.children.Length; i++)
                    {
                        inv.children[i] = (int)listOfDescendants[i].AsValue();
                    }
                    inv.transformMatrix = CreateMatrixFromACommand((string)x["transform_matrix"]);
                    solids_decomposed.Add(inv);
                }
            }
            foreach (var x in solids_decomposed)
            {
                if (x is InnerFlatVertex inner)
                {
                    inner.listOfNodes = solids_decomposed;
                }
            }
            return true;
        }

        private static bool DecomposeMaterials(JsonArray materials, List<Phong> materials_decomposed)
        {
            foreach (var x in materials)
            {
                var c_arr = x?["color"]?.AsArray();
                if (c_arr == null || x == null)
                {
                    System.Console.WriteLine("WRONG MATERIAL DEFINITION. ABORTING...");
                    return false;
                }
                Vector3d color = new((double)c_arr[0], (double)c_arr[1], (double)c_arr[2]);
                //color":[1,1,0.2],"H":10,"kA":0.1,"kD":0.8,"kS":0.2,"RefractionIndex":1
                Phong addable = new(color, (double)x["H"], (double)x["kA"], (double)x["kD"], (double)x["kS"], (double)x["RefractionIndex"]);
                materials_decomposed.Add(addable);
            }
            return true;
        }

        static Matrix4d CreateMatrixFromACommand(string strCommand)
        {
            string[] matrixCommands = strCommand.Trim().ToLower().Split(" ");
            MatrixCommand matCommand = MatrixCommand.IDENTITY;
            Matrix4d matrix = Matrix4d.Identity;
            foreach (string command in matrixCommands)
            {
                bool finishedReadingCommand = false;
                StringBuilder sb = new StringBuilder();
                foreach (var x in command)
                {
                    if (x == '(')
                    {
                        finishedReadingCommand = true;
                        string matrixCommand = sb.ToString();
                        sb.Clear();
                        if (matrixCommand == "translate")
                        {
                            matCommand = MatrixCommand.TRANSLATE;
                        }
                        else if (matrixCommand == "rotate")
                        {
                            matCommand = MatrixCommand.ROTATE;
                        }
                        else if (matrixCommand == "scale")
                        {
                            matCommand = MatrixCommand.SCALE;
                        }
                        else
                        {
                            Console.Error.WriteLine($"COULD NOT READ THE COMMAND \"{matrixCommand}\", PROCEEDING WITH THE IDENTITY MATRIX!");
                            matCommand = MatrixCommand.IDENTITY;
                            break;
                        }
                    }
                    else if (x == ')')
                    {
                        if (!finishedReadingCommand)
                        {
                            System.Console.Error.WriteLine("NOT A VALID COMMAND!");
                        }
                        string[] inputVector = sb.ToString().Trim().Split(',');

                        switch (matCommand)
                        {
                            case MatrixCommand.TRANSLATE:
                                if (
                                    float.TryParse(inputVector[0], out float coordX)
                                    && float.TryParse(inputVector[1], out float coordY)
                                    && float.TryParse(inputVector[2], out float coordZ)
                                )
                                    matrix *= Matrix4d.CreateTranslation(
                                       coordX, coordY, coordZ
                                    );
                                break;
                            case MatrixCommand.ROTATE:
                                if (
                                float.TryParse(inputVector[0], out float cX)
                                && float.TryParse(inputVector[1], out float cY)
                                && float.TryParse(inputVector[2], out float cZ)
                            )
                                    matrix *= Matrix4d.CreateRotationX(cX) *
                                                Matrix4d.CreateRotationY(cY) *
                                                Matrix4d.CreateRotationZ(cZ);
                                break;
                            case MatrixCommand.SCALE:
                                if (
                                    float.TryParse(inputVector[0], out float scale)
                                )
                                    matrix *= Matrix4d.Scale(scale);
                                break;
                            case MatrixCommand.IDENTITY:
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(x);
                    }
                }
            }
            return matrix;

        }

        private static string? TryLoadJsonText(string path)
        {
            try
            {
                using (var reader = File.OpenText(path))
                {
                    StringBuilder stringBuilder = new();
                    int x;
                    while ((x = reader.Read()) != -1)
                    {
                        stringBuilder.Append((char)x);
                    }
                    return stringBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return null;
            }
        }
    }

}