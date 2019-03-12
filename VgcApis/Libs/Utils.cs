using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScintillaNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VgcApis.Libs
{
    public static class Utils
    {

        #region net
        static readonly IPEndPoint _defaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
        static readonly object getFreeTcpPortLocker = new object();
        public static int GetFreeTcpPort()
        {
            // https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            var port = -1;
            lock (getFreeTcpPortLocker)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(_defaultLoopbackEndpoint);
                    port = ((IPEndPoint)socket.LocalEndPoint).Port;
                }
            }
            return port;
        }
        #endregion

        #region Task
        public static void Sleep(int milliseconds) =>
          System.Threading.Thread.Sleep(milliseconds);

        public static Task RunInBackground(Action worker) =>
            Task.Factory.StartNew(worker, TaskCreationOptions.LongRunning);
        #endregion

        #region Json
        public static Dictionary<string, string> GetterJsonSections(
            JToken jtoken)
        {
            var rootKey = Models.Consts.Config.ConfigSectionDefRootKey;
            var defDepth = Models.Consts.Config.ConfigSectionDefDepth;
            var setting = Models.Consts.Config.ConfigSectionDefSetting;

            var ds = new Dictionary<string, string>();

            GetterJsonDataStructRecursively(
                ref ds, jtoken, rootKey, defDepth, setting);

            ds.Remove(rootKey);

            int index = rootKey.Length + 1;
            return ds
                .Select(kv => new KeyValuePair<string, string>(kv.Key.Substring(index), kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        static bool IsValidJobjectKey(string key)
        {
            if (string.IsNullOrEmpty(key)
                || int.TryParse(key, out int blackhole))
            {
                return false;
            }

            return true;
        }

        static void GetterJsonDataStructRecursively(
            ref Dictionary<string, string> sections,
            JToken jtoken,
            string root,
            int depth,
            Dictionary<string, int> setting)
        {
            if (depth < 0)
            {
                return;
            }

            if (setting.ContainsKey(root))
            {
                depth = setting[root];
            }

            switch (jtoken)
            {
                case JObject jobject:
                    sections[root] = Models.Consts.Config.JsonObject;
                    foreach (var prop in jobject.Properties())
                    {
                        var key = prop.Name;
                        if (!IsValidJobjectKey(key))
                        {
                            continue;
                        }
                        var subRoot = $"{root}.{key}";
                        GetterJsonDataStructRecursively(
                           ref sections, jobject[key], subRoot, depth - 1, setting);
                    }
                    break;

                case JArray jarry:
                    sections[root] = Models.Consts.Config.JsonArray;
                    for (int i = 0; i < jarry.Count(); i++)
                    {
                        var key = i;
                        var subRoot = $"{root}.{key}";
                        GetterJsonDataStructRecursively(
                            ref sections, jarry[key], subRoot, depth - 1, setting);
                    }
                    break;
                default:
                    break;
            }
        }

        public static string TrimConfig(string config)
        {
            try
            {
                var cfg = JObject.Parse(config);
                return cfg?.ToString(Formatting.None);
            }
            catch { }
            return null;
        }

        public static bool TryParseJObject(
           string jsonString, out JObject json)
        {
            json = null;
            try
            {
                json = JObject.Parse(jsonString);
                return true;
            }
            catch { }
            return false;
        }

        public static void SavePluginSetting<T>(
            string pluginName,
            T userSettings,
            Models.IServices.ISettingsService vgcSetting)
            where T : class
        {
            var content = Utils.SerializeObject(userSettings);
            vgcSetting.SavePluginsSetting(pluginName, content);
        }

        public static T LoadPluginSetting<T>(
            string pluginName,
            Models.IServices.ISettingsService vgcSetting)
            where T : class, new()
        {
            var empty = new T();
            var userSettingString =
                vgcSetting.GetPluginsSetting(pluginName);

            if (string.IsNullOrEmpty(userSettingString))
            {
                return empty;
            }

            try
            {
                var result = VgcApis.Libs.Utils
                    .DeserializeObject<T>(userSettingString);
                return result ?? empty;
            }
            catch { }
            return empty;
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string content) where T : class
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(content);
                if (result != null)
                {
                    return result;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <param name="serializeObject"></param>
        /// <returns></returns>
        public static string SerializeObject(object serializeObject)
        {
            if (serializeObject == null)
            {
                return null;
            }
            return JsonConvert.SerializeObject(serializeObject);
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <returns></returns>
        public static T Clone<T>(T a) where T : class
        {
            if (a == null)
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(
                    JsonConvert.SerializeObject(a));
            }
            catch { }
            return null;
        }
        #endregion

        #region string processor
        public static List<string> ExtractBase64Strings(string text)
        {
            var b64s = new List<string>();
            var matches = Regex.Matches(
                text,
                Models.Consts.Patterns.Base64NonStandard);
            foreach (Match match in matches)
            {
                b64s.Add(match.Value);
            }
            return b64s;
        }

        public static string GetFragment(
            Scintilla editor,
            string searchPattern)
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            var selectedText = editor.SelectedText;
            if (selectedText.Length > 0)
            {
                return selectedText;
            }

            string text = editor.Text;
            var regex = new Regex(searchPattern);

            var startPos = editor.CurrentPosition;

            //go forward
            int i = startPos;
            while (i >= 0 && i < text.Length)
            {
                if (!regex.IsMatch(text[i].ToString()))
                    break;
                i++;
            }

            var endPos = i;

            //go backward
            i = startPos;
            while (i > 0 && (i - 1) < text.Length)
            {
                if (!regex.IsMatch(text[i - 1].ToString()))
                    break;
                i--;
            }
            startPos = i;

            return GetSubString(startPos, endPos, text);
        }

        static string GetSubString(int start, int end, string text)
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            if (string.IsNullOrEmpty(text))
                return "";
            if (start >= text.Length)
                return "";
            if (end > text.Length)
                return "";

            return text.Substring(start, end - start);
        }

        public static bool PartialMatchCi(string source, string partial) =>
            PartialMatch(source.ToLower(), partial.ToLower());

        public static bool PartialMatch(string source, string partial) =>
            MeasureSimilarity(source, partial) > 0;

        public static long MeasureSimilarityCi(string source, string partial) =>
            MeasureSimilarity(source.ToLower(), partial.ToLower());

        /// <summary>
        /// -1: not match 1: equal >=2: the smaller the value, the more similar
        /// </summary>
        public static long MeasureSimilarity(string source, string partial)
        {
            if (string.IsNullOrEmpty(partial))
            {
                return 1;
            }

            if (string.IsNullOrEmpty(source))
            {
                return -1;
            }

            long marks = 1;

            var s = source;
            var p = partial;

            int idxS = 0, idxP = 0;
            int lenS = s.Length, lenP = p.Length;
            while (idxS < lenS && idxP < lenP)
            {
                if (s[idxS] == p[idxP])
                {
                    idxP++;
                }
                else
                {
                    marks += lenP - idxP;
                }
                idxS++;
            }

            if (idxP != lenP)
            {
                return -1;
            }

            return marks;
        }

        public static string GetLinkPrefix(string shareLink)
        {
            var index = shareLink.IndexOf(@"://");
            if (index == -1)
            {
                return null;
            }

            var prefix = shareLink.Substring(0, index);
            return prefix.ToLower();
        }

        public static Models.Datas.Enum.LinkTypes DetectLinkType(
            string shareLink)
        {
            var unknow = Models.Datas.Enum.LinkTypes.unknow;
            var prefix = GetLinkPrefix(shareLink);
            if (!string.IsNullOrEmpty(prefix)
                && Enum.TryParse(prefix, out Models.Datas.Enum.LinkTypes linkType))
            {
                return linkType;
            }
            return unknow;
        }

        /// <summary>
        /// regex = @"(?&lt;groupName>pattern)"
        /// <para>return string.Empty if sth. goes wrong</para>
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="pattern"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string ExtractStringWithPattern(string groupName, string pattern, string content)
        {
            var ptnStr = string.Format(@"(?<{0}>{1})", groupName, pattern);
            Regex rgx = new Regex(ptnStr);
            Match match = rgx.Match(content ?? string.Empty);
            if (match.Success)
            {
                return match.Groups[groupName].Value;
            }
            return string.Empty;
        }

        #endregion

        #region numbers
        public static int GetLenInBitsOfInt(int value)
        {
            var k = 0;
            while (value > 0)
            {
                value = value >> 1;
                k++;
            }
            return value < 0 ? -1 : k;
        }

        #endregion

        #region Misc
        public static bool IsImportResultSuccess(string[] result) =>
           result[3] == VgcApis.Models.Consts.Import.MarkImportSuccess;

        public static void TrimDownConcurrentQueue<T>(
            ConcurrentQueue<T> queue,
            int maxLines,
            int minLines)
        {
            var count = queue.Count();
            if (maxLines < 1 || count < maxLines)
            {
                return;
            }

            var loop = Clamp(count - minLines, 0, count);
            var blackHole = default(T);
            for (int i = 0; i < loop; i++)
            {
                queue.TryDequeue(out blackHole);
            }
        }

        public static bool IsHttpLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return false;
            }
            if (link.ToLower().StartsWith("http"))
            {
                return true;
            }
            return false;
        }

        public static string RelativePath2FullPath(string path)
        {
            if (string.IsNullOrEmpty(path)
                || Path.IsPathRooted(path))
            {
                return path;
            }

            var appDir = GetAppDir();
            return Path.Combine(appDir, path);
        }

        public static bool CopyToClipboard(string content)
        {
            try
            {
                Clipboard.SetText(content);
                return true;
            }
            catch { }
            return false;
        }

        // Assembly location may change while app running.
        // So we should cache it when app starts.
        static string appDirCache = GenAppDir();
        static string GenAppDir()
        {
            // z:\vgc\libs\vgcapi.dll
            var vgcApiDllFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var parent = new DirectoryInfo(vgcApiDllFile).Parent;
            if (parent.Name == "libs")
            {
                parent = parent.Parent;
            }
            return parent.FullName;
        }

        public static string GetCoreFolderFullPath() =>
            Path.Combine(GetAppDir(), Models.Consts.Files.CoreFolderName);

        public static string GetAppDir() => appDirCache;

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max - 1), min);
        }

        public static string RandomHex(int length)
        {
            //  https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
            if (length <= 0)
            {
                return string.Empty;
            }

            Random random = new Random();
            const string chars = "0123456789abcdef";
            return new string(
                Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());
        }

        public static int Str2Int(string value)
        {
            if (float.TryParse(value, out float f))
            {
                return (int)Math.Round(f);
            };
            return 0;
        }
        #endregion

        #region reflection
        public static string GetFriendlyName(Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        /// <summary>
        /// [0: ReturnType 1: MethodName 2: ParamsStr 3: ParamsWithType]
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Tuple<string, string, string, string>> GetPublicMethodNameAndParam(Type type)
        {
            var exceptList = new List<string>
            {
                "add_OnPropertyChanged",
                "remove_OnPropertyChanged",
            };

            var fullNames = new List<Tuple<string, string, string, string>>();
            var methods = type.GetMethods();
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (method.IsPublic && !exceptList.Contains(name))
                {
                    var paramStrs = GenParamStr(method);
                    var returnType = GetFriendlyName(method.ReturnType);
                    fullNames.Add(
                        new Tuple<string, string, string, string>(
                            returnType, name, paramStrs.Item1, paramStrs.Item2));
                }
            }
            return fullNames;
        }

        static Tuple<string, string> GenParamStr(System.Reflection.MethodInfo methodInfo)
        {
            var fullParamList = new List<string>();
            var paramList = new List<string>();

            foreach (var paramInfo in methodInfo.GetParameters())
            {

                fullParamList.Add(
                    paramInfo.ParameterType.Name +
                    " " +
                    paramInfo.Name);

                paramList.Add(paramInfo.Name);
            }

            return new Tuple<string, string>(
                string.Join(@", ", paramList),
                string.Join(@", ", fullParamList));
        }

        public static List<string> GetPublicMethodNames(Type type)
        {
            var exceptList = new List<string>
            {
                "add_OnPropertyChanged",
                "remove_OnPropertyChanged",
            };

            var methodsName = new List<string>();
            var methods = type.GetMethods();
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (method.IsPublic && !exceptList.Contains(name))
                {
                    methodsName.Add(name);
                }
            }
            return methodsName;
        }
        #endregion
    }
}
