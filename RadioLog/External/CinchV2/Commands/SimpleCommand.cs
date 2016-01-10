using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Cinch
{

    /// <summary>
    /// Interface that is used for ICommands that notify when they are
    /// completed
    /// </summary>
    public interface ICompletionAwareCommand
    {
        /// <summary>
        /// Notifies that the command has completed
        /// </summary>
        event Action<Object> CommandCompleted;
    }
    

    /// <summary>
    /// Interface that defines if the object instance is active
    /// and notifies when the activity changes.
    /// </summary>
    public interface IActiveAware
    {
        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        /// <value><see langword="true" /> if the object is active; otherwise <see langword="false" />.</value>
        bool IsActive { get; set; }

        /// <summary>
        /// Notifies that the value for <see cref="IsActive"/> property has changed.
        /// </summary>
        event EventHandler IsActiveChanged;
    }


    /// <summary>
    /// Simple delegating command, based largely on DelegateCommand from PRISM/CAL
    /// </summary>
    /// <typeparam name="T">The type for the </typeparam>
    public class SimpleCommand<T1,T2> : ICommand, IActiveAware, ICompletionAwareCommand
    {
        private Func<T1, bool> canExecuteMethod;
        private Action<T2> executeMethod;
        private bool isActive=true;
        private event EventHandler isActiveChanged;
        private event Action<Object> commandCompleted;

        public SimpleCommand(Func<T1, bool> canExecuteMethod, Action<T2> executeMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public SimpleCommand(Action<T2> executeMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = (x) => { return true; };
        }
       
        public event EventHandler IsActiveChanged
        {
            add { isActiveChanged += value; }
            remove { isActiveChanged -= value; }
        }

        public event Action<Object> CommandCompleted
        {
            add { commandCompleted += value; }
            remove { commandCompleted -= value; }
        }


        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive == value) return;
                isActive = value;

                EventHandler handler = isActiveChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public bool CanExecute(T1 parameter)
        {
            if (canExecuteMethod == null) return true;
            return canExecuteMethod(parameter);
        }

        public void Execute(T2 parameter)
        {
            if (executeMethod != null)
            {
                executeMethod(parameter);
            }

            //now raise CommandCompleted for this ICommand
            Action<Object> completedHandler = commandCompleted;
            if (completedHandler != null)
            {
                completedHandler(parameter);
            }
        }

        public bool CanExecute(object parameter)
        {
            return CanExecute((T1)parameter) && IsActive;
        }

        public void Execute(object parameter)
        {
            Execute((T2)parameter);
        }

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
                if (canExecuteMethod != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (canExecuteMethod != null)
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