using System;
using System.Collections.Generic;
using System.Reflection;

namespace MEFedMVVM.Services.CommonServices
{
    public class MessageToActionMap
    {
        private readonly Dictionary<string, List<WeakAction>> _map = new Dictionary<string, List<WeakAction>>();

        internal void AddAction(string message, object target, MethodInfo method, Type actionType)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (method == null)
                throw new ArgumentNullException("method");

            lock (_map)
            {
                if (!_map.ContainsKey(message))
                    _map[message] = new List<WeakAction>();
                _map[message].Add(new WeakAction(target, method, actionType));
            }
        }

        /// <summary>
        /// Get the list of actions that will be invoked for a specific message.
        /// </summary>
        /// <param name="message">The message to get the actions for.</param>
        /// <returns>Returns a list of actions that are registered to the specific message.</returns>
        internal List<Delegate> GetActions(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            List<Delegate> actions = null;
            lock (_map)
            {
                if (_map.ContainsKey(message))
                {
                    List<WeakAction> weakActions = _map[message];
                    actions = new List<Delegate>(weakActions.Count);

                    GetWeakActionList(actions, weakActions);

                    if (weakActions.Count == 0)
                        _map.Remove(message);
                }
            }

            return actions;
        }

        private void GetWeakActionList(List<Delegate> actions, List<WeakAction> weakActions)
        {
            for (int i = weakActions.Count - 1; i >= 0; i--)
            {
                WeakAction weakAction = weakActions[i];
                if (!weakAction.IsAlive)
                    weakActions.RemoveAt(i);
                else
                    actions.Add(weakAction.CreateAction());
            }
        }
    }
}
