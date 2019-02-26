using System.Collections.Generic;
using System.Linq;
using VgcApis.Models.Interfaces;

namespace VgcApis.Models.BaseClasses
{
    public class ContainerOf<TContainer> :
        Disposable,
        IContainer<TContainer>
        where TContainer : class
    {
        List<IComponent<TContainer>> components =
            new List<IComponent<TContainer>>();

        public TComponent GetComponent<TComponent>()
            where TComponent : class, IComponent<TContainer>
        {
            lock (components)
            {
                return components
                    .FirstOrDefault(c => c is TComponent)
                    as TComponent;
            }
        }

        #region protected methods
        protected void InitComponents()
        {
            lock (components)
            {
                foreach (var component in components)
                {
                    component.Prepare();
                }
            }
        }

        protected bool Plug(
            TContainer container,
            IComponent<TContainer> component)
        {
            if (component == null)
            {
                return false;
            }

            lock (components)
            {
                if (components.Contains(component))
                {
                    return false;
                }

                component.Bind(container);
                components.Add(component);
                return true;
            }
        }

        protected int UnPlug(IComponent<TContainer> component)
        {
            if (component == null)
            {
                return 0;
            }

            lock (components)
            {
                return components.RemoveAll(c => c == component);
            }
        }

        protected override void Cleanup()
        {
            lock (components)
            {
                foreach (var comp in components)
                {
                    comp.Dispose();
                }
            }
        }
        #endregion 
    }
}
