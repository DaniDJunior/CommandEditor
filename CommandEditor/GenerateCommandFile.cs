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

        public static void GenerateFiles(string jsonFile)
        {
            StreamReader fileJson = new StreamReader(jsonFile);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = fileJson.ReadToEnd();
            fileJson.Close();

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
            public List<string> keys { get; set; }
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

        public static List<string> GetKeys(string texto)
        {
            var regex = new Regex(@"{{([a-zA-Z0-9!:@#$&()\[\]\\-`.+,/\""]+)}}");
            List<string> chaves = new List<string>();
            foreach (Match i in regex.Matches(texto))
            {
                string chave = i.Value.Substring(0, i.Value.Length - 2).Substring(2);
                if (!chaves.Contains(chave))
                {
                    chaves.Add(chave);
                }
            }
            return chaves;
        }

        private static void GetFiles(dynamic dados, List<string> listFile, string key)
        {
            List<fileChange> filesChange = new List<fileChange>();

            foreach (string file in listFile)
            {
                StreamReader s = new StreamReader(file);
                string fileName = pathOut + file.Replace(pathIn, string.Empty);
                FileInfo fileInfo = new FileInfo(fileName);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                fileName = fileInfo.FullName.Replace(fileInfo.Extension, string.Empty);
                fileChange fileChange = new fileChange { originalFileName = file, fileName = fileName, body = s.ReadToEnd() };
                fileChange.keys = GetKeys(fileChange.body + fileChange.fileName);
                filesChange.Add(fileChange);
                s.Close();
            }

            foreach (fileChange file in filesChange)
            {
                foreach (string Key in file.keys)
                {
                    string realKey = Key;
                    string partKey = Key;
                    string KeyExtetion = Key;
                    if(partKey.Contains(":"))
                    {
                        string[] listKeys = partKey.Split(':');
                        partKey = listKeys[0];
                        KeyExtetion = listKeys[0] + "." + listKeys[1];
                    }



                    object entry = ((Dictionary<string, object>)dados)[partKey];
                    switch (entry.GetType().FullName)
                    {
                        case "System.String":
                        case "System.Boolean":
                        case "System.Int32":
                        case "System.Decimal":
                                file.fileName = file.fileName.Replace("{{" + realKey + "}}", entry.ToString());
                                file.body = file.body.Replace("{{" + realKey + "}}", entry.ToString());
                            break;
                        case "System.Object[]":
                                file.body = file.body.Replace("{{" + realKey + "}}", GetValList(entry, KeyExtetion, file.originalFileName));
                            break;
                        default:
                            if (entry.GetType().FullName.Contains("System.Collections.Generic.Dictionary"))
                            {
                                    file.body = file.body.Replace("{{" + realKey + "}}", GetValRetroative(entry, KeyExtetion, file.originalFileName));
                            }
                            else
                            {
                                throw new Exception("Objeto não reconhecido pelo sistema: " + entry.GetType().FullName);
                            }
                            break;
                    }
                }
            }

            foreach (fileChange file in filesChange)
            {
                if (File.Exists(file.fileName))
                {
                    List<string> linhas = new List<string>();
                    StreamReader reader = new StreamReader(file.fileName);
                    string line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("{{" + key + "}}"))
                        {
                            linhas.Add(line);
                        }
                    }
                    reader.Close();
                    if (linhas.Count > 0)
                    {
                        reader = new StreamReader(file.fileName);
                        string allfile = reader.ReadToEnd();
                        reader.Close();
                        foreach (string i in linhas)
                        {
                            allfile = allfile.Replace(i, i + "\r\n" + file.body);
                        }
                        StreamWriter w = new StreamWriter(file.fileName, false);
                        w.Write(allfile);
                        w.Close();
                    }
                    else
                    {
                        StreamWriter w = new StreamWriter(file.fileName, true);
                        w.Write(file.body);
                        w.Close();
                    }
                }
                else
                {
                    StreamWriter w = new StreamWriter(file.fileName, false);
                    w.Write(file.body);
                    w.Close();
                }
            }
        }

        private static string GetValRetroative(dynamic data, string key, string file)
        {
            if (File.Exists(file + "." + key))
            {
                StreamReader r = new StreamReader(file + "." + key);
                string _return = r.ReadToEnd();
                r.Close();

                foreach (KeyValuePair<string, object> entry in data)
                {
                    if (entry.Value != null)
                    {
                        switch (entry.Value.GetType().FullName)
                        {
                            case "System.String":
                            case "System.Boolean":
                            case "System.Int32":
                            case "System.Decimal":
                                _return = _return.Replace("{{" + entry.Key + "}}", entry.Value.ToString());
                                break;
                            case "System.Object[]":
                                _return += GetValList(entry.Value, entry.Key, file + "." + key);
                                break;
                            default:
                                if (entry.Value.GetType().FullName.Contains("System.Collections.Generic.Dictionary"))
                                {
                                    _return = _return.Replace("{{" + entry.Key + "}}", GetValRetroative(entry.Value, entry.Key, file + "." + key));
                                }
                                else
                                {
                                    throw new Exception("Objeto não reconhecido pelo sistema: " + entry.Value.GetType().FullName);
                                }
                                break;
                        }
                    }
                }
                return _return;
            }
            return string.Empty;
        }

        private static string GetValList(dynamic data, string key, string file)
        {
            string _return = string.Empty;
            foreach (object list in data)
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
                            _return += GetValRetroative(list, key, file);
                        }
                        else
                        {
                            throw new Exception("Objeto não reconhecido pelo sistema: " + list.GetType().FullName);
                        }
                        break;
                }
            }
            return _return;
        }
    }
}
