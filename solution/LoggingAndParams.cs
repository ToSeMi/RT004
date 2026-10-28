
using System.Text.Json;
namespace rt004 {
        #region logging system
    /*
    I implement here logging system using Strategy Design pattern.
    */

    public interface ILog
    {
        void Log(string info);
    }

    public class ConsoleLog : ILog
    {
        public void Log(string info)
        {
            System.Console.WriteLine(info);
        }
    }
    public class FileLog : ILog
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
                File.AppendAllText(filename, info + "\n");
            }
            catch (IOException e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
    }

    public class Logger
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
}