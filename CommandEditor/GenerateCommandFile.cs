using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CommandEditor
{
    public class GenerateCommandFile
    {
        private static string pathIn = string.Empty;
        private static string pathOut = string.Empty;
        private static object Data = null;

        public static void GenerateFiles(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            foreach (KeyValuePair<string, object> entry in (dynamic)serializer.DeserializeObject(json))
            {
                if (entry.Key == "pathIn")
                {
                    pathIn = entry.Value.ToString();
                }
                if (entry.Key == "pathOut")
                {
                    pathOut = entry.Value.ToString();
                }
                if (entry.Key == "data")
                {
                    Data = entry.Value;
                }
            }
            if (!string.IsNullOrEmpty(pathIn) && !string.IsNullOrEmpty(pathOut) && (Data != null))
            {
                OpenData(Data);
            }
            else
            {
                throw new Exception("Json em formato invalido");
            }
        }

        private static List<string> GetDirectory(string path, string extension)
        {
            List<string> temp = new List<string>();
            foreach (string p in Directory.GetDirectories(path))
            {
                foreach (string i in GetDirectory(p, extension))
                    temp.Add(i);
            }
            foreach (string f in Directory.GetFiles(path, "*." + extension))
            {
                temp.Add(f);
            }
            return temp;
        }

        private static void OpenData(dynamic dados)
        {
            foreach (KeyValuePair<string, object> entry in dados)
            {
                if (entry.Value != null)
                {
                    List<string> files = GetDirectory(pathIn, entry.Key);

                    switch (entry.Value.GetType().FullName)
                    {
                        case "System.String":
                        case "System.Boolean":
                        case "System.Int32":
                        case "System.Decimal":
                            throw new Exception("Json em formato invalido");
                        case "System.Object[]":
                            GetFileList(entry.Value, files, entry.Key);
                            break;
                        default:
                            if (entry.Value.GetType().FullName.Contains("System.Collections.Generic.Dictionary"))
                            {
                                GetFiles(entry.Value, files, entry.Key);
                            }
                            else
                            {
                                throw new Exception("Objeto não reconhecido pelo sistema: " + entry.Value.GetType().FullName);
                            }
                            break;
                    }
                }
            }
        }

        private class fileChange
        {
            public string originalFileName { get; set; }
            public string fileName { get; set; }
            public string body { get; set; }
        }

        private static void GetFileList(dynamic dados, List<string> listFile, string key)
        {
            foreach (object list in dados)
            {
                switch (list.GetType().FullName)
                {
                    case "System.String":
                    case "System.Boolean":
                    case "System.Int32":
                    case "System.Decimal":
                    case "System.Object[]":
                        throw new Exception("Json no formato errado");
                    default:
                        if (list.GetType().FullName.Contains("System.Collections.Generic.Dictionary"))
                        {
                            GetFiles(list, listFile, key);
                        }
                        else
                        {
                            throw new Exception("Objeto não reconhecido pelo sistema: " + list.GetType().FullName);
                        }
                        break;
                }
            }
        }

        private static void GetFiles(string json, List<string> listFile, string key)
        {
            List<fileChange> filesChange = new List<fileChange>();

            foreach (string file in listFile)
            {
                StreamReader s = new StreamReader(file);
                string fileName = pathOut + file.Replace(pathIn, string.Empty);
                FileInfo fileInfo = new FileInfo(fileName);
                fileName = fileInfo.FullName.Replace(fileInfo.Extension, string.Empty);
                fileChange fileChange = new fileChange { originalFileName = file, fileName = fileName, body = s.ReadToEnd() };
                filesChange.Add(fileChange);
                s.Close();
            }

            foreach (fileChange file in filesChange)
            {
                file.fileName = DuplaChave.Custon.Keys.ReplaceKeys(file.fileName, json);
                string body;
                if (File.Exists(file.fileName))
                {
                    StreamReader s = new StreamReader(file.fileName);
                    body = s.ReadToEnd();
                    s.Close();
                    if (!body.Contains("{{:NoUpdate}}"))
                    {
                        if (body.Contains("{{" + key + ":StartUpdate}}") && body.Contains("{{" + key + ":EndUpdate}}"))
                        {
                            string lineStart = DuplaChave.Custon.StringCuston.getLine(body, "{{" + key + ":StartUpdate}}");
                            string lineEnd = DuplaChave.Custon.StringCuston.getLine(body, "{{" + key + ":EndUpdate}}");

                            string block = DuplaChave.Custon.StringCuston.getBetween(body, lineStart, lineEnd);
                            body = body.Replace(block, lineStart + "\r\n" + DuplaChave.Custon.Keys.ReplaceKeys(file.body, json) + "\r\n" + lineEnd);
                        }
                        else if (body.Contains("{{" + key + ":Insert}}"))
                        {
                            string line = DuplaChave.Custon.StringCuston.getLine(body, "{{" + key + ":Insert}}");
                            body = body.Replace(line, line + "\r\n" + DuplaChave.Custon.Keys.ReplaceKeys(file.body, json));
                        }
                        else
                        {
                            body = DuplaChave.Custon.Keys.ReplaceKeys(file.body, json);
                        }
                    }
                }
                else
                {
                    body = DuplaChave.Custon.Keys.ReplaceKeys(file.body, json);
                }
                StreamWriter w = new StreamWriter(file.fileName, false);
                w.Write(body);
                w.Close();
            }
        }
    }

}
