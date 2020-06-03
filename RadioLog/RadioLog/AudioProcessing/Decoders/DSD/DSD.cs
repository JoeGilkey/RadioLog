using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RadioLog.AudioProcessing.Decoders.DSD
{
    public class DSD
    {
        private Stream _sourceStream = null;

        private PointerArrayHolder<int> _dibit_buf = new PointerArrayHolder<int>(1000000, 200);
        private int _repeat = 0;
        private int _center = 0;
        private int _jitter = -1;
        private int _synctype = -1;
        private short _min = -15000;
        private short _max = 15000;
        private int _lmid = 0;
        private int _umid = 0;
        private int _minref = -12000;
        private int _maxref = 12000;
        private int _lastsample = 0;
        private int[] _sbuf = new int[128];
        private int _sidx = 0;
        private int _ssize = 0;
        private int[] _maxbuf = new int[1024];
        private int[] _minbuf = new int[1024];
        private int _midx = 0;
        private string _err_str = string.Empty;
        private string _fsubtype = string.Empty;
        private string _ftype = string.Empty;
        private int _symbolcnt = 0;
        private int _rf_mod = 0;
        private int _numflips = 0;
        private int _lastsynctype = 0;
        private int _lastp25type = 0;
        private int _offset = 0;
        private int _carrier = 0;
        private char[,] _tg = new char[25, 16];
        private int _tgcount = 0;
        private int _lasttg = 0;
        private int _lastsrc = 0;
        private int _nac = 0;
        private int _errs = 0;
        private int _errs2 = 0;
        private int _mbe_file_type = 0;
        private int _optind = 0;
        private int _numtdulc = 0;
        private int _firstframe = 0;
        private string _slot0light = string.Empty;
        private string _slot1light = string.Empty;
        private float _aout_gain = 0.0f;
        private int _samplesPerSymbol = 0;
        private int _symbolCenter = 0;
        private int _symboltiming = 0;
        private bool _use_cosine_filter = true;
        private int _mod_threashold = 0;
        private int _mod_qpsk = 0;
        private int _mod_gfsk = 0;
        private int _mod_c4fm = 0;
        private PointerArrayHolder<float> aout_max_buf = new PointerArrayHolder<float>(200);

        private void InitValues() { }

        private void NoCarrier()
        {
            _dibit_buf.PointerPosition = 200;
            _dibit_buf.ClearValueRange(0, 200);
            _jitter = -1;
            _lastsynctype = -1;
            _carrier = 0;
            _max = 15000;
            _min = -15000;
            _center = 0;
            _err_str = string.Empty;
            _fsubtype = string.Empty;
            _ftype = string.Empty;
            _errs = 0;
            _errs2 = 0;
            _lasttg = 0;
            _lastsrc = 0;
            _lastp25type = 0;
            _repeat = 0;
            _nac = 0;
            _numtdulc = 0;
            _slot0light = " slot0 ";
            _slot1light = " slot1 ";
        }

        private short ReadNextSample()
        {
            //JG Need to update this to reflect reading a single item from the stream...
            return 0;
        }
        private int GetSymbol(int have_sync)
        {
            short sample = 0;
            int i, sum, symbol, count;
            sum = 0;
            count = 0;
            for (i = 0; i < _samplesPerSymbol; i++)
            {
                //timing control
                if ((i == 0) && (have_sync == 0))
                {
                    if (_samplesPerSymbol == 20)
                    {
                        if ((_jitter >= 7) && (_jitter <= 10))
                        {
                            i--;
                        }
                        else if ((_jitter >= 11) && (_jitter <= 14))
                        {
                            i++;
                        }
                    }
                    else if (_rf_mod == 1)
                    {
                        if ((_jitter >= 0) && (_jitter < _symbolCenter))
                        {
                            i++;
                        }
                        else if ((_jitter > _symbolCenter) && (_jitter < 10))
                        {
                            i--;
                        }
                    }
                    else if (_rf_mod == 2)
                    {
                        if ((_jitter >= _symbolCenter - 1) && (_jitter <= _symbolCenter))
                        {
                            i--;
                        }
                        else if ((_jitter >= _symbolCenter + 1) && (_jitter <= _symbolCenter + 2))
                        {
                            i++;
                        }
                    }
                    else if (_rf_mod == 0)
                    {
                        if ((_jitter > 0) && (_jitter <= _symbolCenter))
                        {
                            i--;
                        }
                        else if ((_jitter > _symbolCenter) && (_jitter < _samplesPerSymbol))
                        {
                            i++;
                        }
                    }
                    _jitter = -1;
                }
                sample = ReadNextSample();

                if (_use_cosine_filter)
                {
                    //JG need to implement dmr_filer & nxdn_filter...
                    //if (_lastsynctype >= 10 && _lastsynctype <= 13)
                    //    sample = dmr_filter(sample);
                    //else if (_lastsynctype == 8 || _lastsynctype == 9 || _lastsynctype == 16 || _lastsynctype == 17)
                    //    sample = nxdn_filter(sample);
                }

                if ((sample > _max) && (have_sync == 1) && (_rf_mod == 0))
                {
                    sample = _max;
                }
                else if ((sample < _min) && (have_sync == 1) && (_rf_mod == 0))
                {
                    sample = _min;
                }

                if (sample > _center)
                {
                    if (_lastsample < _center)
                    {
                        _numflips += 1;
                    }
                    if (sample > (_maxref * 1.25))
                    {
                        if (_lastsample < (_maxref * 1.25))
                        {
                            _numflips += 1;
                        }
                        if ((_jitter < 0) && (_rf_mod == 1))
                        {
                            _jitter = i;
                        }
                        if ((_symboltiming == 1) && (have_sync == 0) && (_lastsynctype != -1))
                        {
                            //print("O");
                        }
                    }
                    else
                    {
                        if ((_symboltiming == 1) && (have_sync == 0) && (_lastsynctype != -1))
                        {
                            //print("+");
                        }
                        if ((_jitter < 0) && (_lastsample < _center) && (_rf_mod != 1))
                        {
                            _jitter = i;
                        }
                    }
                }
                else
                {
                    if (_lastsample > _center)
                    {
                        _numflips += 1;
                    }
                    if (sample < (_minref * 1.25))
                    {
                        if (_lastsample > (_minref * 1.25))
                        {
                            _numflips += 1;
                        }
                        if ((_jitter < 0) && (_rf_mod == 1))
                        {
                            _jitter = 1;
                        }
                        if ((_symboltiming == 1) && (have_sync == 0) && (_lastsynctype != -1))
                        {
                            //print("X");
                        }
                    }
                    else
                    {
                        if ((_symboltiming == 1) && (have_sync == 0) && (_lastsynctype != -1))
                        {
                            //print("-");
                        }
                        if ((_jitter < 0) && (_lastsample < _center) && (_rf_mod != -1))
                        {
                            _jitter = i;
                        }
                    }
                }
                if (_samplesPerSymbol == 20)
                {
                    if ((i >= 9) && (i <= 11))
                    {
                        sum += sample;
                        count++;
                    }
                }
                if (_samplesPerSymbol == 5)
                {
                    if (i == 2)
                    {
                        sum += sample;
                        count++;
                    }
                }
                else
                {
                    if (_rf_mod == 0)
                    {
                        if ((i >= _symbolCenter - 1) && (i <= _symbolCenter + 2))
                        {
                            sum += sample;
                            count++;
                        }
                    }
                    else
                    {
                        if ((i == _symbolCenter - 1) || (i == _symbolCenter + 1))
                        {
                            sum += sample;
                            count++;
                        }
                    }
                }

                _lastsample = sample;
            }
            symbol = (sum / count);
            if ((_symboltiming == 1) && (have_sync == 0) && (_lastsynctype != -1))
            {
                if (_jitter >= 0)
                {
                    //println("%i", _jitter);
                }
                else
                {
                    //println();
                }
            }
            _symbolcnt++;
            return symbol;
        }

        /* detects frame sync and returns frame type
         *  0 = +P25p1
         *  1 = -P25p1
         *  2 = +X2-TDMA (non inverted signal data frame)
         *  3 = -X2-TDMA (inverted signal voice frame)
         *  4 = +X2-TDMA (non inverted signal voice frame)
         *  5 = -X2-TDMA (inverted signal data frame)
         *  6 = +D-STAR
         *  7 = -D-STAR
         *  8 = +NXDN (non inverted voice frame)
         *  9 = -NXDN (inverted voice frame)
         * 10 = +DMR (non inverted signal data frame)
         * 11 = -DMR (inverted signal voice frame)
         * 12 = +DMR (non inverted signal voice frame)
         * 13 = -DMR (inverted signal data frame)
         * 14 = +ProVoice
         * 15 = -ProVoice
         * 16 = +NXDN (non inverted data frame)
         * 17 = -NXDN (inverted data frame)
         * 18 = +D-STAR_HD
         * 19 = -D-STAR_HD
         */
        private int GetFrameSync()
        {
            int[] lbuf = new int[24]; int[] lbuf2 = new int[24];
            int t = 0, symbol = 0, sync = 0, lmin = 0, lmax = 0, lidx = 0, lsum = 0, lastt=0;
            for (int i = 18; i < 24; i++)
            {
                lbuf[i] = 0;
                lbuf2[1] = 0;
            }
            _numflips = 0;
            while (sync == 0)
            {
                t++;
                symbol = GetSymbol(0);
                lbuf[lidx] = symbol;
                _sbuf[_sidx] = symbol;
                if (lidx == 23)
                {
                    lidx = 0;
                }
                else
                {
                    lidx++;
                }
                if (_sidx == (_ssize - 1))
                {
                    _sidx = 0;
                }
                else
                {
                    _sidx++;
                }
                if (lastt == 23)
                {
                    lastt = 0;
                    if (_numflips > _mod_threashold)
                    {
                        if (_mod_qpsk == 1)
                        {
                            _rf_mod = 1;
                        }
                    }
                    else if (_numflips > 18)
                    {
                        if (_mod_gfsk == 1)
                        {
                            _rf_mod = 2;
                        }
                    }
                    else
                    {
                        if (_mod_c4fm == 1)
                        {
                            _rf_mod = 0;
                        }
                    }
                    _numflips = 0;
                }
                else
                {
                    lastt++;
                }
            }
            return -1;
        }
    }

}
