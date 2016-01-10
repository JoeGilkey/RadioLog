using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace MEFedMVVM.Common
{
    /// <summary>
    /// Base class that raises the property changed
    /// </summary>
    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            var propertyInfo = (property.Body as MemberExpression).Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyInfo.Name));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
