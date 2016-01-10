using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Cinch
{

    /// <summary>
    /// A simple Numeric text based Behavior which can be applied to TxtBox
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
    ///         <TextBox Width="150" Text="{Binding Age}" Margin="179.5,10">
    ///             <i:Interaction.Behaviors>
    ///                 <CinchV2:NumericTextBoxBehaviour/>
    ///             </i:Interaction.Behaviors>
    ///         </TextBox>
    /// </code>
    /// </remarks>
  public class NumericTextBoxBehaviour : Behavior<TextBox>
  {
      #region Overrides
      protected override void OnAttached()
      {
          base.OnAttached();
          AssociatedObject.PreviewTextInput += AssociatedObject_PreviewTextInput;
          DataObject.AddPastingHandler(AssociatedObject, OnClipboardPaste);
      }

      protected override void OnDetaching()
      {
          AssociatedObject.PreviewTextInput -= AssociatedObject_PreviewTextInput;
          DataObject.RemovePastingHandler(AssociatedObject, OnClipboardPaste);
      }
      #endregion

      #region Private Methods
      private void AssociatedObject_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
          TextBox tb = sender as TextBox;

          if (tb != null && !Validate(tb, e.Text))
              e.Handled = true;
      }


#if !SILVERLIGHT
      /// <summary>
      /// This method handles paste and drag/drop events onto the TextBox.  It restricts the character
      /// set to numerics and ensures we have consistent behavior. 
      /// This is only available in WPF
      /// </summary>
      /// <param name="sender">TextBox sender</param>
      /// <param name="e">EventArgs</param>
      private static void OnClipboardPaste(object sender, DataObjectPastingEventArgs e)
      {
          TextBox tb = sender as TextBox;
          string text = e.SourceDataObject.GetData(e.FormatToApply) as string;

          if (tb != null && !string.IsNullOrEmpty(text) && !Validate(tb, text))
              e.CancelCommand();
      }
#endif

      private static bool Validate(TextBox tb, string newContent)
      {
          string testString = string.Empty;
          // replace selection with new text.
          if (!string.IsNullOrEmpty(tb.SelectedText))
          {
              string pre = tb.Text.Substring(0, tb.SelectionStart);
              string after = tb.Text.Substring(tb.SelectionStart + tb.SelectionLength, 
                  tb.Text.Length - (tb.SelectionStart + tb.SelectionLength));
              testString = pre + newContent + after;
          }
          else
          {
              string pre = tb.Text.Substring(0, tb.CaretIndex);
              string after = tb.Text.Substring(tb.CaretIndex, tb.Text.Length - tb.CaretIndex);
              testString = pre + newContent + after;
          }

          Regex regExpr = new Regex(@"^([-+]?)(\d*)([,.]?)(\d*)$");
          if (regExpr.IsMatch(testString))
              return true;

          return false;
      }
      #endregion
  }
}