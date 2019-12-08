using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static void GenerateFiles(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            JObject Dados = (JObject)JsonConvert.DeserializeObject(json);
            pathIn = Dados.SelectToken("pathIn").Value<string>();

            pathOut = Dados.SelectToken("pathOut").Value<string>();
            if (!string.IsNullOrEmpty(pathIn) && !string.IsNullOrEmpty(pathOut))
            {
                JToken next = Dados.SelectToken("data").First();
                while (next != null)
                {
                    var valor = next.First();
                    string key = next.Path.Replace("data.", string.Empty);
                    List<string> files = GetDirectory(pathIn, key);
                    if (valor.GetType().Name == "JObject")
                    {
                        GetFiles(valor.ToString(), files, key);
                    }
                    if (valor.GetType().Name == "JArray")
                    {
                        foreach (JToken JSonData in valor)
                        {
                            GetFiles(JSonData.ToString(), files, key);
                        }
                    }
                    next = next.Next;

                }
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

        private class fileChange
        {
            public string originalFileName { get; set; }
            public string fileName { get; set; }
            public string body { get; set; }
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

                            foreach (var block in DuplaChave.Custon.StringCuston.getBetween(body, lineStart, lineEnd))
                            {
                                body = body.Replace(block, lineStart + "\r\n" + DuplaChave.Custon.Keys.ReplaceKeys(file.body, json) + "\r\n" + lineEnd);
                            }
                            
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
                FileInfo info = new FileInfo(file.fileName);
                if(!Directory.Exists(info.DirectoryName))
                {
                    Directory.CreateDirectory(info.DirectoryName);
                }
                StreamWriter w = new StreamWriter(file.fileName, false);
                w.Write(body);
                w.Close();
            }
        }
    }

}
