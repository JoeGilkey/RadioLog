using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RadioLog.WPFCommon
{
    public delegate bool SuperSimpleCanExecuteDelegate();

    public class SuperSimpleCommand : ICommand
    {
        private SuperSimpleCanExecuteDelegate canExecuteMethod;
        private Action executeMethod;
        private bool isActive = true;
        private event EventHandler isActiveChanged;

        public SuperSimpleCommand(SuperSimpleCanExecuteDelegate canExecuteMethod, Action executeMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public SuperSimpleCommand(Action executeMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = () => { return true; };
        }

        public event EventHandler IsActiveChanged
        {
            add { isActiveChanged += value; }
            remove { isActiveChanged -= value; }
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

        public bool CanExecute()
        {
            if (canExecuteMethod == null) return true;
            return canExecuteMethod();
        }

        public void Execute()
        {
            if (executeMethod != null)
            {
                executeMethod();
            }
        }

        public bool CanExecute(object parameter)
        {
            return CanExecute() && IsActive;
        }

        public void Execute(object parameter)
        {
            Execute();
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
