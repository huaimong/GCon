using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProxySetter.Lib.Sys
{
    // https://github.com/NoNeil/ProxySetting/blob/master/MyProxy/ProxySetting.cs
    public static class WinInet
    {
        const int QueryOptionCount = 4;

        #region public methods
        public static Model.Data.ProxyRegKeyValue GetProxySettings()
        {
            var query = GenOptionQurey(true);
            var success = false;
            var result = new Model.Data.ProxyRegKeyValue();

            void action(IntPtr _listPtr, int _listSize)
            {
                var newSize = _listSize;
                success = InternetQueryOption(
                    IntPtr.Zero,
                    InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                    _listPtr, ref newSize);
                if (success)
                {
                    InternetPerConnOptionList list =
                        (InternetPerConnOptionList)Marshal.PtrToStructure(
                            _listPtr,
                            typeof(InternetPerConnOptionList));

                    var options = IntPtrToOptionArray(list.options, QueryOptionCount);

                    var proxyServer = Marshal.PtrToStringAuto(options[1].m_Value.m_StringPtr);
                    var autoConfigUrl = Marshal.PtrToStringAuto(options[2].m_Value.m_StringPtr);
                    result.proxyEnable = !(string.IsNullOrEmpty(proxyServer) && string.IsNullOrEmpty(autoConfigUrl));
                    result.proxyServer = proxyServer ?? string.Empty;
                    result.autoConfigUrl = autoConfigUrl ?? string.Empty;
                }
            }

            SystemProxyHandler(GenOptionQurey(true), action);
            if (!success)
            {
                // try again
                SystemProxyHandler(GenOptionQurey(false), action);
            }
            return result;
        }

        static InternetConnectionOption[] IntPtrToOptionArray(IntPtr ptr, int size)
        {
            var result = new List<InternetConnectionOption>();

            int optSize = Marshal.SizeOf(typeof(InternetConnectionOption));

            // copy the array over into that spot in memory ...
            for (int i = 0; i < size; ++i)
            {
                IntPtr opt = new IntPtr(ptr.ToInt32() + (i * optSize));
                var option =
                    (InternetConnectionOption)Marshal.PtrToStructure(
                        opt,
                        typeof(InternetConnectionOption));
                result.Add(option);
            }

            return result.ToArray();
        }

        public static bool UpdateProxySettings(Model.Data.ProxyRegKeyValue proxySettings)
        {
            var result = false;

            if (!proxySettings.proxyEnable)
            {
                result = SetProxyOption(false, null, null);
            }
            else
            {
                if (!string.IsNullOrEmpty(proxySettings.proxyServer))
                {
                    result = SetProxyOption(false, proxySettings.proxyServer, null);
                }
                else
                {
                    result = SetProxyOption(true, proxySettings.autoConfigUrl, null);
                }
            }
            ForceSysProxyRefresh();
            return result;
        }

        public static void ForceSysProxyRefresh()
        {
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        #endregion

        #region helper functions
        static bool SetProxyOption(bool isPac, string proxy, string bypass)
        {
            var options = GenOptionProxy(isPac, proxy, bypass);
            var result = false;

            void action(IntPtr listPtr, int listSize)
            {
                result = InternetSetOption(
                    IntPtr.Zero,
                    InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                    listPtr,
                    listSize);
            }

            SystemProxyHandler(options, action);
            return result;
        }

        static InternetConnectionOption[] GenOptionQurey(bool isWin7OrNewer)
        {
            InternetConnectionOption[] options = new InternetConnectionOption[QueryOptionCount];
            options[0].m_Option = isWin7OrNewer ? PerConnOption.INTERNET_PER_CONN_FLAGS_UI : PerConnOption.INTERNET_PER_CONN_FLAGS;
            options[1].m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_SERVER;
            options[2].m_Option = PerConnOption.INTERNET_PER_CONN_AUTOCONFIG_URL;
            options[3].m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_BYPASS;
            return options;
        }

        static InternetConnectionOption[] GenOptionDirect()
        {
            InternetConnectionOption[] options = new InternetConnectionOption[1];
            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;
            options[0].m_Value.m_Int = (int)(PerConnFlags.PROXY_TYPE_DIRECT);
            return options;
        }

        static InternetConnectionOption[] GenOptionProxy(bool isPac, string proxy, string bypass)
        {
            if (string.IsNullOrEmpty(proxy))
            {
                return GenOptionDirect();
            }

            bool isBypassEmpty = string.IsNullOrEmpty(bypass);
            InternetConnectionOption[] options =
                new InternetConnectionOption[isBypassEmpty ? 2 : 3];

            // USE a proxy server ...
            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;
            options[0].m_Value.m_Int = (int)(isPac ?
                (PerConnFlags.PROXY_TYPE_AUTO_PROXY_URL | PerConnFlags.PROXY_TYPE_DIRECT) :
                (PerConnFlags.PROXY_TYPE_DIRECT | PerConnFlags.PROXY_TYPE_PROXY));

            options[1].m_Option = isPac ?
                PerConnOption.INTERNET_PER_CONN_AUTOCONFIG_URL :
                PerConnOption.INTERNET_PER_CONN_PROXY_SERVER;

            options[1].m_Value.m_StringPtr = Marshal.StringToHGlobalAuto(proxy);

            if (!isBypassEmpty)
            {
                options[2].m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_BYPASS;
                options[2].m_Value.m_StringPtr = Marshal.StringToHGlobalAuto(bypass);
            }

            return options;
        }

        internal static void SystemProxyHandler(InternetConnectionOption[] options, Action<IntPtr, int> action)
        {
            InternetPerConnOptionList list = new InternetPerConnOptionList();

            // default stuff
            list.dwSize = Marshal.SizeOf(list);
            list.szConnection = IntPtr.Zero;
            list.dwOptionCount = options.Length;
            list.dwOptionError = 0;

            int optSize = Marshal.SizeOf(typeof(InternetConnectionOption));
            // make a pointer out of all that ...
            IntPtr optionsPtr = Marshal.AllocCoTaskMem(optSize * options.Length);
            // copy the array over into that spot in memory ...
            for (int i = 0; i < options.Length; ++i)
            {
                IntPtr opt = new IntPtr(optionsPtr.ToInt32() + (i * optSize));
                Marshal.StructureToPtr(options[i], opt, false);
            }

            list.options = optionsPtr;

            // and then make a pointer out of the whole list
            IntPtr ipcoListPtr = Marshal.AllocCoTaskMem((Int32)list.dwSize);
            Marshal.StructureToPtr(list, ipcoListPtr, false);

            // and finally, call the API method!
            action(ipcoListPtr, list.dwSize);

            // FREE the data ASAP
            Marshal.FreeCoTaskMem(optionsPtr);
            Marshal.FreeCoTaskMem(ipcoListPtr);
        }
        #endregion

        #region WinInet structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct InternetPerConnOptionList
        {
            public int dwSize;               // size of the INTERNET_PER_CONN_OPTION_LIST struct
            public IntPtr szConnection;         // connection name to set/query options
            public int dwOptionCount;        // number of options to set/query
            public int dwOptionError;           // on error, which option failed
            //[MarshalAs(UnmanagedType.)]
            public IntPtr options;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct InternetConnectionOption
        {
            static readonly int Size;
            public PerConnOption m_Option;
            public InternetConnectionOptionValue m_Value;
            static InternetConnectionOption()
            {
                InternetConnectionOption.Size = Marshal.SizeOf(typeof(InternetConnectionOption));
            }

            // Nested Types
            [StructLayout(LayoutKind.Explicit)]
            public struct InternetConnectionOptionValue
            {
                // Fields
                [FieldOffset(0)]
                public System.Runtime.InteropServices.ComTypes.FILETIME m_FileTime;
                [FieldOffset(0)]
                public int m_Int;
                [FieldOffset(0)]
                public IntPtr m_StringPtr;
            }
        }
        #endregion

        #region WinInet enums
        public enum OperationType
        {
            GetProxyOption,
            SetProxyOption,
        }

        //
        // options manifests for Internet{Query|Set}Option
        //
        public enum InternetOption : uint
        {
            INTERNET_OPTION_SETTINGS_CHANGED = 39,
            INTERNET_OPTION_REFRESH = 37,
            INTERNET_OPTION_PER_CONNECTION_OPTION = 75,
        }

        //
        // Options used in INTERNET_PER_CONN_OPTON struct
        //
        public enum PerConnOption
        {
            INTERNET_PER_CONN_FLAGS = 1, // Sets or retrieves the connection type. The Value member will contain one or more of the values from PerConnFlags 
            INTERNET_PER_CONN_PROXY_SERVER = 2, // Sets or retrieves a string containing the proxy servers.  
            INTERNET_PER_CONN_PROXY_BYPASS = 3, // Sets or retrieves a string containing the URLs that do not use the proxy server.  
            INTERNET_PER_CONN_AUTOCONFIG_URL = 4, // Sets or retrieves a string containing the URL to the automatic configuration script.  
            INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5,
            INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6,
            INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9,
            INTERNET_PER_CONN_FLAGS_UI = 10,
        }

        //
        // PER_CONN_FLAGS
        //
        [Flags]
        public enum PerConnFlags
        {
            PROXY_TYPE_DIRECT = 0x00000001,  // direct to net
            PROXY_TYPE_PROXY = 0x00000002,  // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,  // autoproxy URL
            PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection
        }
        #endregion

        #region wininet.dll
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool InternetQueryOption(IntPtr hInternet, InternetOption dwOption, IntPtr lpBuffer, ref int lpdwBufferLength);

        [DllImport("WinInet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool InternetSetOption(IntPtr hInternet, InternetOption dwOption, IntPtr lpBuffer, int dwBufferLength);
        #endregion

    }
}
