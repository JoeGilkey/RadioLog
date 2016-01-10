using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEFedMVVM.Services.Contracts
{
    /// <summary>
    /// A message mediator allowing disconnected ViewModels to send and
    /// receive messages. This interface should not be referenced directly 
    /// by the end system.
    /// </summary>
    public interface IMediatorBase
    {
        /// <summary>
        /// Register a ViewModel so that it can receive notifications from the mediator.
        /// </summary>
        /// <param name="target">The instance to register.</param>
        void Register(object target);

        /// <summary>
        /// Notify any registered parties that a specific message has been broadcast.
        /// </summary>
        /// <typeparam name="T">The type of the parameter being passed.</typeparam>
        /// <param name="message">The message that is being broadcast.</param>
        /// <param name="parameter">The parameter to pass with the message.</param>
        void NotifyColleagues<T>(string message, T parameter);

        /// <summary>
        /// Notify any registered parties that a specific message has been broadcast.
        /// </summary>
        /// <param name="message">The message that is being broadcast.</param>
        void NotifyColleagues(string message);
    }
}
