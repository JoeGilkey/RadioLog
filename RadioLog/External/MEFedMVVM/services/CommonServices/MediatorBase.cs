using System;
using MEFedMVVM.Services.Contracts;
using System.Reflection;

namespace MEFedMVVM.Services.CommonServices
{
    public abstract class MediatorBase : IMediatorBase
    {
        private readonly MessageToActionMap _invocationList = new MessageToActionMap();

        public void Register(object target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            foreach (var methodInfo in target.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (MediatorMessageSinkAttribute attribute in methodInfo.GetCustomAttributes(typeof(MediatorMessageSinkAttribute), true))
                {
                    if (methodInfo.GetParameters().Length != 1)
                        throw new InvalidOperationException("The registered method should only have 1 parameter since the Mediator has only 1 argument to pass");
                    _invocationList.AddAction(attribute.Message, target, methodInfo, attribute.ParameterType);
                }
            }
        }

        public void NotifyColleagues<T>(string message, T parameter)
        {
            var actions = _invocationList.GetActions(message);

            if (actions != null)
            {
                actions.ForEach(action => action.DynamicInvoke(parameter));
            }
        }

        public void NotifyColleagues(string message)
        {
            var actions = _invocationList.GetActions(message);

            if (actions != null)
            {
                actions.ForEach(action => action.DynamicInvoke());
            }
        }

        protected MessageToActionMap InvocationList
        {
            get
            {
                return _invocationList;
            }
        }
    }
}
