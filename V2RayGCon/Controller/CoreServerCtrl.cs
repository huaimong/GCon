using System;
using VgcApis.Models.Interfaces.CoreCtrlComponents;

namespace V2RayGCon.Controller
{
    public class CoreServerCtrl :
        VgcApis.Models.BaseClasses.ContainerTpl<CoreServerCtrl>,
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
             Service.Servers servers)
        {
            //external dependency injection
            coreCtrl = new CoreServerComponent.CoreCtrl(setting, servers);
            states = new CoreServerComponent.CoreStates(servers, coreInfo);
            logger = new CoreServerComponent.Logger(setting);
            configer = new CoreServerComponent.Configer(
                setting, cache, servers, coreInfo);

            Plug(coreCtrl);
            Plug(states);
            Plug(logger);
            Plug(configer);

            //inter-container dependency injection
            InitComponents();

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
        void Plug(VgcApis.Models.Interfaces.IComponent<CoreServerCtrl> component)
            => Plug(this, component);

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
        protected override void Cleanup()
        {
            InvokeEventOnCoreClosing();
            coreCtrl.StopCore();
            coreCtrl.ReleaseEvents();
            base.Cleanup();
        }
        #endregion
    }
}
