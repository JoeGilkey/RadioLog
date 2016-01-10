using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;

namespace Cinch
{

    /// <summary>
    /// Provides a focus behaviour base class that attempts
    /// to focus elements by matching their bound property paths
    /// with a input propertyPath string 
    /// </summary>
    public abstract class FocusBehaviorBase : Behavior<FrameworkElement>
    {
        #region Protected Methods
        /// <summary>
        /// Attempts to force focus to the bound property with the same propertyPath
        /// as the propertyPath input
        /// </summary>
        /// <param name="elementBinding">Binding to evaluate</param>
        /// <param name="propertyPath">propertyPath to try and find finding for</param>
        /// <param name="isUsingDataWrappers">shoul be true if the property is bound to a 
        /// <c>Cinch.DataWrapper</c></param>
        protected virtual void ConductFocusOnElement(Binding elementBinding, 
            String propertyPath, bool isUsingDataWrappers)
        {
            if (elementBinding == null)
                return;

            if (isUsingDataWrappers)
            {
                if (!elementBinding.Path.Path.Contains(propertyPath))
                    return;
            }
            else
            {
                if (elementBinding.Path.Path != propertyPath)
                    return;
            }


            // Delay the call to allow the current batch
            // of processing to finish before we shift focus.
            AssociatedObject.Dispatcher.BeginInvoke((Action)delegate
            {

                if (!AssociatedObject.Focus())
                {
                    DependencyObject fs = FocusManager.GetFocusScope(AssociatedObject);
                    FocusManager.SetFocusedElement(fs, AssociatedObject);
                }
            },
            DispatcherPriority.Background);
        }
        #endregion
    }
}