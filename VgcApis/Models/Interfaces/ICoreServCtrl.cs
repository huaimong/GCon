namespace VgcApis.Models.Interfaces
{
    public interface ICoreServCtrl
    {
        /****************     new      ****************************/

        CoreCtrlComponents.IStates GetStates();
        CoreCtrlComponents.ICore GetCoreCtrl();
        CoreCtrlComponents.ILogger GetLogger();
        CoreCtrlComponents.IConfig GetConfiger();

        void InvokeEventOnRequireNotifierUpdate();
        void InvokeEventOnCoreClosing();
        void InvokeEventOnRequireKeepTrack(bool isServerStart);


    }
}
