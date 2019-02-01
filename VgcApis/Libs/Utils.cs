using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VgcApis.Libs
{
    public static class Utils
    {
        public static void Sleep(int milliseconds) =>
           System.Threading.Thread.Sleep(milliseconds);

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

        public static void SavePluginSetting<T>(
            string pluginName,
            T userSettings,
            Models.IServices.ISettingService vgcSetting)
            where T : class
        {
            try
            {
                var content = Utils.SerializeObject(userSettings);
                vgcSetting.SavePluginsSetting(pluginName, content);
            }
            catch { }
        }

        public static T LoadPluginSetting<T>(
            string pluginName,
            Models.IServices.ISettingService vgcSetting)
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

        #region Json
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

        #region Misc
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
            var vgcapiDllFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var libsDirInfo = System.IO.Directory.GetParent(vgcapiDllFile);
            var vgcDirInfo = System.IO.Directory.GetParent(libsDirInfo.ToString());

            return vgcDirInfo.ToString();
        }

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
    }
}
