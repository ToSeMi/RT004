using Util;
using System.Text.Json;
using System.IO;
//using System.Numerics;

namespace rt004;

[Serializable]
class Params {
  public int width {set;get;}
  public int height {set;get;}
  public string fileName {set;get;}
  public Params(int width, int height, string fileName) {
    this.fileName = fileName;
    this.height = height;
    this.width = width;
  }
}
class JsonParser {
  string filename;
  public JsonParser(string filename){
    this.filename = filename;
  }

  public void ReadFile(out int wid, out int hei, out string fname){
    //default params
    wid = 600;
    hei = 450;
    fname = "demo.pfm";
    try {
        string sr =File.ReadAllText(filename);
        Params? par = JsonSerializer.Deserialize<Params>(sr);
        
        wid = par.width;
        hei = par.height;
        fname = par.fileName!;
        
      
    }catch(IOException e) {
      System.Console.WriteLine(e);
    }
  }
};

internal class Program
{
  static void Main(string[] args)
  {
    // Parameters.
    int wid, hei;
    string fileName;
    string configFileName = "conf.json";
    if(args.Length == 1){
      configFileName = args[0];
    }
    JsonParser jp = new JsonParser(configFileName);
    jp.ReadFile(out wid, out hei, out fileName);
    
    // HDR image.
    FloatImage fi = new FloatImage(wid, hei, 3);

    // TODO: put anything interesting into the image.
    // TODO: use fi.PutPixel() function, pixel should be a float[3] array [R, G, B]
    fi.PutPixel(2,5,new float[]{5f, 4f, 255f});
    //fi.SaveHDR(fileName);   // Doesn't work well yet...
    fi.SavePFM(fileName);

    Console.WriteLine("HDR image is finished.");
  }
}
