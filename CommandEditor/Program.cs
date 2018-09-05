using System.IO;

namespace CommandEditor
{
    class Program
    {
        static void Main(string[] args)
        {
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
