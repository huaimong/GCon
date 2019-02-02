namespace VgcApis.Models.Interfaces
{
    // https://code.msdn.microsoft.com/windowsdesktop/Creating-a-simple-plugin-b6174b62
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }

        void Run(Models.IServices.IAllServices api);
        void Show();
        void Cleanup();
    }

}
