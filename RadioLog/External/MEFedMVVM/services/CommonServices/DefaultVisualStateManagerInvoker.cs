using System;
using MEFedMVVM.ViewModelLocator;
using MEFedMVVM.Services.Contracts;
using Microsoft.Expression.Interactivity.Core;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.ComponentModel.Composition;

namespace MEFedMVVM.Services.CommonServices
{
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [ExportService(ServiceType.Both, typeof(IVisualStateManager))]
    public class DefaultVisualStateManagerInvoker : GoToStateAction, IVisualStateManager
    {
        #region IVisualStateManager Members

        public void GoToState(string stateName)
        {
            LastStateExecuted = stateName;

            if (!IsInitialized)
            {
                Debug.WriteLine("Could not attach to the Visual State Manager. Make sure you have the correct visual states in your XAML. This can also be the case where the view is not loaded yet. In that case ignore this message since the State will be applied as soon as the View is loaded");
            }
            else
            {
                InvokeState(stateName);
            }
        }

        #endregion

        public bool IsInitialized { get; set; }

        #region IContextAware Members
        public void InjectContext(object context)
        {
            var element = context as FrameworkElement;
            if (element != null)
            {
                RoutedEventHandler handler = null;
                handler = (sender, e) =>
                    {
                        var root = (FrameworkElement)sender;
                        TryAttach(root);
                        element.Loaded -= handler;
                    };
                element.Loaded += handler;
                TryAttach(element);
            }
        }
        private void TryAttach(FrameworkElement root)
        {
            if (VisualStateManager.GetVisualStateGroups(root).Count > 0) // check if the Visual States are defined in the root element
                AttachAndExecuteLastState(root);
            else
            {
                // if not then check if they are in the content (This is what Blend does, the Visual States are defined in the first element)
                var contentControlRoot = root as UserControl;
                if (contentControlRoot != null)
                {
                    var child = (FrameworkElement)contentControlRoot.Content;
                    if (child != null)
                    {
                        if (VisualStateManager.GetVisualStateGroups(child).Count > 0)
                            AttachAndExecuteLastState(child);
                    }
                }
            }
        }

        private void AttachAndExecuteLastState(FrameworkElement root)
        {
            Detach();
            Attach(root);
            IsInitialized = true;
            if (!string.IsNullOrEmpty(LastStateExecuted))
                InvokeState(LastStateExecuted);
        }

        private void InvokeState(string stateName)
        {
            try
            {
                UseTransitions = true;
                StateName = stateName;
                if (AssociatedObject == null)
                    Debug.WriteLine("Cannot invoke state since AssociatedObject is null");
                Invoke(null);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not invoke State " + stateName + " exception " + e);
            }
        }


        public string LastStateExecuted { get; private set; }

        #endregion
    }
}
