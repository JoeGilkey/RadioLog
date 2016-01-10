using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace Cinch
{
    /// <summary>
    /// This is a common partial section of a ViewModelBase class that is used by WPF/SL
    /// </summary>
    public abstract partial class ViewModelBase  : INotifyPropertyChanged, IDisposable, IParentablePropertyExposer
    {
        #region Data
        private Boolean isCloseable = true;
        private String displayName = string.Empty;
        #endregion

        #region Public Properties

        /// <summary>
        /// CloseActivePopUpCommand : Close popup command
        /// </summary>
        public SimpleCommand<Object, Object> CloseActivePopUpCommand { get; private set; }

        /// <summary>
        /// CloseActiveWorkspace (CinchV1 compatibility only)
        /// </summary>
        public SimpleCommand<Object, Object> CloseWorkSpaceCommand { get; private set; }


        /// <summary>
        /// Is this ViewModel closeable (for compatibility with Cinch V1)
        /// </summary>
        static PropertyChangedEventArgs isCloseableArgs =
            ObservableHelper.CreateArgs<ViewModelBase>(x => x.IsCloseable);

        public Boolean IsCloseable
        {
            get { return isCloseable; }
            set
            {
                isCloseable = value;
                NotifyPropertyChanged(isCloseableArgs);
            }
        }


        /// <summary>
        /// DisplayName for ViewModel (for compatibility with Cinch V1)
        /// </summary>
        static PropertyChangedEventArgs displayNameArgs =
            ObservableHelper.CreateArgs<ViewModelBase>(x => x.DisplayName);

        public String DisplayName
        {
            get { return displayName; }
            set
            {
                displayName = value;
                NotifyPropertyChanged(displayNameArgs);
            }
        }


        #endregion

        #region Event(s)
        /// <summary>
        /// Raised when this workspace should be removed from the UI.
        /// </summary>
        public event EventHandler<EventArgs> CloseWorkSpace;
        #endregion 

        #region Private Methods

        /// <summary>
        /// Raises RaiseCloseRequest event, passing back correct DialogResult
        /// </summary>
        private void ExecuteCloseActivePopupCommand(Object param)
        {
            if (param is Boolean)
            {
                // Close the dialog using DialogResult requested
                RaiseCloseRequest((bool)param);
                return;
            }

            //param is not a bool so try and parse it to a Bool
            Boolean popupAction = true;
            Boolean result = Boolean.TryParse(param.ToString(), out popupAction);
            if (result)
            {
                // Close the dialog using DialogResult requested
                RaiseCloseRequest(popupAction);
            }
            else
            {
                // Close the dialog passing back true as default
                RaiseCloseRequest(true);
            }
        }

        /// <summary>
        /// Executes the CloseWorkSpace Command
        /// </summary>
        private void ExecuteCloseWorkSpaceCommand()
        {

            EventHandler<EventArgs> handlers = CloseWorkSpace;

            // Invoke the event handlers
            if (handlers != null)
            {
                try
                {
                    handlers(this, EventArgs.Empty);
                }
                catch
                {
                    String err = "Error firing CloseWorkSpace event";
#if debug
                    Debug.WriteLine(err);
#endif
                    Console.WriteLine(err);
                }
            }

        }



        #endregion

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
#if !SILVERLIGHT
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
#endif
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify using pre-made PropertyChangedEventArgs
        /// </summary>
        /// <param name="args"></param>
        protected void NotifyPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Notify using String property name
        /// </summary>
        protected void NotifyPropertyChanged(String propertyName)
        {
            this.VerifyPropertyName(propertyName);
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region IParentablePropertyExposer
        /// <summary>
        /// Returns the list of delegates that are currently subscribed for the
        /// <see cref="System.ComponentModel.INotifyPropertyChanged">INotifyPropertyChanged</see>
        /// PropertyChanged event
        /// </summary>
        public Delegate[] GetINPCSubscribers()
        {
            return PropertyChanged == null ? null : PropertyChanged.GetInvocationList();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
#if !SILVERLIGHT
            Mediator.Instance.UnregisterHandler<WorkspaceData>("RemoveWorkspaceItem", OnNotifyDataRecieved);
#endif
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~ViewModelBase()
        {
            string msg = string.Format("{0} ({1}) Finalized",
                this.GetType().Name, this.GetHashCode());
            System.Diagnostics.Debug.WriteLine(msg);
        }
#endif

        #endregion // IDisposable Members

    }
}
