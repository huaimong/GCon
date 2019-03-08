using System;
using System.Collections.Generic;
using VgcApis.Models.Interfaces;

namespace VgcApis.Models.BaseClasses
{
    public class ComponentOf<TContainer> :
        IComponentOf<TContainer>
        where TContainer : class
    {
        TContainer container;
        List<IDisposable> components;

        public ComponentOf()
        {
            components = new List<IDisposable>();
            container = null;
        }

        #region public methods
        public virtual void Prepare() { }

        public virtual TContainer GetContainer() => container;

        public IReadOnlyCollection<object> GetAllComponents() =>
            components.AsReadOnly();

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

        public virtual void Bind(TContainer container)
        {
            this.container = container;
        }

        public void Plug<TSelf, TComponent>(TSelf container, TComponent component)
            where TSelf : class, IComponentOf<TContainer>
            where TComponent : class, IComponentOf<TSelf>
        {
            /*
             * proxy func:
             *   void Plug<TComponent>(TComponent component)=>
             *      Plug(this,component);
             *      
             * DO NOT:
             *   void Plug(InterfaceType component) =>
             *      Plug(this,component);
             *      
             * Because component maybe can not convert to "InterfaceType".
             * If that happens, this function will throw an exception.
             */

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

        #endregion

        #region protected methods
        protected virtual void BeforeCleanup() { }
        protected virtual void Cleanup() { }
        #endregion

        #region private methods
        private void DisposeAllComponents()
        {
            foreach (var obj in components)
            {
                (obj as IDisposable).Dispose();
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    BeforeCleanup();
                    DisposeAllComponents();
                    Cleanup();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~Disposable() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
