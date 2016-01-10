using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MEFedMVVM.Services.Contracts;

namespace MEFedMVVM.Services.Contracts
{
    /// <summary>
    /// A message mediator allowing disconnected ViewModels to send and
    /// receive messages. This interface should not be referenced directly 
    /// by the end system.
    /// </summary>
    public interface IMediator : IMediatorBase
    {
        /// <summary>
        /// Register a ViewModel so that it can receive notifications from the mediator.
        /// </summary>
        /// <param name="message">The message to register.</param>
        /// <param name="callback">The callback.</param>
        void Register(string message, Delegate callback);
    }
}
