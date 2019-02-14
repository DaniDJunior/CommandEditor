using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace CommandEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            //List<string> Arg = new List<string>() { @"C:\Users\Animatech\Documents\Projetos\Teste\Teste.json" };
            //args = Arg.ToArray();

            foreach(string arg in args)
            {
                if (File.Exists(arg))
                {
                    FileInfo file = new FileInfo(arg);
                    StreamReader fileJson = new StreamReader(file.FullName);

                    string json = fileJson.ReadToEnd();
                    fileJson.Close();
                    GenerateCommandFile.GenerateFiles(json);
                }
            }
        }

        
    }
}
