using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MEFedMVVM.Services.Contracts;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MEFedMVVM.ViewModelLocator;

namespace MEFedMVVM.Services.CommonServices
{
    /// <summary>
    /// The task dialog service allows you to use a TaskDialog in
    /// place of a MessageBox in your applications.
    /// </summary>
    /// <remarks>
    /// In order to use the TaskDialog, you have to make sure you are using the correct version of com.
    /// You might need to add a manifest file and add the following code after the &lt;/trustinfo&gt; tag:
    /// <code>
    /// <![CDATA[
    ///   <dependency>
    ///   <dependentAssembly>
    ///     <!-- Necessary for using the task dialog -->
    ///     <assemblyIdentity type="win32" name="Microsoft.Windows.Common-Controls" version="6.0.0.0" processorArchitecture="*" publicKeyToken="6595b64144ccf1df" language="*" />
    ///   </dependentAssembly>
    ///   </dependency>
    ///   ]]>
    /// </code>
    /// </remarks>
    [ExportService(ServiceType.Runtime, typeof(ITaskDialog))]
    public class TaskDialog : ITaskDialog
    {
        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
        static extern int externalTaskDialog(IntPtr hWndParent, IntPtr hInstance, String pszWindowTitle, String pszMainInstruction, String pszContent, int dwCommonButtons, IntPtr pszIcon, out int pnButton);


        private IntPtr _owner;

        /// <summary>
        /// Initialise a new instance of <see cref="TaskDialog"/>.
        /// </summary>
        public TaskDialog()
        {
            _owner = IntPtr.Zero;
        }

        /// <summary>
        /// Show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        public TaskDialogResult Show(string text)
        {
            return Show(text, null, null, TaskDialogButtons.OK);
        }

        /// <summary>
        /// Show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="instruction">The instructions to display.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        public TaskDialogResult Show(string text, string instruction)
        {
            return Show(text, instruction, null, TaskDialogButtons.OK, 0);
        }

        /// <summary>
        /// Show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="instruction">The instructions to display.</param>
        /// <param name="caption">The caption for the task dialog.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        public TaskDialogResult Show(string text, string instruction, string caption)
        {
            return Show(text, instruction, caption, TaskDialogButtons.OK, 0);
        }

        /// <summary>
        /// Show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="instruction">The instructions to display.</param>
        /// <param name="caption">The caption for the task dialog.</param>
        /// <param name="buttons">Any <see cref="TaskDialogButtons"/> to display.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        public TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons)
        {
            return Show(text, instruction, caption, buttons, 0);
        }

        /// <summary>
        /// Show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="instruction">The instructions to display.</param>
        /// <param name="caption">The caption for the task dialog.</param>
        /// <param name="buttons">Any <see cref="TaskDialogButtons"/> to display.</param>
        /// <param name="icon">The <see cref="TaskDialogIcon"/> to display.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        public TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            return ShowInternal(text, instruction, caption, buttons, icon);
        }

        #region Private methods
        /// <summary>
        /// Method to actually show the task dialog.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="instruction">The instructions to display.</param>
        /// <param name="caption">The caption for the task dialog.</param>
        /// <param name="buttons">Any <see cref="TaskDialogButtons"/> to display.</param>
        /// <param name="icon">The <see cref="TaskDialogIcon"/> to display.</param>
        /// <returns>The <see cref="TaskDialogResult"/> from the task dialog.</returns>
        private TaskDialogResult ShowInternal(string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            int result;

            if (externalTaskDialog(_owner, IntPtr.Zero, caption, instruction, text, (int)buttons, new IntPtr((int)icon), out result) != 0)
                throw new InvalidOperationException("ShowInternal");

            switch (result)
            {
                case 1: return TaskDialogResult.OK;
                case 2: return TaskDialogResult.Cancel;
                case 4: return TaskDialogResult.Retry;
                case 6: return TaskDialogResult.Yes;
                case 7: return TaskDialogResult.No;
                case 8: return TaskDialogResult.Close;
                default: return TaskDialogResult.None;
            }
        }

        #endregion
    }

}
