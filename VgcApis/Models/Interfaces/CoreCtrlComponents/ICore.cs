using System;

namespace VgcApis.Models.Interfaces.CoreCtrlComponents
{
    public interface ICore
    {
        void RunSpeedTest();

        // 非正常终止时调用 
        void SetTitle(string title);

        void StopCore();
        void StopCoreThen();
        void StopCoreThen(Action next);

        bool IsCoreRunning();

        void RestartCore();
        void RestartCoreThen();
        void RestartCoreThen(Action next);

        Datas.StatsSample TakeStatisticsSample();
    }
}
