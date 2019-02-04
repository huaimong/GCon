using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.CoreServerComponent
{
    sealed public class Config :
        VgcApis.Models.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Models.Interfaces.CoreCtrlComponents.IConfig
    {
        Service.Setting setting;
        Service.Cache cache;
        Service.Servers servers;
        VgcApis.Models.Datas.CoreInfo coreInfo;

        public Config(
            Service.Setting setting,
            Service.Cache cache,
            Service.Servers servers,

            VgcApis.Models.Datas.CoreInfo coreInfo)
        {
            this.setting = setting;
            this.servers = servers;

            this.coreInfo = coreInfo;
        }

        States states;
        CoreServerCtrl container;
        Logger logger;
        Core coreCtrl;
        public override void Prepare()
        {
            container = GetContainer();
            states = container.GetComponent<States>();
            logger = container.GetComponent<Logger>();
            coreCtrl = container.GetComponent<Core>();
        }

        #region public methods
        public void UpdateSummaryThen(Action next = null)
        {
            Task.Factory.StartNew(() =>
            {
                var configString = coreInfo.isInjectImport ?
                    InjectGlobalImport(coreInfo.config, false, true) :
                    coreInfo.config;
                try
                {
                    UpdateSummary(
                        servers.ParseImport(configString));
                }
                catch
                {
                    UpdateSummary(JObject.Parse(configString));
                }

                // update summary should not clear status
                // this.status = string.Empty;
                container.InvokeEventOnPropertyChange();
                next?.Invoke();
            });
        }

        void UpdateSummary(JObject config)
        {
            coreInfo.name = Lib.Utils.GetAliasFromConfig(config);
            coreInfo.summary = Lib.Utils.GetSummaryFromConfig(config);
        }


        bool IsProtocolMatchProxyRequirment(bool isGlobalProxy, string protocol)
        {
            if (isGlobalProxy && protocol != "http")
            {
                return false;
            }

            if (protocol != "socks" && protocol != "http")
            {
                return false;
            }

            return true;
        }


        public bool IsSuitableToBeUsedAsSysProxy(
          bool isGlobal,
          out bool isSocks,
          out int port)
        {
            isSocks = false;
            port = 0;

            var inboundInfo = GetParsedInboundInfo();
            if (inboundInfo == null)
            {
                logger.Log(I18N.GetInboundInfoFail);
                return false;
            }

            var protocol = inboundInfo.Item1;
            port = inboundInfo.Item3;

            if (!IsProtocolMatchProxyRequirment(isGlobal, protocol))
            {
                return false;
            }

            isSocks = protocol == "socks";
            return true;
        }

        public void ChangeCoreConfig(string newConfig)
        {
            if (coreInfo.config == newConfig)
            {
                return;
            }

            coreInfo.config = newConfig;
            container.InvokeEventOnPropertyChange();
            UpdateSummaryThen(() =>
            {
                container.InvokeEventOnRequireMenuUpdate();
            });

            if (coreCtrl.IsCoreRunning())
            {
                coreCtrl.RestartCoreThen();
            }
        }


        /// <summary>
        /// return Tuple(protocol, ip, port)
        /// </summary>
        /// <returns></returns>
        Tuple<string, string, int> GetParsedInboundInfo()
        {
            var protocol = GetInProtocolNameByNumber(coreInfo.customInbType);
            var ip = coreInfo.inbIp;
            var port = coreInfo.inbPort;

            if (protocol != "config")
            {
                return new Tuple<string, string, int>(protocol, ip, port);
            }

            var parsedConfig = GetDecodedConfig(true, false, true);
            if (parsedConfig == null)
            {
                return null;
            }

            string prefix = "inbound";
            foreach (var p in new string[] { "inbound", "inbounds.0" })
            {
                prefix = p;
                protocol = Lib.Utils.GetValue<string>(parsedConfig, prefix, "protocol");
                if (!string.IsNullOrEmpty(protocol))
                {
                    break;
                }
            }

            ip = Lib.Utils.GetValue<string>(parsedConfig, prefix, "listen");
            port = Lib.Utils.GetValue<int>(parsedConfig, prefix, "port");
            return new Tuple<string, string, int>(protocol, ip, port);
        }

        public void GetterInboundInfoFor(Action<string> next)
        {
            var serverName = coreInfo.name;
            Task.Factory.StartNew(() =>
            {
                var inInfo = GetParsedInboundInfo();
                if (inInfo == null)
                {
                    next(string.Format("[{0}]", serverName));
                    return;
                }
                if (string.IsNullOrEmpty(inInfo.Item2))
                {
                    next(string.Format("[{0}] {1}", serverName, inInfo.Item1));
                    return;
                }
                next(string.Format("[{0}] {1}://{2}:{3}",
                    serverName,
                    inInfo.Item1,
                    inInfo.Item2,
                    inInfo.Item3));
            });
        }


        public string PrepareSpeedTestConfig(int port)
        {
            var empty = string.Empty;
            if (port <= 0)
            {
                return empty;
            }

            var config = GetDecodedConfig(true, true, false);

            if (config == null)
            {
                return empty;
            }

            if (!OverwriteInboundSettings(
                ref config,
                (int)Model.Data.Enum.ProxyTypes.HTTP,
                "127.0.0.1",
                port))
            {
                return empty;
            }

            return config.ToString(Formatting.None);
        }

        private void ReplaceInboundSetting(ref JObject config, JObject o)
        {
            // Bug. Stream setting will mess things up.
            // Lib.Utils.MergeJson(ref config, o);
            var hasInbound = Lib.Utils.GetKey(config, "inbound") != null;
            var hasInbounds = Lib.Utils.GetKey(config, "inbounds.0") != null;
            var isUseV4 = !hasInbound && (hasInbounds || setting.isUseV4);

            if (isUseV4)
            {
                if (!hasInbounds)
                {
                    config["inbounds"] = JArray.Parse(@"[{}]");
                }
                config["inbounds"][0] = o;
            }
            else
            {
                config["inbound"] = o;
            }
        }

        private JObject CreateInboundSetting(int inboundType, string ip, int port, string protocol, string part)
        {
            var o = JObject.Parse(@"{}");
            o["tag"] = "agentin";
            o["protocol"] = protocol;
            o["listen"] = ip;
            o["port"] = port;
            o["settings"] = cache.tpl.LoadTemplate(part);

            if (inboundType == (int)Model.Data.Enum.ProxyTypes.SOCKS)
            {
                o["settings"]["ip"] = ip;
            }

            return o;
        }

        string GetInProtocolNameByNumber(int typeNumber)
        {
            var table = Model.Data.Table.customInbTypeNames;
            return table[Lib.Utils.Clamp(typeNumber, 0, table.Length)];
        }

        public void InjectSkipCnSiteSettingsIntoConfig(ref JObject config)
        {
            if (!coreInfo.isInjectSkipCNSite)
            {
                return;
            }

            // 优先考虑兼容旧配置。
            servers.InjectSkipCnSiteSettingsIntoConfig(
                ref config,
                false);
        }

        public void InjectStatsSettingsIntoConfig(ref JObject config)
        {
            if (!setting.isEnableStatistics)
            {
                return;
            }

            var freePort = Lib.Utils.GetFreeTcpPort();
            if (freePort <= 0)
            {
                return;
            }

            states.SetStatPort(freePort);

            var result = cache.tpl.LoadTemplate("statsApiV4Inb") as JObject;
            result["inbounds"][0]["port"] = freePort;
            Lib.Utils.CombineConfig(ref result, config);
            result["inbounds"][0]["tag"] = "agentin";

            var statsTpl = cache.tpl.LoadTemplate("statsApiV4Tpl") as JObject;
            Lib.Utils.CombineConfig(ref result, statsTpl);
            config = result;
        }

        public bool OverwriteInboundSettings(
            ref JObject config,
            int inboundType,
            string ip,
            int port)
        {
            switch (inboundType)
            {
                case (int)Model.Data.Enum.ProxyTypes.HTTP:
                case (int)Model.Data.Enum.ProxyTypes.SOCKS:
                    break;
                default:
                    return true;
            }

            var protocol = GetInProtocolNameByNumber(inboundType);
            var part = protocol + "In";
            try
            {
                JObject o = CreateInboundSetting(inboundType, ip, port, protocol, part);
                ReplaceInboundSetting(ref config, o);
#if DEBUG
                var debug = config.ToString(Formatting.Indented);
#endif
                return true;
            }
            catch
            {
                logger.Log(I18N.CoreCantSetLocalAddr);
            }
            return false;
        }

        public string InjectGlobalImport(
            string config,
            bool isIncludeSpeedTest,
            bool isIncludeActivate)
        {
            JObject import = Lib.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                isIncludeSpeedTest,
                isIncludeActivate);

            Lib.Utils.MergeJson(ref import, JObject.Parse(config));
            return import.ToString();
        }

        public JObject GetDecodedConfig(
            bool isUseCache,
            bool isIncludeSpeedTest,
            bool isIncludeActivate)
        {
            var coreConfig = coreInfo.config;
            JObject decodedConfig = null;

            try
            {
                string injectedConfig;
                if (coreInfo.isInjectImport)
                {
                    injectedConfig = InjectGlobalImport(
                        coreInfo.config,
                        isIncludeSpeedTest,
                        isIncludeActivate);
                }
                else
                {
                    injectedConfig = coreConfig;
                }
                decodedConfig = servers.ParseImport(injectedConfig);
                cache.core[coreConfig] = decodedConfig.ToString(Formatting.None);
            }
            catch { }

            if (decodedConfig == null)
            {
                logger.Log(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        decodedConfig = JObject.Parse(cache.core[coreConfig]);
                    }
                    catch (KeyNotFoundException) { }
                    logger.Log(I18N.UsingDecodeCache);
                }
            }

            return decodedConfig;
        }


        #endregion

        #region private methods
        #endregion
    }
}
