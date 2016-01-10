using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;

namespace MEFedMVVM.Services.Contracts
{
    [Flags]
    public enum TaskDialogButtons
    {
        OK = 0x0001,
        Cancel = 0x0008,
        Yes = 0x0002,
        No = 0x0004,
        Retry = 0x0010,
        Close = 0x0020
    }

    public enum TaskDialogIcon
    {
        Question = 0,
        Warning = ushort.MaxValue,
        Stop = Warning - 1,
        Information = Warning - 2,
        SecurityShield = Warning - 3,
        SecurityShieldBlue = Warning - 4,
        SecurityWarning = Warning - 5,
        SecurityError = Warning - 6,
        SecuritySuccess = Warning - 7,
        SecurityShieldGray = Warning - 8
    }

    public enum TaskDialogResult
    {
        None,
        OK = 1,
        Cancel = 2,
        Yes = 4,
        No = 6,
        Retry = 7,
        Close = 8
    }

    /// <summary>
    /// The task dialog service allows you to use a TaskDialog in
    /// place of a MessageBox in your applications.
    /// </summary>
    public interface ITaskDialog
    {

        TaskDialogResult Show(string text);

        TaskDialogResult Show(string text, string instruction);

        TaskDialogResult Show(string text, string instruction, string caption);

        TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons);

        TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon);
    }
}
