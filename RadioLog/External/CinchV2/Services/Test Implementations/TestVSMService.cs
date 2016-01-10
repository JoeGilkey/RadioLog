using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.Windows.Interactivity;

namespace Cinch
{
    /// <summary>
    /// A test IVisualStateManager based service that can be used in a Unit
    /// test for WPF/SL
    /// </summary>
    public class TestVVSMService : IVSM
    {
        #region Public Properties
        public bool IsInitialized { get; set; }
        #endregion

        #region IVisualStateManager Members

        public void GoToState(string stateName)
        {
            //Nothing we can do in a Unit test, there should be no view attached
        }

        #endregion

        #region IViewAware Members

        public void InjectContext(object view)
        {
            //Nothing we can do in a Unit test, there should be no view attached
        }


        public string LastStateExecuted { get; private set; }

        #endregion
    }
}