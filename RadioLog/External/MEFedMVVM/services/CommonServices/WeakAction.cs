using System;
using System.Reflection;

namespace MEFedMVVM.Services.CommonServices
{
    internal class WeakAction
    {
        private readonly MethodInfo _method;
        private readonly Type _delegateType;
        private readonly WeakReference _weakRef;

        /// <summary>
        /// Instantiate a new instance of <see cref="WeakAction"/>.
        /// </summary>
        /// <param name="target">The instance to store as a weak reference.</param>
        /// <param name="method">The method information to create the action for.</param>
        /// <param name="parameterType">The type of parameter to be passed in the action.
        /// Pass null if there's no parameter.</param>
        internal WeakAction(object target, MethodInfo method, Type parameterType)
        {
            _weakRef = new WeakReference(target);
            _method = method;

            if (parameterType == null)
                _delegateType = typeof(Action);
            else
                _delegateType = typeof(Action<>).MakeGenericType(parameterType);
        }

        /// <summary>
        /// Create a temporary delegate so that we can invoke the method on the target.
        /// </summary>
        /// <returns></returns>
        internal Delegate CreateAction()
        {
            object target = _weakRef.Target;
            if (target == null) return null;

            // Rehydrate this back into a real action so that the
            // method can be invoked on the target.
            return Delegate.CreateDelegate(_delegateType, target, _method);
        }

        /// <summary>
        /// Is the object still in memory? Returns true if it is, false otherwise.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return _weakRef.IsAlive;
            }
        }
    }
}
