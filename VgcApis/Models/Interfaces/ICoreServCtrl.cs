namespace VgcApis.Models.Interfaces
{
    public interface ICoreServCtrl
    {
        CoreCtrlComponents.ICoreStates GetCoreStates();
        CoreCtrlComponents.ICoreCtrl GetCoreCtrl();
        CoreCtrlComponents.ILogger GetLogger();
        CoreCtrlComponents.IConfiger GetConfiger();
    }
}
