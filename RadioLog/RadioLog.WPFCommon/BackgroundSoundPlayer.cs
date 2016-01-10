using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.ComponentModel;

namespace RadioLog.WPFCommon
{
    public class BackgroundSoundPlayer
    {
        private BackgroundWorker _bwSound = null;
        private string _soundFileName = string.Empty;

        public BackgroundSoundPlayer(string soundFileName)
        {
            _soundFileName = soundFileName;
            if (!System.IO.File.Exists(soundFileName))
                return;
            _bwSound = new BackgroundWorker();
            _bwSound.RunWorkerCompleted += (bcs, bce) => { };
            _bwSound.DoWork += (bcs, bce) =>
            {
                try
                {
                    MediaPlayer _mpSound = new MediaPlayer();
                    _mpSound.Open(new Uri(_soundFileName, UriKind.Absolute));
                    _mpSound.IsMuted = false;
                    _mpSound.Volume = 1.0d;
                    _mpSound.Play();
                }
                catch { }
            };
        }

        public void PlaySound()
        {
            if (_bwSound != null && !_bwSound.IsBusy)
            {
                _bwSound.RunWorkerAsync();
            }
        }
    }
}
