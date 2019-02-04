using System;

namespace VgcApis.Models.Interfaces.CoreCtrlComponents
{
    public interface ICoreStates
    {
        Datas.CoreInfo GetAllRawCoreInfo();

        void SetIndexQuiet(double index);

        void SetIndex(double index);


        bool GetterInfoFor(Func<string[], bool> filter);
        void ToggleIsAutoRun();
        void ToggleIsUntrack();
        void ToggleIsInjectImport();
        void ToggleIsInjectSkipCnSite();

        string GetUid();

        bool IsAutoRun();
        bool IsUntrack();
        bool IsSelected();
        bool IsInjectSkipCnSite();
        bool IsInjectImport();

        double GetIndex();

        string GetMark();
        string GetSummary();

        int GetFoldingLevel();
        void SetFoldingLevel(int level);

        long GetSpeedTestResult();
        string GetStatus();
        string GetName();
        string GetTitle();

        void SetIsSelected(bool selected);
        void SetCustomInbAddr(string ip, int port);
        void SetCustomInbType(int type);
        string GetCustomMark();
        void SetCustomMark(string mark);

        int GetCustomInbType();
        string GetCustomInbAddr();
        string GetInbIp();
        int GetInbPort();
        string GetRawUid();
    }
}
