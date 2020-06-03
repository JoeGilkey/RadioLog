using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RadioLog.Common
{
    public abstract class BaseTaskObject
    {
        private bool _shouldRun = false;
        private bool _isRunning = false;
        private int _sleepDelay = 1000;

        protected abstract void ThreadProc();

        public BaseTaskObject(int sleepDelay = 1000)
        {
            _shouldRun = false;
            _isRunning = false;
            _sleepDelay = sleepDelay;
        }

        private void ThreadHandlerProc()
        {
            if (_isRunning || !_shouldRun)
                return;
            _isRunning = true;
            try
            {
                while (_shouldRun)
                {
                    ThreadProc();
                    if (_shouldRun && _sleepDelay > 0)
                    {
                        Thread.Sleep(_sleepDelay);
                    }
                }
                DoPostStop();
            }
            finally
            {
                _isRunning = false;
            }
        }

        protected virtual bool DoPreStart() { return true; }
        protected virtual void DoPostStop() { }

        public bool Restart()
        {
            Stop();
            return Start();
        }
        public bool Start()
        {
            if (_isRunning)
                return true;
            try
            {
                _shouldRun = DoPreStart();
                if (_shouldRun)
                {
                    Thread t = new Thread(ThreadHandlerProc);
                    t.IsBackground = true;
                    t.Start();
                }
                return _shouldRun;
            }
            catch { return false; }
        }
        public void Stop()
        {
            if (_isRunning)
            {
                _shouldRun = false;
                while (_isRunning)
                {
                    Thread.Sleep(10);
                }
            }
        }

        protected bool IsCancelPending { get { return _isRunning && !_shouldRun; } }
        public bool IsRunning { get { return _isRunning; } }
    }
}
