using System.Windows.Interactivity;
using System.Windows;
using Microsoft.Expression.Interactivity.Core;


namespace Cinch
{
    /// <summary>
    /// Is a Trigger that invokes does 2 things depending on the value of
    /// the IsBeingUsedAtRootLevel DP
    /// 
    /// 1. If IsBeingUsedAtRootLevel DP is true (meaning we are at root level of VisualTree
    ///    so should expect to find VisualStateManager groups at root level), this trigger will
    ///    invoked its action (Which is really expected to be a 
    ///    <c>CommandDrivenGoToStateAction</c>, when a to a bound ViewModels
    ///    SimpleCommand.CommandCompleted event occurs
    /// 
    /// 2. If IsBeingUsedAtRootLevel DP is false (meaning we are NOT at root level of VisualTree
    ///    so should expect to find VisualStateManager groups at current Framework element level, 
    ///    so must use ExtendedVisualStateManager), this trigger will
    ///    goto the state specified by the bound ViewModel SimpleCommand parameter, when the
    ///    SimpleCommand.CommandCompleted event occurs, As follows: 
    ///     ExtendedVisualStateManager.GoToElementState(this.AssociatedObject, (string)parameter, true);
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
    ///         <TabControl x:Name="tabControl">
    ///	            <i:Interaction.Triggers>
    ///		         <CinchV2:CompletedAwareGoToStateCommandTrigger 
    ///		                IsBeingUsedAtRootLevel="True"
    ///                     Command="{Binding GoToStateCommand}">
    ///			            <CinchV2:CommandDrivenGoToStateAction TargetObject="{Binding ElementName=tabControl}"/>
    ///		            </CinchV2:CompletedAwareGoToStateCommandTrigger>
    ///	         </i:Interaction.Triggers>
    ///	         <VisualStateManager.VisualStateGroups>
    ///		            <VisualStateGroup x:Name="VisualStateGroup">
    ///			            <VisualState x:Name="GreenState">
    ///			
    ///			            </VisualState>
    ///			            <VisualState x:Name="BlueState">
    ///			
    ///			            </VisualState>
    ///		            </VisualStateGroup>
    /// 	        </VisualStateManager.VisualStateGroups>
    /// 	        
    ///            .....rest of code
    ///            .....rest of code
    ///            .....rest of code
    /// 	     </TabControl>
    ///
    /// 
    /// AND WHERE THE VIEWMODEL LOOKS SOMETHING LIKE THIS
    /// 
    ///         public SimpleCommand<String, String> GoToStateCommand { get; private set; }
    ///         
    ///         GoToStateCommand = new SimpleCommand<String, String>(
    ///             (parameter) => { return !string.IsNullOrEmpty(parameter); },
    ///                 (input)=> {});
    /// 
    /// WHICH ALLOWS YOUR TO GO TO A NEW VISUAL STATE AS EASILY AS THIS IN YOUR VIEWMODEL
    /// 
    ///     GoToStateCommand.Execute("BlueState");
    /// </code>
    /// </remarks>
    public class CompletedAwareGoToStateCommandTrigger : TriggerBase<FrameworkElement>
    {
        #region DPs

        #region Command
        public static readonly DependencyProperty CommandProperty = 
            DependencyProperty.Register("Command", typeof(ICompletionAwareCommand), 
                typeof(CompletedAwareGoToStateCommandTrigger), null);

        /// <summary>
        /// Gets or sets the Command property. 
        /// </summary>
        public ICompletionAwareCommand Command
        {
            get { return (ICompletionAwareCommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        #endregion

        #region IsBeingUsedAtRootLevel
#if !SILVERLIGHT
        public static readonly DependencyProperty IsBeingUsedAtRootLevelProperty =
            DependencyProperty.Register("IsBeingUsedAtRootLevel", typeof(bool),
                typeof(CompletedAwareGoToStateCommandTrigger), new UIPropertyMetadata(false));
#else
        public static readonly DependencyProperty IsBeingUsedAtRootLevelProperty =
            DependencyProperty.Register("IsBeingUsedAtRootLevel", typeof(bool),
                typeof(CompletedAwareGoToStateCommandTrigger), new PropertyMetadata(false));
#endif

        /// <summary>
        /// Gets or sets the IsBeingUsedAtRootLevel property. 
        /// </summary>
        public bool IsBeingUsedAtRootLevel
        {
            get { return (bool)GetValue(IsBeingUsedAtRootLevelProperty); }
            set { SetValue(IsBeingUsedAtRootLevelProperty, value); }
        }
        #endregion
        #endregion

        #region Overrides
        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>
        /// Override this to hook up functionality to the AssociatedObject.
        /// </remarks>
        protected override void OnAttached()
        {
            base.OnAttached();

#if !SILVERLIGHT
            if (!AssociatedObject.IsLoaded)
            {
                this.Command.CommandCompleted  += Command_Completed;
            }
#else
            this.Command.CommandCompleted += Command_Completed;
#endif

        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.Command.CommandCompleted -= Command_Completed;
        }
        #endregion

        #region Private Methods
        private void Command_Completed(object parameter)
        {

            if (IsBeingUsedAtRootLevel)
            {
                // Invoke the actions
                InvokeActions(parameter);
            }
            else
            {
                if (VisualStateManager.GetVisualStateGroups(this.AssociatedObject).Count > 0)
                {
                    ExtendedVisualStateManager.GoToElementState(this.AssociatedObject, (string)parameter, true);
                }
            }
        }
        #endregion
    }
}
