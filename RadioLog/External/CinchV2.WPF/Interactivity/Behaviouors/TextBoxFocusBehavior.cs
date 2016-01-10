using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Data;
using System.Windows.Threading;


namespace Cinch
{

    /// <summary>
    /// A simple TextBox Focus behaviour that allows the ViewModel that this
    /// TextBox is bound to to set focus. The ViewModel attempts
    /// to focus elements by matching their bound property paths
    /// with a input propertyPath string 
    /// </summary>
    /// <remarks>
    /// Recommended usage:
    /// <code>
    /// IN YOUR VIEW HAVE SOMETHING LIKE THIS
    /// 
    /// 
    ///         xmlns:i="clr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity"
    ///         xmlns:CinchV2="clr-namespace:Cinch;assembly=Cinch.WPF"
    ///         
    ///         <TextBox Width="150" Text="{Binding FirstName}" Margin="10">
    ///                <i:Interaction.Behaviors>
    ///                    <CinchV2:TextBoxFocusBehavior />
    ///                </i:Interaction.Behaviors>
    ///         </TextBox>
    ///            
    /// 
    /// AND IN YOUR VIEWMODEL YOU WOULD HAVE SOMETHING LIKE THIS
    /// 
    ///         RaiseFocusEvent("FirstName");
    /// </code>
    /// </remarks>
    public class TextBoxFocusBehavior : FocusBehaviorBase
    {
        #region Protected Methods
        protected DependencyProperty GetSourceProperty()
        {
            //As this is a TextBox we use TextBox.TextProperty
            return TextBox.TextProperty;
        }
        #endregion

        #region Overrides
        protected override void OnAttached()
        {

            if (!(AssociatedObject is TextBoxBase))
                return;

            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.DataContext is ViewModelBase)
                ((ViewModelBase)AssociatedObject.DataContext).FocusRequested += TextBoxFocusBehavior_FocusRequested;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            if (AssociatedObject.DataContext is ViewModelBase)
                ((ViewModelBase)AssociatedObject.DataContext).FocusRequested -= TextBoxFocusBehavior_FocusRequested;
        }
        #endregion

        #region Private Methods
        private void TextBoxFocusBehavior_FocusRequested(String propertyPath)
        {
                Binding binding = BindingOperations.GetBinding(AssociatedObject, GetSourceProperty());
                base.ConductFocusOnElement(binding,propertyPath, IsUsingDataWrappers);
        }
        #endregion

        #region DPs

        #region IsUsingDataWrappers

        /// <summary>
        /// IsUsingDataWrappers Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsUsingDataWrappersProperty =
            DependencyProperty.Register("IsUsingDataWrappers", typeof(bool), typeof(TextBoxFocusBehavior),
                new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsUsingDataWrappers property.  
        /// </summary>
        public bool IsUsingDataWrappers
        {
            get { return (bool)GetValue(IsUsingDataWrappersProperty); }
            set { SetValue(IsUsingDataWrappersProperty, value); }
        }

        #endregion

        

        #endregion


    }

}