using System;
using MEFedMVVM.Services.Contracts;
using System.Windows;

namespace Cinch
{
    public interface IVSM : IContextAware
    {
        string LastStateExecuted { get; }
        void GoToState(string stateName);
    }
}

