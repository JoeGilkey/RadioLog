using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NAudio.Wave;
using NAudio.Mixer;

namespace RadioLog.AudioProcessing
{
    public class VolumeHelper
    {
        private WeakReference _waveIn;
        private UnsignedMixerControl volumeControl;
        private SignedMixerControl altVolumeControl;

        public WaveInEvent Wave
        {
            get
            {
                if (_waveIn.IsAlive)
                    return _waveIn.Target as WaveInEvent;
                return null;
            }
        }

        public VolumeHelper(WaveInEvent waveIn)
        {
            if (waveIn == null)
                throw new ArgumentNullException();
            _waveIn = new WeakReference(waveIn);
            volumeControl = null;
            altVolumeControl = null;
            TryGetVolumeControl();
        }

        public bool VolumeSetup
        {
            get { return (volumeControl != null || altVolumeControl != null); }
        }
        public double Percent
        {
            get
            {
                try
                {
                    if (!VolumeSetup)
                        TryGetVolumeControl();
                    if (volumeControl != null)
                        return volumeControl.Percent;
                    else if (altVolumeControl != null)
                        return altVolumeControl.Percent;
                    else
                        return 0.0;
                }
                catch { return 0.0; }
            }
            set
            {
                try
                {
                    if (!VolumeSetup)
                        TryGetVolumeControl();
                    if (volumeControl != null)
                        volumeControl.Percent = value;
                    else if (altVolumeControl != null)
                        altVolumeControl.Percent = value;
                }
                catch
                {
                    volumeControl = null;
                    altVolumeControl = null;
                }
            }
        }

        private void TryGetVolumeControl()
        {
            if (Wave == null)
                return;
            try
            {
                int waveInDeviceNumber = Wave.DeviceNumber;
                if (Environment.OSVersion.Version.Major >= 6) // Vista and over
                {
                    var mixerLine = Wave.GetMixerLine();

                    foreach (var control in mixerLine.Controls)
                    {
                        Common.DebugHelper.WriteLine("{0} Mixer Line Control {1} [{2}]", mixerLine.Name, control.Name, control.ControlType);
                    }

                    foreach (var control in mixerLine.Controls)
                    {
                        if (control.ControlType == MixerControlType.Volume)
                        {
                            if (control.IsUnsigned)
                            {
                                try
                                {
                                    this.volumeControl = control as UnsignedMixerControl;
                                    break;
                                }
                                catch { this.volumeControl = null; }
                            }
                            else if (control.IsSigned)
                            {
                                try
                                {
                                    this.altVolumeControl = control as SignedMixerControl;
                                }
                                catch { this.altVolumeControl = null; }
                            }
                        }
                    }
                }
                else
                {
                    var mixer = new Mixer(waveInDeviceNumber);
                    foreach (var destination in mixer.Destinations.Where(d => d.ComponentType == MixerLineComponentType.DestinationWaveIn))
                    {
                        foreach (var source in destination.Sources
                            .Where(source => source.ComponentType == MixerLineComponentType.SourceMicrophone))
                        {
                            foreach (var control in source.Controls
                                .Where(control => control.ControlType == MixerControlType.Volume))
                            {
                                if (control.IsUnsigned)
                                {
                                    try
                                    {
                                        volumeControl = control as UnsignedMixerControl;
                                        break;
                                    }
                                    catch
                                    {
                                        volumeControl = null;
                                    }
                                }
                                else if (control.IsSigned)
                                {
                                    try
                                    {
                                        this.altVolumeControl = control as SignedMixerControl;
                                    }
                                    catch { this.altVolumeControl = null; }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                volumeControl = null;
                altVolumeControl = null;
            }
        }
    }
}
