using Util;
using System.Text.Json;
using rt004.checkpoint2;
using System.Numerics;
//using System.Numerics;

namespace rt004;

#region logging system
/*
I implement here logging system using Strategy Design pattern.
*/

interface ILog
{
    void Log(string info);
}

class ConsoleLog : ILog
{
    public void Log(string info)
    {
        System.Console.WriteLine(info);
    }
}
class FileLog : ILog
{
    string filename;
    public FileLog(string fname)
    {
        filename = fname;
    }
    public void Log(string info)
    {
        try
        {
            File.AppendText(info + "\n");
        }
        catch (IOException e)
        {
            System.Console.WriteLine(e.Message);
        }
    }
}

class Logger
{
    ILog currentLog;
    public Logger()
    {
        currentLog = new ConsoleLog();
    }
    public void ChangeLogger(ILog logger)
    {
        currentLog = logger;
    }
    public void DoLog(string info)
    {
        currentLog.Log(info);
    }
}
#endregion
#region parsing
[Serializable]
class Params
{
    public int width { set; get; }
    public int height { set; get; }
    public string fileName { set; get; }
    public Params(int width, int height, string fileName)
    {
        this.fileName = fileName;
        this.height = height;
        this.width = width;
    }
}
class JsonParser
{
    string filename;
    public JsonParser(string filename)
    {
        this.filename = filename;
    }

    public void ReadFile(out int wid, out int hei, out string fname)
    {
        //default params
        wid = 600;
        hei = 450;
        fname = "demo.pfm";
        try
        {
            string sr = File.ReadAllText(filename);
            Params par = JsonSerializer.Deserialize<Params>(sr)!;
            wid = par.width;
            hei = par.height;
            fname = par.fileName!;
        }
        catch (IOException e)
        {
            System.Console.WriteLine(e.Message + "\nUsing default parameters.");
        }
    }
};
#endregion
internal class Program
{
    static void SetPoints(FloatImage fi, int x, int y, float[] color)
    {
        fi.PutPixel(x, y, color);
        fi.PutPixel(-x, y, color);
        fi.PutPixel(x, -y, color);
        fi.PutPixel(-x, -y, color);
    }

    //Algorithm rewritten from https://cgg.mff.cuni.cz/~pepca/lectures/pdf/icg-13-curves.pdf, slightly modified in the second part
    static void MidpointDraw(FloatImage fi, int a, int b, float[] color)
    {
        int x = 0, y = b;
        double aa = Math.Sqrt(a), aa2 = 2 * aa, bb = (int)Math.Sqrt(b), bb2 = 2 * bb;
        double D = (bb - aa * b + aa) / 4;
        double dx = bb2, dy = aa * (2 * b - 1);
        SetPoints(fi, 0, b, color);
        while (dx < dy)
        {
            if (D >= 0)
            {
                D = D - dy + aa;
                dy = dy - aa2;
                y--;
            }
            D += dx + bb;
            dx += bb2;
            x++;
            SetPoints(fi, x, y, color);
        }
        D = bb * (Math.Sqrt(x) + x) + bb / 4 + aa * (Math.Sqrt(y - 1) - bb);
        while (0 < y)
        {
            if (D < 0)
            {
                D += dx;
                dx = dy - aa2;
                x++;
            }
            D = D - dy + aa2;
            dy -= aa2;
            y--;
            SetPoints(fi, x, y, color);
        }
    }

    static void FloodQueue(FloatImage fi, int x, int y, float[] newColor, float[] oldColor)
    {
        Queue<(int, int)> queue = new();
        (int, int) currentState = (x, y);
        HashSet<(int, int)> visited = new();
        queue.Enqueue(currentState);
        while (queue.Count > 0)
        {
            currentState = queue.Dequeue();
            int[] plusminus = new int[] { -1, 0, 1 };
            foreach (int i in plusminus)
            {
                foreach (int j in plusminus)
                {
                    if (currentState.Item1 + i < fi.Width && currentState.Item2 + j < fi.Height
                    && currentState.Item1 + i >= 0 && currentState.Item2 + j >= 0)
                    {
                        float[] newArr = new float[3];
                        fi.GetPixel(currentState.Item1 + i, currentState.Item2 + j, newArr);
                        if (
                            i != j
  
                            //if i am really rewriting an old color
                        && Math.Abs(newArr[0] - oldColor[0]) < 0.0001f
                        && Math.Abs(newArr[1] - oldColor[1]) < 0.0001f
                        && Math.Abs(newArr[2] - oldColor[2]) < 0.0001f
                        && !visited.Contains((currentState.Item1 + i, currentState.Item2 + j)))
                        {
                            queue.Enqueue((currentState.Item1 + i, currentState.Item2 + j));
                            visited.Add((currentState.Item1 + i, currentState.Item2 + j));
                        }
                    }
                }
            }
            fi.PutPixel(currentState.Item1, currentState.Item2, newColor);
            visited.Add(currentState);
        }
    }
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
            bool b =int.TryParse(args[0], out wid);
            bool b0 =int.TryParse(args[1], out hei);
            fileName = args[2];
            if(!b || !b0){
                throw new ArgumentException("Wrong input, numbers expected");
            }
        }
        else
        {
            JsonParser jp = new JsonParser(configFileName);
            jp.ReadFile(out wid, out hei, out fileName);
        }
        // HDR image.
        var dir = new Vector3(0, 0.9f, 1);
        var pos = new Vector3(0.60f, -0.00f,-5.60f);
        var BACKGROUND =new float[] {0.1f,0.2f,0.3f};
        FloatCamera fc = new FloatCamera(pos, dir,40);
        ImageSynthetizer img = new ImageSynthetizer(wid,hei,fc,BACKGROUND);

        img.AddLight(new PointLightSource(new Vector3(-10,8,-6),new Vector3(0,1,0)));
        
        //img.AddLight(new PointLightSource(new Vector3(0,20,-3), new Vector3(0.3f,0.3f,0.3f)));
       // img.AddSolid(new InfPlane(new Vector3(-8,1,0),new Phong(new Vector3(1,0,0.2f),10,0.1f,0.8f,0.2f)));
        img.AddSolid(new Sphere(new Vector3(0, 0.9f, 1),new Phong(new Vector3(1,0,0.2f),1,0.1f,0.8f,0.2f), 0.5f));
        var x = img.RenderScene();
        x.SavePFM(fileName);
    }
}
