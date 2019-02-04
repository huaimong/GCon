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
            List<Models.Interfaces.ICoreServCtrl> servList,
            string orgServerUid,
            string packageName);

        ReadOnlyCollection<Models.Interfaces.ICoreServCtrl> GetTrackableServerList();
        ReadOnlyCollection<Models.Interfaces.ICoreServCtrl> GetAllServersList();
    }
}
