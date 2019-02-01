using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VgcApis.Models.IServices
{
    public interface IServersService
    {
        event EventHandler<Models.Datas.BoolEvent> OnServerStateChange;
        event EventHandler<Models.Datas.StrEvent> OnCoreClosing;

        string PackServersIntoV4Package(
            List<Models.IControllers.ICoreCtrl> servList,
            string orgServerUid,
            string packageName);

        ReadOnlyCollection<Models.IControllers.ICoreCtrl> GetTrackableServerList();
        ReadOnlyCollection<Models.IControllers.ICoreCtrl> GetAllServersList();
    }
}
