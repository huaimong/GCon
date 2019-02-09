using VgcApis.Models.Interfaces;

namespace VgcApis.Models.BaseClasses
{
    public class ComponentOf<TContainer> :
        Disposable,
        IComponent<TContainer>

        where TContainer :
            class,
            IContainer<TContainer>
    {
        TContainer container = null;

        #region public methods
        public void Bind(TContainer container) =>
            this.container = container;

        public virtual void Prepare() { }
        #endregion

        #region protected methods
        protected TCompo GetComponent<TCompo>()
            where TCompo : class, IComponent<TContainer>
            => container.GetComponent<TCompo>();

        protected TContainer GetContainer() => container;
        #endregion
    }
}
