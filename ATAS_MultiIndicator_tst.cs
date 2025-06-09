using System;
using System.Collections.Generic;
using ATAS.Indicators.Technical;
using ATAS.Indicators.Drawing;
using ATAS.Indicators;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;
using System.Linq;
using System.Media;

namespace ATAS.Indicators.Custom
{
    [Indicator("MultiDayLevelsComplete")]
    public class MultiDayLevelsComplete : Indicator
    {
        #region Variables

        private DateTime _currentDay = DateTime.MinValue;
        private decimal _prevHigh, _prevLow, _prevOpen, _prevClose;
        private bool _sessionStarted;
        private decimal _open30MinHigh = decimal.MinValue;
        private decimal _open30MinLow = decimal.MaxValue;
        private DateTime _firstBarTime;
        private bool _zoneDrawn;

        private Dictionary<decimal, decimal> _volumeByPrice = new();
        private decimal _poc;
        private bool _pocExtended;
        private int _barsSinceSessionStart;

        private List<decimal> _lvnLevels = new();

        #endregion

        public MultiDayLevelsComplete()
        {
            AddLine(Brushes.Red, 0, "PrevHigh");
            AddLine(Brushes.Blue, 0, "PrevLow");
            AddLine(Brushes.Green, 0, "PrevOpen");
            AddLine(Brushes.Orange, 0, "PrevClose");

            Lines[0].LineWidth = 2;
            Lines[1].LineWidth = 2;
            Lines[2].LineWidth = 1;
            Lines[3].LineWidth = 1;
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            var time = Time[bar];

            if (_currentDay != time.Date)
            {
                // Nouvelle session
                if (_currentDay != DateTime.MinValue)
                {
                    _prevHigh = High.Take(bar).Where((v, i) => Time[i].Date == _currentDay).DefaultIfEmpty().Max();
                    _prevLow = Low.Take(bar).Where((v, i) => Time[i].Date == _currentDay).DefaultIfEmpty().Min();
                    _prevOpen = Open.FirstOrDefault((v, i) => Time[i].Date == _currentDay);
                    _prevClose = Close.LastOrDefault((v, i) => Time[i].Date == _currentDay);
                }

                _currentDay = time.Date;
                _sessionStarted = false;
                _zoneDrawn = false;
                _barsSinceSessionStart = 0;
                _volumeByPrice.Clear();
                _pocExtended = false;
                _lvnLevels.Clear();
            }

            if (!_sessionStarted)
            {
                _firstBarTime = time;
                _sessionStarted = true;

                DrawHorizontalLine("PrevHigh", _prevHigh, Colors.Red);
                DrawHorizontalLine("PrevLow", _prevLow, Colors.Blue);
                DrawHorizontalLine("PrevOpen", _prevOpen, Colors.Green);
                DrawHorizontalLine("PrevClose", _prevClose, Colors.Orange);
            }

            _barsSinceSessionStart++;

            if (time < _firstBarTime.AddMinutes(30))
            {
                _open30MinHigh = Math.Max(_open30MinHigh, High[bar]);
                _open30MinLow = Math.Min(_open30MinLow, Low[bar]);
            }
            else if (!_zoneDrawn)
            {
                DrawRectangle("OpeningRange", _firstBarTime, time, _open30MinLow, _open30MinHigh, Colors.DodgerBlue, Colors.LightBlue, 20);
                DrawRectangle("OR_Upper", _firstBarTime, time, _open30MinHigh + (_open30MinHigh - _open30MinLow), _open30MinHigh, Colors.Gray, Colors.Transparent, 10);
                DrawRectangle("OR_Lower", _firstBarTime, time, _open30MinLow - (_open30MinHigh - _open30MinLow), _open30MinLow, Colors.Gray, Colors.Transparent, 10);
                _zoneDrawn = true;
            }

            // Profil de volume
            var price = Math.Round(Close[bar], 2);
            if (_volumeByPrice.ContainsKey(price))
                _volumeByPrice[price] += Volume[bar];
            else
                _volumeByPrice[price] = Volume[bar];

            if (bar == Count - 1)
            {
                _poc = _volumeByPrice.OrderByDescending(kvp => kvp.Value).First().Key;

                if (!_pocExtended)
                {
                    DrawHorizontalLine("POC", _poc, Colors.Magenta, DashStyles.Solid, 2);
                }

                // Détection des LVNs (volume inférieur à 20% du max et entre deux volumes plus élevés)
                var maxVolume = _volumeByPrice.Max(v => v.Value);
                foreach (var kvp in _volumeByPrice.OrderBy(k => k.Key).Skip(1).Take(_volumeByPrice.Count - 2))
                {
                    var prev = _volumeByPrice[kvp.Key - TickSize];
                    var next = _volumeByPrice.ContainsKey(kvp.Key + TickSize) ? _volumeByPrice[kvp.Key + TickSize] : 0;
                    if (kvp.Value < maxVolume * 0.2m && kvp.Value < prev && kvp.Value < next)
                    {
                        _lvnLevels.Add(kvp.Key);
                        DrawHorizontalLine($"LVN_{kvp.Key}", kvp.Key, Colors.LightGray, DashStyles.Dash, 1);
                    }
                }
            }

            // Alertes : si prix touche un niveau important
            if (Math.Abs(Close[bar] - _prevHigh) < TickSize) Alert("PrevHighTouch", "Touched Previous High");
            if (Math.Abs(Close[bar] - _prevLow) < TickSize) Alert("PrevLowTouch", "Touched Previous Low");
            if (Math.Abs(Close[bar] - _poc) < TickSize) Alert("POCTouch", "Touched POC");

            foreach (var lvn in _lvnLevels)
            {
                if (Math.Abs(Close[bar] - lvn) < TickSize)
                    Alert($"LVN_{lvn}", $"Touched LVN at {lvn}");
            }
        }

        private void Alert(string id, string message)
        {
            AddAlert(id, message, AlertTypes.Sound);
        }
    }
}
