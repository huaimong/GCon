using System;

namespace VgcApis.Models.Interfaces.CoreCtrlComponents
{
    public interface ICoreCtrl
    {
        bool IsCoreRunning();

        void RunSpeedTest();

        void StopCore();
        void StopCoreThen();
        void StopCoreThen(Action next);

        void RestartCore();
        void RestartCoreThen();
        void RestartCoreThen(Action next);

        Datas.StatsSample TakeStatisticsSample();
    }
}
