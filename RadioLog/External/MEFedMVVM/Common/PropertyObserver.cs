using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace MEFedMVVM.Common
{
    public static class ObservablePropertyChanged
    {
        /// <summary>
        /// returns a PropertyChangedSubscriber so taht you can hook to PropertyChanged
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="source">The source that is the INotifyPropertyChanged</param>
        /// <param name="property">THe property to attach to</param>
        /// <returns>Returns the subscriber</returns>
        public static PropertyChangedSubscriber<TSource, TProperty>
            OnChanged<TSource, TProperty>(this TSource source, Expression<Func<TSource, TProperty>> property)
            where TSource : class, INotifyPropertyChanged
        {
            return new PropertyChangedSubscriber<TSource, TProperty>(source, property);
        }
    }

    /// <summary>
    /// Shortcut to subscribe to PropertyChanged on an INotfiyPropertyChanged and executes an action when that happens
    /// </summary>
    /// <typeparam name="TSource">Must implement INotifyPropertyChanged</typeparam>
    /// <typeparam name="TProperty">Can be any type</typeparam>
    public class PropertyChangedSubscriber<TSource, TProperty>
        : IDisposable where TSource : class, INotifyPropertyChanged
    {
        private readonly Expression<Func<TSource, TProperty>> _propertyValidation;
        private readonly TSource _source;
        private Action<TSource> _onChange;

        public PropertyChangedSubscriber(TSource source, Expression<Func<TSource, TProperty>> property)
        {
            _propertyValidation = property;
            _source = source;
            source.PropertyChanged += SourcePropertyChanged;
        }

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsPropertyValid(e.PropertyName))
            {
                _onChange(sender as TSource);
            }
        }

        /// <summary>
        /// Executes the action and returns an IDisposable so that you can unregister 
        /// </summary>
        /// <param name="onChanged">The action to execute</param>
        /// <returns>The IDisposable so that you can unregister</returns>
        public IDisposable Do(Action<TSource> onChanged)
        {
            _onChange = onChanged;
            return this;
        }

        /// <summary>
        /// Executes the action only once and automatically unregisters
        /// </summary>
        /// <param name="onChanged">The action to be executed</param>
        public void DoOnce(Action<TSource> onChanged)
        {
            Action<TSource> dispose = x => Dispose();
            _onChange = (Action<TSource>)Delegate.Combine(onChanged, dispose);
        }

        private bool IsPropertyValid(string propertyName)
        {
            var propertyInfo = ((MemberExpression)_propertyValidation.Body).Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            return propertyInfo.Name == propertyName;
        }

        #region Implementation of IDisposable

        /// <summary>
        ///   Unregisters the property
        /// </summary>
        public void Dispose()
        {
            _source.PropertyChanged -= SourcePropertyChanged;
        }

        #endregion
    }
}
