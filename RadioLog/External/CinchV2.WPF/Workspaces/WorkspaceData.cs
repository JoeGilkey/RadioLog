using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Cinch
{
    /// <summary>
    /// Workspace data class which can be used within a DataTemplate along
    /// with the NavProps DP to manage workspaces
    /// </summary>
    public class WorkspaceData : INotifyPropertyChanged, IDisposable
    {
        #region Data
        private string imagePath;
        private string viewLookupKey;
        private object dataValue;
        private string displayText;
        private SimpleCommand<Object, Object> closeWorkSpaceCommand;
        private Boolean isCloseable = true;
        #endregion

        #region Ctor
        public WorkspaceData(string imagePath,string viewLookupKey, object dataValue, string displayText, bool isCloseable)
        {

            Mediator.Instance.Register(this);
            this.ImagePath = imagePath;
            this.ViewLookupKey = viewLookupKey;
            this.DataValue = dataValue;
            this.DisplayText = displayText;
            this.IsCloseable = isCloseable;

            CloseWorkSpaceCommand = new SimpleCommand<object, object>(x => true, x => ExecuteCloseWorkSpaceCommand(x));
        }
        #endregion

        #region Command Implememtation
        /// <summary>
        /// Executes the CloseWorkSpace Command
        /// </summary>
        private void ExecuteCloseWorkSpaceCommand(object o)
        {
            Mediator.Instance.NotifyColleagues<WorkspaceData>("RemoveWorkspaceItem", this);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// CloseActivePopUpCommand : Close popup command
        /// </summary>
        public SimpleCommand<Object, Object> CloseWorkSpaceCommand { get; private set; }

        /// <summary>
        /// Is this workspace a closeable workspace
        /// </summary>
        static PropertyChangedEventArgs isCloseableArgs =
            ObservableHelper.CreateArgs<WorkspaceData>(x => x.IsCloseable);

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
        /// True if this workspace has an image
        /// </summary>
        public bool HasImage
        {
            get
            {
                return !string.IsNullOrEmpty(ImagePath);
            }
        }


        /// <summary>
        /// ImagePath
        /// </summary>
        static PropertyChangedEventArgs imagePathArgs =
            ObservableHelper.CreateArgs<WorkspaceData>(x => x.ImagePath);

        public string ImagePath
        {
            get { return imagePath; }
            set
            {
                imagePath = value;
                NotifyPropertyChanged(imagePathArgs);
            }
        }



        /// <summary>
        /// View key lookup name
        /// </summary>
        static PropertyChangedEventArgs viewLookupKeyArgs =
            ObservableHelper.CreateArgs<WorkspaceData>(x => x.ViewLookupKey);

        public string ViewLookupKey
        {
            get { return viewLookupKey; }
            set
            {
                viewLookupKey = value;
                NotifyPropertyChanged(viewLookupKeyArgs);
            }
        }


        /// <summary>
        /// Workspace context data
        /// </summary>
        static PropertyChangedEventArgs dataValueArgs =
            ObservableHelper.CreateArgs<WorkspaceData>(x => x.DataValue);

        public object DataValue
        {
            get { return dataValue; }
            set
            {
                dataValue = value;
                NotifyPropertyChanged(dataValueArgs);
            }
        }


        /// <summary>
        /// Workspace display text, is used for Headers controls such as TabControl
        /// </summary>
        static PropertyChangedEventArgs displayTextArgs =
            ObservableHelper.CreateArgs<WorkspaceData>(x => x.DisplayText);

        public string DisplayText
        {
            get { return displayText; }
            set
            {
                displayText = value;
                NotifyPropertyChanged(displayTextArgs);
            }
        }
        #endregion

        #region Overrides
        public override string ToString()
        {
            return String.Format("ViewLookupKey {0}, DataValue {1}, DisplayText {2}", ViewLookupKey, DataValue.ToString(), DisplayText);
        }
        #endregion

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
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            Mediator.Instance.Unregister(this);
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
        ~WorkspaceData()
        {

        }
#endif

        #endregion // IDisposable Members
    }
}
