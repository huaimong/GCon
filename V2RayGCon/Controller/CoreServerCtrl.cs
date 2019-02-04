using System;
using System.Threading.Tasks;
using VgcApis.Models.Interfaces.CoreCtrlComponents;

namespace V2RayGCon.Controller
{
    public class CoreServerCtrl :
        VgcApis.Models.BaseClasses.ContainerTpl<CoreServerCtrl>,
        VgcApis.Models.Interfaces.ICoreServCtrl
    {


        /// <summary>
        /// false: stop true: start
        /// </summary>
        public event EventHandler<VgcApis.Models.Datas.BoolEvent>
            OnRequireKeepTrack;

        public event EventHandler
            OnPropertyChanged,
            OnRequireStatusBarUpdate,
            OnRequireMenuUpdate,
            OnRequireNotifierUpdate,
            OnCoreClosing;

        VgcApis.Models.Datas.CoreInfo coreInfo;

        CoreServerComponent.States states;
        CoreServerComponent.Logger logger;
        CoreServerComponent.Config configer;
        CoreServerComponent.Core coreCtrl;

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
            coreCtrl = new CoreServerComponent.Core(setting);
            states = new CoreServerComponent.States(servers, coreInfo);
            logger = new CoreServerComponent.Logger(setting);
            configer = new CoreServerComponent.Config(
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

        public void InvokeEventOnRequireKeepTrack(bool isServerStart) =>
            OnRequireKeepTrack(
                this,
                new VgcApis.Models.Datas.BoolEvent(isServerStart));

        public void InvokeEventOnRequireMenuUpdate() =>
            InvokeEmptyEvent(OnRequireMenuUpdate);
        #endregion

        #region public method
        public IStates GetStates() => states;
        public ICore GetCoreCtrl() => coreCtrl;
        public ILogger GetLogger() => logger;
        public IConfig GetConfiger() => configer;

        public void CleanupThen(Action next)
        {
            InvokeEventOnCoreClosing();
            coreCtrl.StopCoreThen(() =>
            {
                coreCtrl.ReleaseEvents();
                Task.Run(() =>
                {
                    next?.Invoke();
                });
            });
        }
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

    }
}
