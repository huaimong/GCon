namespace VgcApis.Models.IServices
{
    public interface ISettingsService
    {
        bool IsShutdown();
        void SendLog(string log);
        void SavePluginsSetting(string pluginName, string value);
        string GetPluginsSetting(string pluginName);
    }
}
