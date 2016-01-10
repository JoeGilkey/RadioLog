using System.Windows;
using System;
using System.Diagnostics;


namespace MEFedMVVM.ViewModelLocator
{
    /// <summary>
    /// Locator for ViewModels.
    /// This defines an attached property to import a ViewModel and it will inject all the required services to the ViewModel
    /// </summary>
    public class ViewModelLocator
    {
        #region ViewModel Attached property

        /// <summary>
        /// ViewModel Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.RegisterAttached("ViewModel", typeof(string), typeof(ViewModelLocator),
                new PropertyMetadata((string)String.Empty,
                    new PropertyChangedCallback(OnViewModelChanged)));

        /// <summary>
        /// Gets the ViewModel property.  This dependency property 
        /// indicates ....
        /// </summary>
        public static string GetViewModel(DependencyObject d)
        {
            return (string)d.GetValue(ViewModelProperty);
        }

        /// <summary>
        /// Sets the ViewModel property.  This dependency property 
        /// indicates ....
        /// </summary>
        public static void SetViewModel(DependencyObject d, string value)
        {
            d.SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Handles changes to the ViewModel property.
        /// </summary>
        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                string vmContractName = (string)e.NewValue;
                var element = d as FrameworkElement;

                if (!String.IsNullOrEmpty(vmContractName) && element != null)
                {
                    ViewModelRepository.AttachViewModelToView(vmContractName, element);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while resolving ViewModel. " + ex);
            }
        }

        #endregion

        #region Exception Strings
        const string CannotFindViewModel = "Cannot get ViewModel. Please check that you applied the ExportViewModel attribute and that the ViewModel inherits from BaseViewModel";
        #endregion
    }
}
