using System;

namespace VgcApis.Models.Interfaces
{
    public interface IComponent<TContainer> : IDisposable
        where TContainer : class
    {
        void Bind(TContainer container);
        void Prepare();
    }
}
