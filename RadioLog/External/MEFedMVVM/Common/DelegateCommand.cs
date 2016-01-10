using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Diagnostics.CodeAnalysis;

namespace MEFedMVVM.Common
{
    /// <summary>
    /// The DelegateCommand implements the <see cref="ICommand"/> interface where delegates can be attached for the
    /// <see cref="Execute"/> and <see cref="CanExecute"/> methods.
    /// </summary>
    /// <typeparam name="T">The Command parameter type.</typeparam>
    public class DelegateCommand<T> : ICommand
    {
        private Action<T> _executeMethod;
        private Func<T, bool> _canExecuteMethod;
        private List<WeakReference> _canExecuteChangedHandlers;

        /// <summary>
        /// Initialize a new instance of <see cref="DelegateCommand{T}"/>.
        /// </summary>
        /// <param name="executeMethod">The delegate that is executed when <see cref="Execute"/> is called on the command.</param>
        /// <remarks><see cref="CanExecute"/> always returns true.</remarks>
        public DelegateCommand(Action<T> executeMethod) : this(executeMethod, (T meth) => { return true; }) { }

        /// <summary>
        /// Initialize a new instance of <see cref="DelegateCommand{T}"/>.
        /// </summary>
        /// <param name="executeMethod">The delegate that is executed when <see cref="Execute"/> is called on the command.</param>
        /// <param name="canExecuteMethod">The delegate to be called when <see cref="CanExecute"/> is called on the command.</param>
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        #region CanExecute
        /// <summary>
        /// Method executed to determine whether or not the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Information used by the command.</param>
        public bool CanExecute()
        {
            return CanExecute(null);
        }

        /// <summary>
        /// Method executed to determine whether or not the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Information used by the command.</param>
        /// <returns>Returns true if this command can be executed, false otherwise.</returns>
        public bool CanExecute(T parameter)
        {
            if (_canExecuteMethod == null) return true;
            return _canExecuteMethod(parameter);
        }

        /// <summary>
        /// Method executed to determine whether or not the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Information used by the command.</param>
        /// <returns>Returns true if this command can be executed, false otherwise.</returns>
        public bool CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }
        #endregion

        #region Execute
        /// <summary>
        /// The method to be executed when the command is invoked.
        /// </summary>
        public void Execute()
        {
            Execute(null);
        }

        /// <summary>
        /// The method to be executed when the command is invoked.
        /// </summary>
        /// <param name="parameter">Information used by the command.</param>
        public void Execute(T parameter)
        {
            if (_executeMethod != null)
            {
                _executeMethod(parameter);
            }
        }

        /// <summary>
        /// The method to be executed when the command is invoked.
        /// </summary>
        /// <param name="parameter">Information used by the command.</param>
        public void Execute(object parameter)
        {
            Execute((T)parameter);
        }
        #endregion

#if SILVERLIGHT
        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;
#else
        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecuteMethod != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (_canExecuteMethod != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }
#endif

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged" /> event.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic",
            Justification = "The this keyword is used in the Silverlight version")]
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate",
            Justification = "This cannot be an event")]
        public void RaiseCanExecuteChanged()
        {
#if SILVERLIGHT
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
#else
            CommandManager.InvalidateRequerySuggested();
#endif
        }
    }
}
