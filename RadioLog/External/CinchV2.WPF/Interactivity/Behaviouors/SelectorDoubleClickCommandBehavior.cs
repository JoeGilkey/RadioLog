using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;

namespace Cinch
{
    /// <summary>
    /// A simple selector double click Behaviour that calls a bound ViewModel Command
    /// when the user double clicks the Selector
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
    ///         <ListView ItemsSource="{Binding People}" IsSynchronizedWithCurrentItem="True">
    ///                <i:Interaction.Behaviors>
    ///                    <CinchV2:SelectorDoubleClickCommandBehavior Command="{Binding SomeViewModelCommand}" />
    ///                </i:Interaction.Behaviors>
    ///         </TextBox>
    ///         
    /// AND IN YOUR VIEW MODEL YOU WOULD HAVE A ICOMMAND DECLARED SOMETHING LIKE THIS
    /// 
    /// 
    ///         public SimpleCommand<Object, EventToCommandArgs> SelectorDoubleClickCommand { get; private set; }
    ///         
    ///         SelectorDoubleClickCommand = new SimpleCommand<Object, EventToCommandArgs>(
    ///                    (parameter) => { return true; },
    ///                    ExecuteSelectorDoubleClickCommand);
    /// 
    /// 
    ///         private void ExecuteSelectorDoubleClickCommand(EventToCommandArgs args)
    ///         {
    ///             ICommand commandRan = args.CommandRan;
    ///             Object o = args.CommandParameter;
    ///             EventArgs ea = args.EventArgs;
    ///             var sender = args.Sender;
    ///         }
    /// </code>
    /// </remarks>
    public class SelectorDoubleClickCommandBehavior : Behavior<Selector>
    {
        #region Overrides
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDoubleClick += AssociatedObject_MouseDoubleClick;
            EnableDisableElement();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseDoubleClick -= AssociatedObject_MouseDoubleClick;
        }
        #endregion

        #region Private Methods
        private void AssociatedObject_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Get the ItemsControl and then get the item, and check there
            //is an actual item, as if we are using a ListView we may have clicked the
            //headers which are not items
            ItemsControl listView = sender as ItemsControl;
            DependencyObject originalSender = e.OriginalSource as DependencyObject;
            if (listView == null || originalSender == null) return;

            DependencyObject container =
                ItemsControl.ContainerFromElement
                (sender as ItemsControl, e.OriginalSource as DependencyObject);

            if (container == null ||
                container == DependencyProperty.UnsetValue) return;

            // found a container, now find the item.
            object activatedItem =
                listView.ItemContainerGenerator.ItemFromContainer(container);

            if (activatedItem != null)
            {
                Invoke(activatedItem, e);
            }
        }


        private static void OnCommandChanged(SelectorDoubleClickCommandBehavior thisBehaviour, 
            DependencyPropertyChangedEventArgs e)
        {
            if (thisBehaviour == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                ((ICommand)e.OldValue).CanExecuteChanged -= thisBehaviour.OnCommandCanExecuteChanged;
            }

            ICommand command = (ICommand)e.NewValue;

            if (command != null)
            {
                command.CanExecuteChanged += thisBehaviour.OnCommandCanExecuteChanged;
            }

            thisBehaviour.EnableDisableElement();
        }

        private bool IsAssociatedElementDisabled()
        {
            return AssociatedObject != null && !AssociatedObject.IsEnabled;
        }

        private void EnableDisableElement()
        {
            if (AssociatedObject == null || Command == null)
            {
                return;
            }

            AssociatedObject.IsEnabled = Command.CanExecute(this.CommandParameter);
        }

        private void OnCommandCanExecuteChanged(object sender, EventArgs e)
        {
            EnableDisableElement();
        }

        #endregion

        #region Protected Methods
        /// <param name="parameter">The EventArgs of the fired event.</param>
        protected void Invoke(object clickedItem, MouseButtonEventArgs parameter)
        {
            if (IsAssociatedElementDisabled())
            {
                return;
            }

            ICommand command = Command;
            if (command != null && command.CanExecute(CommandParameter))
            {
                command.Execute(new EventToCommandArgs(clickedItem, command,
                    CommandParameter, (EventArgs)parameter));
            }
        }
        #endregion

        #region DPs

        #region CommandParameter
        /// <summary>
        /// Identifies the <see cref="CommandParameter" /> dependency property
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter", typeof(object), typeof(SelectorDoubleClickCommandBehavior),
            new PropertyMetadata(null,
                (s, e) =>
                {
                    SelectorDoubleClickCommandBehavior sender = s as SelectorDoubleClickCommandBehavior;
                    if (sender == null)
                    {
                        return;
                    }

                    if (sender.AssociatedObject == null)
                    {
                        return;
                    }

                    sender.EnableDisableElement();
                }));


        /// <summary>
        /// Gets or sets an object that will be passed to the <see cref="Command" />
        /// attached to this trigger. This is a DependencyProperty.
        /// </summary>
        public object CommandParameter
        {
            get { return (Object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        #endregion

        #region Command
        /// <summary
        /// >
        /// Identifies the <see cref="Command" /> dependency property
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(SelectorDoubleClickCommandBehavior),
            new PropertyMetadata(null,
                (s, e) => OnCommandChanged(s as SelectorDoubleClickCommandBehavior, e)));


        /// <summary>
        /// Gets or sets the ICommand that this trigger is bound to. This
        /// is a DependencyProperty.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        #endregion

        #endregion

    }
}