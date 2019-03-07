using System;
using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    // has container
    public interface IPlugable<TContainer> :
        IDisposable
        where TContainer : class
    {

        TContainer GetContainer();

        void Bind(TContainer container);

        IReadOnlyCollection<object> GetAllComponents();

        void Prepare();

        TComponent GetComponent<TComponent>()
            where TComponent : class;


        void Plug<TSelf, TComponent>(TSelf container, TComponent component)
            where TSelf : class, IPlugable<TContainer>
            where TComponent : class, IPlugable<TSelf>;

    }


}
