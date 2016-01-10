using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MEFedMVVM.ViewModelLocator;
using MEFedMVVM.Services.Contracts;
using System.Reflection;
using System.ComponentModel.Composition;

namespace MEFedMVVM.Services.CommonServices
{
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.Shared)]
    [ExportService(ServiceType.Both, typeof(IMediator))]
    public class Mediator : MediatorBase, IMediator
    {
        /// <summary>
        /// Register a ViewModel so that it can receive notifications from the mediator.
        /// </summary>
        /// <param name="message">The message to register.</param>
        /// <param name="callback">The callback.</param>
        public void Register(string message, Delegate callback)
        {
            ParameterInfo[] parameters = callback.Method.GetParameters();

            if (parameters != null && parameters.Length > 1)
                throw new InvalidOperationException("The registered delegate should only have 0 or 1 parameter since the Mediator has up to 1 argument to pass");
            Type paramType = parameters[0].ParameterType;
            InvocationList.AddAction(message, callback.Target, callback.Method, paramType);
        }
    }
}
