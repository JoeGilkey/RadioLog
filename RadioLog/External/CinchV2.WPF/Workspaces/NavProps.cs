using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Cinch
{
    /// <summary>
    /// Attached properties for managing WorkSpaces
    /// </summary>
    public static class NavProps
    {
        /// <summary>
        /// This DP is used to tell allow the workspace management to hide the hosting
        /// control when there are no more workspaces open. You can set this property
        /// true if you want this behaviour, false will keep the hosting control visible
        /// even when all the workspaces in it are closed
        /// </summary>
        #region ShouldHideHostWhenNoItems

        /// <summary>
        /// ShouldHideHostWhenNoItems Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty ShouldHideHostWhenNoItemsProperty =
            DependencyProperty.RegisterAttached("ShouldHideHostWhenNoItems", typeof(bool), typeof(NavProps),
                new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        /// Gets the ShouldHideHostWhenNoItems property.  
        /// </summary>
        public static bool GetShouldHideHostWhenNoItems(DependencyObject d)
        {
            return (bool)d.GetValue(ShouldHideHostWhenNoItemsProperty);
        }

        /// <summary>
        /// Sets the ShouldHideHostWhenNoItems property.  
        /// </summary>
        public static void SetShouldHideHostWhenNoItems(DependencyObject d, bool value)
        {
            d.SetValue(ShouldHideHostWhenNoItemsProperty, value);
        }

        #endregion

        /// <summary>
        /// This DP is used to create the actual workspace View based on the value of the
        /// bound WorkspaceData.ViewType
        /// </summary>
        #region ViewCreator

        /// <summary>
        /// ViewCreator Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty ViewCreatorProperty =
            DependencyProperty.RegisterAttached("ViewCreator", typeof(WorkspaceData), typeof(NavProps),
                new FrameworkPropertyMetadata((WorkspaceData)null,
                    new PropertyChangedCallback(OnViewCreatorChanged)));

        /// <summary>
        /// Gets the ViewCreator property.  
        /// </summary>
        public static WorkspaceData GetViewCreator(DependencyObject d)
        {
            return (WorkspaceData)d.GetValue(ViewCreatorProperty);
        }

        /// <summary>
        /// Sets the ViewCreator property.  
        /// </summary>
        public static void SetViewCreator(DependencyObject d, WorkspaceData value)
        {
            d.SetValue(ViewCreatorProperty, value);
        }

        /// <summary>
        /// Handles changes to the ViewCreator property.
        /// </summary>
        private static void OnViewCreatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {


            ItemsControl itemsControl = null;

            if (e.NewValue == null)
            {
                itemsControl = TreeHelper.TryFindParent<ItemsControl>(d);
                bool shouldHideHostWhenNoItems = (bool)itemsControl.GetValue(NavProps.ShouldHideHostWhenNoItemsProperty);
                if (shouldHideHostWhenNoItems)
                {
                    if (itemsControl != null)
                        itemsControl.Visibility = Visibility.Collapsed;
                }
                return;
            }

            Border contPresenter = (Border)d;
            WorkspaceData viewNavData = (WorkspaceData)e.NewValue;

            var theView = ViewResolver.CreateView(viewNavData.ViewLookupKey);
            IWorkSpaceAware dataAwareView = theView as IWorkSpaceAware;
            if (dataAwareView == null)
            {
                throw new InvalidOperationException(
                    "NavProps attached property is only designed to work with Views that implement the IWorkSpaceAware interface");
            }
            else
            {
                dataAwareView.WorkSpaceContextualData = viewNavData;
                contPresenter.Child = (UIElement)dataAwareView;
            }
            itemsControl = TreeHelper.TryFindParent<ItemsControl>(d);
            if (itemsControl != null)
                itemsControl.Visibility = Visibility.Visible;
        }

        #endregion
    }
}
