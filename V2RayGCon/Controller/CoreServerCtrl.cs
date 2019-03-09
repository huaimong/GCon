using System;
using VgcApis.Models.Interfaces.CoreCtrlComponents;

namespace V2RayGCon.Controller
{
    public class CoreServerCtrl :
        VgcApis.Models.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Models.Interfaces.ICoreServCtrl
    {
        public event EventHandler
            OnPropertyChanged,
            OnRequireStatusBarUpdate,
            OnRequireMenuUpdate,
            OnRequireNotifierUpdate,
            OnCoreClosing,
            OnTrackCoreStop,
            OnTrackCoreStart;

        VgcApis.Models.Datas.CoreInfo coreInfo;

        CoreServerComponent.CoreStates states;
        CoreServerComponent.Logger logger;
        CoreServerComponent.Configer configer;
        CoreServerComponent.CoreCtrl coreCtrl;

        public CoreServerCtrl(
            VgcApis.Models.Datas.CoreInfo coreInfo)
        {
            this.coreInfo = coreInfo;
        }

        public void Run(
             Service.Cache cache,
             Service.Setting setting,
             Service.ConfigMgr configMgr,
             Service.Servers servers)
        {
            //external dependency injection
            coreCtrl = new CoreServerComponent.CoreCtrl(setting, servers, configMgr);
            states = new CoreServerComponent.CoreStates(servers, coreInfo);
            logger = new CoreServerComponent.Logger(setting);
            configer = new CoreServerComponent.Configer(
                setting, cache, configMgr, servers, coreInfo);

            Plug(this, coreCtrl);
            Plug(this, states);
            Plug(this, logger);
            Plug(this, configer);

            //inter-container dependency injection
            coreCtrl.Prepare();
            states.Prepare();
            logger.Prepare();
            configer.Prepare();


            //other initializiations
            coreCtrl.BindEvents();
        }


        #region event relay
        public void InvokeEventOnCoreClosing() =>
            OnCoreClosing?.Invoke(this, EventArgs.Empty);

        public void InvokeEventOnRequireStatusBarUpdate() =>
            InvokeEmptyEvent(OnRequireStatusBarUpdate);

        public void InvokeEventOnRequireNotifierUpdate() =>
           InvokeEmptyEvent(OnRequireNotifierUpdate);

        public void InvokeEventOnPropertyChange() =>
            InvokeEmptyEventIgnoreError(OnPropertyChanged);

        public void InvokeEventOnTrackCoreStop() =>
            OnTrackCoreStop?.Invoke(this, EventArgs.Empty);

        public void InvokeEventOnTrackCoreStart() =>
            OnTrackCoreStart?.Invoke(this, EventArgs.Empty);

        public void InvokeEventOnRequireMenuUpdate() =>
            InvokeEmptyEvent(OnRequireMenuUpdate);
        #endregion

        #region public method
        public ICoreStates GetCoreStates() => states;
        public ICoreCtrl GetCoreCtrl() => coreCtrl;
        public ILogger GetLogger() => logger;
        public IConfiger GetConfiger() => configer;
        #endregion

        #region private method
        void InvokeEmptyEvent(EventHandler evHandler)
        {
            evHandler?.Invoke(null, EventArgs.Empty);
        }

        void InvokeEmptyEventIgnoreError(EventHandler evHandler)
        {
            try
            {
                InvokeEmptyEvent(evHandler);
            }
            catch { }
        }
        #endregion

        #region protected methods
        protected override void BeforeComponentsDispose()
        {
            InvokeEventOnCoreClosing();
            coreCtrl?.StopCore();
            coreCtrl?.ReleaseEvents();
        }
        #endregion
    }
}
