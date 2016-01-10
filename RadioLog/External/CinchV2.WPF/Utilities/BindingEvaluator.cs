using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Cinch
{

    /// <summary>
    /// Use this evaluator when you do not know the return type expected.
    /// The return type will always be Object, and you will have to deal with
    /// that outside of this class
    /// </summary>
    /// <remarks>
    /// Recommended usage:
    /// <code>
    ///     TextBox positionedTextBox = new TextBox();
    ///     Binding positionBinding = new Binding("Minute");
    ///     positionBinding.Source = System.DateTime.Now;
    ///     positionedTextBox.SetBinding(Canvas.TopProperty, positionBinding);
    ///     
    ///     //Use GenericBindingEvaluator to get Bound Value
    ///     BindingEvaluator be = new BindingEvaluator();
    ///     Object x = be.GetBoundValue(positionBinding);
    ///
    /// </code>
    /// </remarks>
    public class BindingEvaluator : DependencyObject
    {
        #region DPs
        /// <summary>
        /// Dummy internal DP, to bind and get value from
        /// </summary>
        public static readonly DependencyProperty DummyProperty = DependencyProperty.Register(
            "Dummy", typeof(Object), typeof(DependencyObject), new UIPropertyMetadata(null));

        public Object Dummy
        {
            get { return (Object)GetValue(DummyProperty); }
            set { SetValue(DummyProperty, value); }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Evaluate the binding
        /// </summary>
        /// <param name="bindingToEvaluate">The BindingBase to get the value of</param>
        /// <returns>The result of the BindingBase</returns>
        public Object GetBoundValue(BindingBase bindingToEvaluate)
        {
            BindingOperations.SetBinding(this, BindingEvaluator.DummyProperty, bindingToEvaluate);
            return this.Dummy;
        }
        #endregion
    }


    /// <summary>
    /// Use this evaluator when you know the return type expected
    /// </summary>
    /// <typeparam name="T">The return type expected from the Binding</typeparam>
    /// <remarks>
    /// Recommended usage:
    /// <code>
    ///     TextBox positionedTextBox = new TextBox();
    ///     Binding positionBinding = new Binding("Minute");
    ///     positionBinding.Source = System.DateTime.Now;
    ///     positionedTextBox.SetBinding(Canvas.TopProperty, positionBinding);
    ///     
    ///     //Use GenericBindingEvaluator to get Bound Value
    ///     GenericBindingEvaluator<Int32> be = new GenericBindingEvaluator<Int32>();
    ///     Int32 x = be.GetBoundValue(positionBinding);
    ///
    /// </code>
    /// </remarks>
    public class GenericBindingEvaluator<T> : DependencyObject
    {
        #region DPs
        /// <summary>
        /// Dummy internal DP, to bind and get value from
        /// </summary>
        public static readonly DependencyProperty DummyProperty = DependencyProperty.Register(
            "Dummy", typeof(T), typeof(DependencyObject), new UIPropertyMetadata(null));

        public T Dummy
        {
            get { return (T)GetValue(DummyProperty); }
            set { SetValue(DummyProperty, value); }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Evaluate the binding
        /// </summary>
        /// <param name="bindingToEvaluate">The BindingBase to get the value of</param>
        /// <returns>The result of the BindingBase</returns>
        public T GetBoundValue(BindingBase bindingToEvaluate)
        {
            BindingOperations.SetBinding(this, GenericBindingEvaluator<T>.DummyProperty, bindingToEvaluate);
            return this.Dummy;
        }
        #endregion
    }
}
