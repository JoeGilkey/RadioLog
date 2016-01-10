using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Cinch
{
    /// <summary>
    /// Class that manages the execution of background tasks, which allows
    /// a ViewModel to setup a background task using a Func&lt;T&gt; and use
    /// the completed results using a Action&lt;T&gt;. This class also
    /// allows Unit tests to be notified when the background tasks completes.
    ///
    /// Taken aand subsequently modified from 
    /// http://blogs.msdn.com/delay/archive/2009/04/08/nobody-likes-seeing-the-hourglass-keep-your-application-responsive-with-backgroundtaskmanager-on-wpf-and-silverlight.aspx
    /// 
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// 
    ///    //==================================================================
    ///    //
    ///    //  EXAMPLE :  Using the BackgroundTaskCompleted Event raised by
    ///    //              the BackgroundTaskManager
    ///    //
    ///    //==================================================================
    ///    //Have a property for the BackgroundTaskManager in the ViewModel
    ///    public BackgroundTaskManager<Int32,Int32> BgWorker
    ///    {
    ///        get { return bgWorker; }
    ///        set
    ///        {
    ///            bgWorker = value;
    ///            OnPropertyChanged();
    ///        }
    ///    }
    ///    
    ///    //Then wire it up
    ///    bgWorker = new BackgroundTaskManager<Int32,Int32>(
    ///        (argument) =>
    ///            {
    ///                Int32 innerCount = 0;
    ///                for (innerCount = 0; innerCount < argument; innerCount++)
    ///                {
    ///
    ///                }
    ///                return innerCount;
    ///            },
    ///        (result) =>
    ///            {
    ///                Count = result;
    ///            });
    ///     
    ///    //Some ViewModel method
    ///    public void Test()
    ///    {
    ///        bgWorker.WorkerArgument=15;
    ///        bgWorker.RunBackgroundTask();
    ///    }
    ///            
    ///   //Then use it in Unit test as follows
    ///   ManualResetEvent manualEvent = new ManualResetEvent(false);
    ///   vm.BgWorker.BackgroundTaskCompleted += delegate(object sender, EventArgs args)
    ///   {
    ///       // Signal the waiting NUnit thread that we're ready to move on.
    ///       manualEvent.Set();
    ///   };
    ///   vm.Test();
    ///   manualEvent.WaitOne(5000, false);
    ///   Assert.AreEqual(200000000, vm.Count);
    /// ]]>
    /// </example>
    public class BackgroundTaskManager<TArg, TResult>
    {

        #region Ctor
        /// <summary>
        /// Empty constructor
        /// </summary>
        public BackgroundTaskManager()
        {

        }

        /// <summary>
        /// Constructs a new BackgroundTaskManager with
        /// the function to run, and the action to call when the function to run
        /// completes
        /// </summary>
        /// <param name="taskFunc">The function to run in the background</param>
        /// <param name="completionAction">The completed action to call
        /// when the background function completes</param>
        public BackgroundTaskManager(Func<TArg, TResult> taskFunc, Action<TResult> completionAction)
        {
            this.TaskFunc = taskFunc;
            this.CompletionAction = completionAction;

        }
        #endregion

        #region Public Properties

        /// <summary>
        /// The worker work delegate
        /// </summary>
        public Func<TArg, TResult> TaskFunc { get; set; }

        /// <summary>
        /// The worker callback delegate when completed
        /// </summary>
        public Action<TResult> CompletionAction { get; set; }


        /// <summary>
        /// Event invoked when a background task is started.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible",
            Justification = "Add/remove is thread-safe for events in .NET.")]
        public EventHandler<EventArgs> BackgroundTaskStarted;

        /// <summary>
        /// Event invoked when a background task completes.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible",
            Justification = "Add/remove is thread-safe for events in .NET.")]
        public EventHandler<EventArgs> BackgroundTaskCompleted;

        /// <summary>
        /// Allows the Unit test to be notified on Task completion
        /// </summary>
        public AutoResetEvent CompletionWaitHandle { get; set; }

        /// <summary>
        /// Worker argumemnt
        /// </summary>
        public Object WorkerArgument { get; set; }

        #endregion
        
        #region Public Methods
        /// <summary>
        /// Runs a task function on a background thread; 
        /// invokes a completion action on the main thread.
        /// </summary>
        public void RunBackgroundTask()
        {
            // Create a BackgroundWorker instance
            var backgroundWorker = new BackgroundWorker();

            // Attach to its DoWork event to run the task function and capture the result
            backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                e.Result = TaskFunc((TArg)e.Argument);
            };


            // Attach to its RunWorkerCompleted event to run the completion action
            backgroundWorker.RunWorkerCompleted += 
                delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                // Call the completion action
                CompletionAction((TResult)e.Result);
                
                // Invoke the BackgroundTaskCompleted event
                var backgroundTaskFinishedHandler = BackgroundTaskCompleted;
                if (null != backgroundTaskFinishedHandler)
                {
                    backgroundTaskFinishedHandler.Invoke(null, EventArgs.Empty);
                }

            };

            // Invoke the BackgroundTaskStarted event
            var backgroundTaskStartedHandler = BackgroundTaskStarted;
            if (null != backgroundTaskStartedHandler)
            {
                backgroundTaskStartedHandler.Invoke(null, EventArgs.Empty);
            }

            // Run the BackgroundWorker asynchronously
            backgroundWorker.RunWorkerAsync(WorkerArgument);
        }
        #endregion
    }

}
