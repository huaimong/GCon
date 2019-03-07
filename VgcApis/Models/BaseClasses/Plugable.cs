using System;
using System.Collections.Generic;
using VgcApis.Models.Interfaces;

namespace VgcApis.Models.BaseClasses
{
    public class PlugableComponent<TContainer> :
        Disposable,
        IPlugable<TContainer>
        where TContainer : class
    {
        object container;
        List<object> components;

        public PlugableComponent()
        {
            components = new List<object>();
            container = null;
        }

        public virtual TContainer GetContainer() => container as TContainer;

        public virtual void Bind(TContainer container)
        {
            this.container = container;
        }

        public IReadOnlyCollection<object> GetAllComponents() =>
            components.AsReadOnly();

        public virtual void Prepare() { }

        public TComponent GetComponent<TComponent>()
            where TComponent : class
        {
            foreach (var component in components)
            {
                if (component is TComponent)
                {
                    return component as TComponent;
                }
            }
            return null;
        }

        public void Plug<TSelf, TComponent>(TSelf container, TComponent component)
            where TSelf : class, IPlugable<TContainer>
            where TComponent : class, IPlugable<TSelf>
        {
            foreach (var item in components)
            {
                if (item is TComponent)
                {
                    throw new ArgumentException(
                        $"Component type {component.GetType().FullName} already existed!");
                }
            }

            components.Add(component);
            component.Bind(container);
        }

        protected override void Cleanup()
        {
            foreach (var obj in components)
            {
                (obj as IDisposable).Dispose();
            }
            base.Cleanup();
        }
    }
}
