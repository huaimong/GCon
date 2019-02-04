using System;

namespace VgcApis.Models.Interfaces
{
    public interface IContainer<TContainer> :
        IDisposable
        where TContainer : class
    {
        TComponent GetComponent<TComponent>()
            where TComponent : class, IComponent<TContainer>;
    }
}
