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
            public string bodyOut { get; set; }
            public Dictionary<string, string> bodys { get; set; }
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
                fileName = fileInfo.FullName.Replace(fileInfo.Extension, string.Empty);
                fileChange fileChange = new fileChange { originalFileName = file, fileName = fileName, body = s.ReadToEnd() };
                fileChange.keys = GetKeys(fileChange.body + fileChange.fileName);
                fileChange.bodys = new Dictionary<string,string>();
                fileChange.bodyOut = string.Empty;
                filesChange.Add(fileChange);
                s.Close();
            }

            foreach (fileChange file in filesChange)
            {
                bool flagUpdate = false;
                foreach (string Key in file.keys)
                {
                    string realKey = Key;
                    string partKey = Key;
                    string KeyExtetion = Key;
                    if (partKey.Contains(":"))
                    {
                        string[] listKeys = partKey.Split(':');
                        partKey = listKeys[0];
                        if (listKeys[1] == "Update")
                        {
                            flagUpdate = true;
                        }
                        else
                        {
                            KeyExtetion = listKeys[0] + "." + listKeys[1];
                        }
                    }
                    if (!string.IsNullOrEmpty(partKey) && (((Dictionary<string, object>)dados).Keys.Contains(partKey)))
                    {
                        if (flagUpdate)
                        {
                            file.bodys.Add(partKey, GetValRetroative(dados, KeyExtetion, file.originalFileName));
                        }
                        else
                        {
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
                    else
                    {
                        file.fileName = file.fileName.Replace("{{" + realKey + "}}", string.Empty);
                        file.body = file.body.Replace("{{" + realKey + "}}", string.Empty);
                    }
                }
            }

            foreach (fileChange file in filesChange)
            {
                FileInfo fileInfo = new FileInfo(file.fileName);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                if (File.Exists(file.fileName))
                {
                    Dictionary<string, string> LinhasInserir = new Dictionary<string, string>();
                    Dictionary<string, DadosUpdate> LinhasAtualizar = new Dictionary<string, DadosUpdate>();
                    StreamReader reader = new StreamReader(file.fileName);
                    string allfileRead = reader.ReadToEnd();
                    reader.Close();

                    List<string> Keys = GetKeys(allfileRead);

                    reader = new StreamReader(file.fileName);
                    string line = string.Empty;
                    bool flagNoUpdate = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        foreach(string k in Keys)
                        {
                            if (line.Contains(k))
                            {
                                if(k.Contains(":"))
                                {
                                    string[] kParts = k.Split(':');
                                    switch (kParts[kParts.Length - 1])
                                    {
                                        case "NoUpdate":
                                            flagNoUpdate = true;
                                            break;
                                        case "Insert":
                                            if (kParts[0].Contains("."))
                                            {
                                                string[] kParts2 = kParts[0].Split('.');
                                                if (LinhasInserir.ContainsKey(kParts2[kParts2.Length - 1]))
                                                {
                                                    LinhasInserir[kParts2[kParts2.Length - 1]] = line;
                                                }
                                                else
                                                {
                                                    LinhasInserir.Add(kParts[kParts2.Length - 1], line);
                                                }
                                            }
                                            else
                                            {
                                                if (LinhasInserir.ContainsKey("Default" + kParts[0]))
                                                {
                                                    LinhasInserir["Default" + kParts[0]] = line;
                                                }
                                                else
                                                {
                                                    LinhasInserir.Add("Default" + kParts[0], line);
                                                }
                                            }
                                            break;
                                        case "StartUpdate":
                                            if (kParts[0].Contains("."))
                                            {
                                                string[] kParts2 = kParts[0].Split('.');
                                                if (LinhasInserir.ContainsKey(kParts2[kParts2.Length - 1]))
                                                {
                                                    LinhasAtualizar[kParts2[kParts2.Length - 1]] = new DadosUpdate { inicio = line, fim = null };
                                                }
                                                else
                                                {
                                                    LinhasAtualizar.Add(kParts[kParts2.Length - 1], new DadosUpdate { inicio = line, fim = null });
                                                }
                                            }
                                            else
                                            {
                                                if (LinhasInserir.ContainsKey("Default" + kParts[0]))
                                                {
                                                    LinhasAtualizar["Default" + kParts[0]] = new DadosUpdate { inicio = line, fim = null };
                                                }
                                                else
                                                {
                                                    LinhasAtualizar.Add("Default" + kParts[0], new DadosUpdate { inicio = line, fim = null });
                                                }
                                            }
                                            break;
                                        case "EndUpdate":
                                            if (kParts[0].Contains("."))
                                            {
                                                string[] kParts2 = kParts[0].Split('.');

                                                LinhasAtualizar[kParts2[kParts2.Length - 1]].fim = line;
                                            }
                                            else
                                            {
                                                LinhasAtualizar["Default" + kParts[0]].fim = line;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    if (k.Contains("."))
                                    {
                                        string[] kParts = k.Split('.');
                                        if (LinhasInserir.ContainsKey(kParts[kParts.Length-1]))
                                        {
                                            LinhasInserir[kParts[kParts.Length - 1]] = line;
                                        }
                                        else
                                        {
                                            LinhasInserir.Add(kParts[kParts.Length - 1], line);
                                        }
                                    }
                                    else
                                    {
                                        if(LinhasInserir.ContainsKey("Default" + k))
                                        {
                                            LinhasInserir["Default" + k] = line;
                                        }
                                        else
                                        {
                                            LinhasInserir.Add("Default" + k, line);
                                        }
                                    }
                                }
                            }
                        }
                        
                    }
                    reader.Close();
                    if (!flagNoUpdate)
                    {
                        if (LinhasInserir.Count > 0)
                        {
                            reader = new StreamReader(file.fileName);
                            string allfile = reader.ReadToEnd();
                            reader.Close();
                            foreach (KeyValuePair<string,string> i in LinhasInserir)
                            {
                                if ((i.Key == "Default") || (!file.bodys.ContainsKey(i.Key)))
                                {
                                    allfile = allfile.Replace(i.Value, i.Value + "\r\n" + file.body);
                                }
                                else
                                {
                                    allfile = allfile.Replace(i.Value, i.Value + "\r\n" + file.bodys[i.Key]);
                                }
                                
                            }
                            StreamWriter w = new StreamWriter(file.fileName, false);
                            w.Write(allfile);
                            w.Close();
                        }
                        if(LinhasAtualizar.Count > 0)
                        {
                            reader = new StreamReader(file.fileName);
                            string allfile = reader.ReadToEnd();
                            reader.Close();
                            foreach (KeyValuePair<string,DadosUpdate> i in LinhasAtualizar)
                            {
                                int pFrom = allfile.IndexOf(i.Value.inicio);
                                int pTo = allfile.LastIndexOf(i.Value.fim) + i.Value.fim.Length;
                                if ((i.Key.Contains("Default")) || (!file.bodys.ContainsKey(i.Key)))
                                {
                                    allfile = allfile.Replace(allfile.Substring(pFrom, pTo - pFrom), i.Value.inicio + "\r\n" + file.body + "\r\n" + i.Value.fim);
                                }
                                else
                                {
                                    allfile = allfile.Replace(allfile.Substring(pFrom, pTo - pFrom), i.Value.inicio + "\r\n" + file.bodys[i.Key] + "\r\n" + i.Value.fim);
                                }
                            }
                            StreamWriter w = new StreamWriter(file.fileName, false);
                            w.Write(allfile);
                            w.Close();
                        }
                        if ((LinhasInserir.Count == 0) && (LinhasAtualizar.Count == 0))
                        {
                            StreamWriter w = new StreamWriter(file.fileName, false);
                            w.Write(file.body);
                            w.Close();
                        }
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

                List<string> keys = GetKeys(_return);

                foreach (string Key in keys)
                {
                    string realKey = Key;
                    string partKey = Key;
                    string KeyExtetion = Key;
                    if (partKey.Contains(":"))
                    {
                        string[] listKeys = partKey.Split(':');
                        partKey = listKeys[0];
                        KeyExtetion = listKeys[0] + "." + listKeys[1];
                    }

                    if (((Dictionary<string, object>)data).Keys.Contains(partKey))
                    {
                        object entry = ((Dictionary<string, object>)data)[partKey];
                        switch (entry.GetType().FullName)
                        {
                            case "System.String":
                            case "System.Boolean":
                            case "System.Int32":
                            case "System.Decimal":
                                _return = _return.Replace("{{" + realKey + "}}", entry.ToString());
                                break;
                            case "System.Object[]":
                                _return += GetValList(entry, KeyExtetion, file + "." + key);
                                break;
                            default:
                                if (entry.GetType().FullName.Contains("System.Collections.Generic.Dictionary"))
                                {
                                    _return = _return.Replace("{{" + realKey + "}}", GetValRetroative(entry, KeyExtetion, file + "." + key));
                                }
                                else
                                {
                                    throw new Exception("Objeto não reconhecido pelo sistema: " + entry.GetType().FullName);
                                }
                                break;
                        }
                    }
                    else
                    {
                        _return = _return.Replace("{{" + realKey + "}}", string.Empty);
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

    public class DadosUpdate
    {
        public string inicio { get; set; }
        public string fim { get; set; }
    }

}
