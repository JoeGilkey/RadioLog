using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Cinch
{
    /// <summary>
    /// This is a WPF specific partial section of a ViewModelBase
    /// </summary>
    public abstract partial class ViewModelBase
    {
        #region Data

        /// <summary>
        /// Collection of workspaces that this ViewModel manages
        /// </summary>
        private ObservableCollection<WorkspaceData> views = new ObservableCollection<WorkspaceData>();
        private ICollectionView collectionView;

        #endregion

        #region Ctor
        public ViewModelBase()
        {

            //This is used for popup control only
            CloseActivePopUpCommand = new SimpleCommand<object, object>(x => true, x => ExecuteCloseActivePopupCommand(x));
            CloseWorkSpaceCommand = new SimpleCommand<object, object>(x => true, x => ExecuteCloseWorkSpaceCommand());
  
            Mediator.Instance.RegisterHandler<WorkspaceData>("RemoveWorkspaceItem", OnNotifyDataRecieved);
            collectionView = CollectionViewSource.GetDefaultView(this.Views);


        }
        #endregion

        #region Mediator Message Sinks

        [MediatorMessageSink("RemoveWorkspaceItem")]
        void OnNotifyDataRecieved(WorkspaceData workspaceToRemove)
        {
            if (this.Views.Contains(workspaceToRemove))
            {
                this.Views.Remove(workspaceToRemove);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event should be raised to activate the UI.  Any view tied to this
        /// ViewModel should register a handler on this event and close itself when
        /// this event is raised.  If the view is not bound to the lifetime of the
        /// ViewModel then this event can be ignored.
        /// </summary>
        public event EventHandler<EventArgs> ActivateRequest;

        /// <summary>
        /// This event should be raised to close the view.  Any view tied to this
        /// ViewModel should register a handler on this event and close itself when
        /// this event is raised.  If the view is not bound to the lifetime of the
        /// ViewModel then this event can be ignored.
        /// </summary>
        public event EventHandler<CloseRequestEventArgs> CloseRequest;


        public event Action<String> FocusRequested;

        #endregion

        #region Public Properties

        static PropertyChangedEventArgs viewsArgs =
            ObservableHelper.CreateArgs<ViewModelBase>(x => x.Views);

        public ObservableCollection<WorkspaceData> Views
        {
            get { return views; }
            set
            {
                views = value;
                NotifyPropertyChanged(viewsArgs);
            }
        }

        #endregion

        #region Public Methods

        public void SetActiveWorkspace(WorkspaceData viewnav)
        {
            if (collectionView != null)
                collectionView.MoveCurrentTo(viewnav);
        }


        /// <summary>
        /// Raises the Focus Requested event
        /// </summary>
        /// <param name="focusProperty"></param>
        public void RaiseFocusEvent(String focusProperty)
        {
            FocusRequested(focusProperty);
        }

        /// <summary>
        /// This raises the CloseRequest event to close the UI.
        /// </summary>
        public virtual void RaiseCloseRequest(bool? dialogResult)
        {
            EventHandler<CloseRequestEventArgs> handlers = CloseRequest;

            // Invoke the event handlers
            if (handlers != null)
            {
                try
                {
                    handlers(this, new CloseRequestEventArgs(dialogResult));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }


        /// <summary>
        /// This raises the ActivateRequest event to activate the UI.
        /// </summary>
        public virtual void RaiseActivateRequest()
        {
            EventHandler<EventArgs> handlers = ActivateRequest;

            // Invoke the event handlers
            if (handlers != null)
            {
                try
                {
                    handlers(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        #endregion

    }
}
