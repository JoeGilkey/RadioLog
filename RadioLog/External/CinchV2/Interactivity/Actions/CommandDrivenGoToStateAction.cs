using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interactivity;
using Microsoft.Expression.Interactivity.Core;

namespace Cinch
{
	/// <summary>
	/// A simple GoToStateAction based Action
    /// that can simply be used with a CompletedAwareCommandTrigger
    /// and can be used without any setup. This Action is called
    /// with the correct StateName by the CompletedAwareCommandTrigger.
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
    ///		            </CinchV2:CompletedAwareCommandTrigger>
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
	public class CommandDrivenGoToStateAction : GoToStateAction
    {
        #region Ctor
        public CommandDrivenGoToStateAction()
		{
            this.UseTransitions = true;
		}
        #endregion

        #region Overrides
        protected override void Invoke(object o)
		{
            try
            {
                base.StateName = (String)o;
                base.Invoke(base.StateName);
            }
            catch (Exception ex)
            {
            }
        }
        #endregion
    }
}