using System.Collections.Generic;
using System.IO;

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
                    GenerateCommandFile.GenerateFiles(file.FullName);
                }
            }
        }

        
    }
}
