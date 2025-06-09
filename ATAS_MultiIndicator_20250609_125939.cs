using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using ATAS.Indicators.Indicators;
using ATAS.Indicators.Technical;
using ATAS.Strategies.Chart;
using Utils.Common.Logging;

namespace ATAS.Strategies.Chart
{
    [DisplayName("Multi-Indicateur ATAS")]
    public class MultiIndicator : ChartStrategy
    {
        private readonly ValueDataSeries _buySignals = new("Signaux d'achat");
        private readonly ValueDataSeries _sellSignals = new("Signaux de vente");
        private readonly ValueDataSeries _volumeSignals = new("Signaux de volume");
        private readonly SMA _sma20 = new() { Period = 20 };
        private readonly SMA _sma50 = new() { Period = 50 };
        private readonly SMA _sma200 = new() { Period = 200 };
        private readonly RSI _rsi = new() { Period = 14 };
        private readonly MACD _macd = new();
        private readonly BollingerBands _bollinger = new() { Period = 20, Width = 2 };
        private readonly VolumeProfile _volumeProfile = new();
        private readonly VWAP _vwap = new();
        private readonly ATR _atr = new() { Period = 14 };
        private readonly ValueDataSeries _previousDayHigh = new("Haut précédent");
        private readonly ValueDataSeries _previousDayLow = new("Bas précédent");
        private readonly ValueDataSeries _previousDayOpen = new("Ouverture précédente");
        private readonly ValueDataSeries _previousDayClose = new("Clôture précédente");
        private DateTime _lastDay = DateTime.MinValue;
        private decimal _first30MinHigh;
        private decimal _first30MinLow;
        private bool _isFirst30MinCalculated = false;

        [Display(Name = "Volume minimum pour signal", GroupName = "Paramètres de volume", Order = 0)]
        public int MinVolume { get; set; } = 1000;

        [Display(Name = "Volume élevé", GroupName = "Paramètres de volume", Order = 1)]
        public int HighVolume { get; set; } = 2000;

        [Display(Name = "Stop Loss (points)", GroupName = "Gestion du risque", Order = 0)]
        public int StopLoss { get; set; } = 50;

        [Display(Name = "Take Profit (points)", GroupName = "Gestion du risque", Order = 1)]
        public int TakeProfit { get; set; } = 100;

        [Display(Name = "Trailing Stop (points)", GroupName = "Gestion du risque", Order = 2)]
        public int TrailingStop { get; set; } = 30;

        [Display(Name = "Couleur ligne haut précédent", GroupName = "Style - Niveaux précédents", Order = 0)]
        public Color PreviousDayHighColor { get; set; } = Colors.Red;

        [Display(Name = "Couleur ligne bas précédent", GroupName = "Style - Niveaux précédents", Order = 1)]
        public Color PreviousDayLowColor { get; set; } = Colors.Green;

        [Display(Name = "Couleur ligne ouverture précédente", GroupName = "Style - Niveaux précédents", Order = 2)]
        public Color PreviousDayOpenColor { get; set; } = Colors.Blue;

        [Display(Name = "Couleur ligne clôture précédente", GroupName = "Style - Niveaux précédents", Order = 3)]
        public Color PreviousDayCloseColor { get; set; } = Colors.Purple;

        [Display(Name = "Épaisseur des lignes", GroupName = "Style - Niveaux précédents", Order = 4)]
        public int LineThickness { get; set; } = 2;

        [Display(Name = "Activer alerte haut précédent", GroupName = "Alertes - Niveaux précédents", Order = 0)]
        public bool AlertPreviousDayHigh { get; set; } = true;

        [Display(Name = "Activer alerte bas précédent", GroupName = "Alertes - Niveaux précédents", Order = 1)]
        public bool AlertPreviousDayLow { get; set; } = true;

        [Display(Name = "Activer alerte ouverture précédente", GroupName = "Alertes - Niveaux précédents", Order = 2)]
        public bool AlertPreviousDayOpen { get; set; } = true;

        [Display(Name = "Activer alerte clôture précédente", GroupName = "Alertes - Niveaux précédents", Order = 3)]
        public bool AlertPreviousDayClose { get; set; } = true;

        [Display(Name = "Couleur zone 30min", GroupName = "Style - Zone 30min", Order = 0)]
        public Color First30MinZoneColor { get; set; } = Colors.Yellow;

        [Display(Name = "Opacité zone 30min", GroupName = "Style - Zone 30min", Order = 1)]
        public int First30MinZoneOpacity { get; set; } = 50;

        [Display(Name = "Activer alerte zone 30min", GroupName = "Alertes - Zone 30min", Order = 0)]
        public bool AlertFirst30MinZone { get; set; } = true;

        [Display(Name = "Couleur POC", GroupName = "Style - Profil de volume", Order = 0)]
        public Color PocColor { get; set; } = Colors.Orange;

        [Display(Name = "Épaisseur ligne POC", GroupName = "Style - Profil de volume", Order = 1)]
        public int PocLineThickness { get; set; } = 2;

        [Display(Name = "Seuil volume faible (%)", GroupName = "Paramètres - Profil de volume", Order = 0)]
        public int LowVolumeThreshold { get; set; } = 20;

        [Display(Name = "Activer alerte POC", GroupName = "Alertes - Profil de volume", Order = 0)]
        public bool AlertPoc { get; set; } = true;

        [Display(Name = "Activer alerte zones faibles", GroupName = "Alertes - Profil de volume", Order = 1)]
        public bool AlertLowVolumeZones { get; set; } = true;

        public MultiIndicator()
        {
            // Configuration des séries de données
            _buySignals.VisualType = VisualMode.UpArrow;
            _buySignals.Color = Colors.Green;
            _sellSignals.VisualType = VisualMode.DownArrow;
            _sellSignals.Color = Colors.Red;
            _volumeSignals.VisualType = VisualMode.Dot;
            _volumeSignals.Color = Colors.Blue;
            _previousDayHigh.VisualType = VisualMode.Line;
            _previousDayLow.VisualType = VisualMode.Line;
            _previousDayOpen.VisualType = VisualMode.Line;
            _previousDayClose.VisualType = VisualMode.Line;

            // Ajout des indicateurs
            AddIndicator(_sma20);
            AddIndicator(_sma50);
            AddIndicator(_sma200);
            AddIndicator(_rsi);
            AddIndicator(_macd);
            AddIndicator(_bollinger);
            AddIndicator(_volumeProfile);
            AddIndicator(_vwap);
            AddIndicator(_atr);
            AddIndicator(_previousDayHigh);
            AddIndicator(_previousDayLow);
            AddIndicator(_previousDayOpen);
            AddIndicator(_previousDayClose);

            // Configuration des paramètres
            MinVolume = 1000;
            HighVolume = 2000;
            StopLoss = 50;
            TakeProfit = 100;
            TrailingStop = 30;
            LineThickness = 2;
            PocLineThickness = 2;
            LowVolumeThreshold = 20;
            First30MinZoneOpacity = 50;
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar < 200) return;

            var currentPrice = GetCandle(bar).Close;
            var currentVolume = GetCandle(bar).Volume;
            var sma20 = _sma20.Calculate(bar, value);
            var sma50 = _sma50.Calculate(bar, value);
            var sma200 = _sma200.Calculate(bar, value);
            var rsi = _rsi.Calculate(bar, value);
            var macd = _macd.Calculate(bar, value);
            var bollinger = _bollinger.Calculate(bar, value);
            var vwap = _vwap.Calculate(bar, value);
            var atr = _atr.Calculate(bar, value);

            // Vérification des positions existantes
            var hasPosition = Positions.Any();

            // Gestion du trailing stop pour les positions existantes
            if (hasPosition)
            {
                foreach (var position in Positions)
                {
                    if (position.Side == Side.Buy)
                    {
                        var newStopPrice = currentPrice - TrailingStop * TickSize;
                        if (newStopPrice > position.StopPrice)
                        {
                            position.StopPrice = newStopPrice;
                        }
                    }
                    else
                    {
                        var newStopPrice = currentPrice + TrailingStop * TickSize;
                        if (newStopPrice < position.StopPrice)
                        {
                            position.StopPrice = newStopPrice;
                        }
                    }
                }
            }

            // Conditions d'achat
            if (!hasPosition && currentPrice > sma20 && currentPrice > sma50 && currentPrice > sma200 &&
                rsi < 70 && macd > 0 && currentPrice > vwap && currentVolume > MinVolume)
            {
                _buySignals[bar] = currentPrice;
                BuyAtMarket(bar + 1, 1, "Signal d'achat");
                
                // Définition du stop loss et take profit
                var stopPrice = currentPrice - StopLoss * TickSize;
                var takeProfitPrice = currentPrice + TakeProfit * TickSize;
                
                // Application du stop loss et take profit
                var position = Positions.Last();
                position.StopPrice = stopPrice;
                position.ProfitPrice = takeProfitPrice;

                // Alerte
                if (currentVolume > HighVolume)
                {
                    AddAlert("Signal d'achat fort", "Volume élevé détecté", AlertType.Buy);
                }
                else
                {
                    AddAlert("Signal d'achat", "Conditions d'achat remplies", AlertType.Buy);
                }
            }

            // Conditions de vente
            if (!hasPosition && currentPrice < sma20 && currentPrice < sma50 && currentPrice < sma200 &&
                rsi > 30 && macd < 0 && currentPrice < vwap && currentVolume > MinVolume)
            {
                _sellSignals[bar] = currentPrice;
                SellAtMarket(bar + 1, 1, "Signal de vente");
                
                // Définition du stop loss et take profit
                var stopPrice = currentPrice + StopLoss * TickSize;
                var takeProfitPrice = currentPrice - TakeProfit * TickSize;
                
                // Application du stop loss et take profit
                var position = Positions.Last();
                position.StopPrice = stopPrice;
                position.ProfitPrice = takeProfitPrice;

                // Alerte
                if (currentVolume > HighVolume)
                {
                    AddAlert("Signal de vente fort", "Volume élevé détecté", AlertType.Sell);
                }
                else
                {
                    AddAlert("Signal de vente", "Conditions de vente remplies", AlertType.Sell);
                }
            }

            // Signal de volume
            if (currentVolume > HighVolume)
            {
                _volumeSignals[bar] = currentPrice;
                AddAlert("Volume élevé", $"Volume: {currentVolume}", AlertType.Info);
            }

            // Gestion des niveaux de la journée précédente
            var currentTime = GetCandle(bar).Time;
            if (currentTime.Date != _lastDay)
            {
                if (_lastDay != DateTime.MinValue)
                {
                    var previousDay = GetCandles().Where(c => c.Time.Date == _lastDay).ToList();
                    if (previousDay.Any())
                    {
                        var high = previousDay.Max(c => c.High);
                        var low = previousDay.Min(c => c.Low);
                        var open = previousDay.First().Open;
                        var close = previousDay.Last().Close;

                        _previousDayHigh[bar] = high;
                        _previousDayLow[bar] = low;
                        _previousDayOpen[bar] = open;
                        _previousDayClose[bar] = close;

                        // Alertes pour les niveaux précédents
                        if (AlertPreviousDayHigh) AddAlert("Niveau haut précédent", $"Prix: {high}", AlertType.Info);
                        if (AlertPreviousDayLow) AddAlert("Niveau bas précédent", $"Prix: {low}", AlertType.Info);
                        if (AlertPreviousDayOpen) AddAlert("Niveau ouverture précédente", $"Prix: {open}", AlertType.Info);
                        if (AlertPreviousDayClose) AddAlert("Niveau clôture précédente", $"Prix: {close}", AlertType.Info);
                    }
                }
                _lastDay = currentTime.Date;
                _isFirst30MinCalculated = false;
            }

            // Gestion de la zone des 30 premières minutes
            if (!_isFirst30MinCalculated && currentTime.TimeOfDay <= TimeSpan.FromMinutes(30))
            {
                _first30MinHigh = Math.Max(_first30MinHigh, GetCandle(bar).High);
                _first30MinLow = Math.Min(_first30MinLow, GetCandle(bar).Low);
            }
            else if (!_isFirst30MinCalculated && currentTime.TimeOfDay > TimeSpan.FromMinutes(30))
            {
                _isFirst30MinCalculated = true;
                // Dessiner les zones
                DrawRectangle(bar, _first30MinLow, _first30MinHigh, First30MinZoneColor, First30MinZoneOpacity);
                DrawRectangle(bar, _first30MinHigh, _first30MinHigh + (_first30MinHigh - _first30MinLow), First30MinZoneColor, First30MinZoneOpacity);
                DrawRectangle(bar, _first30MinLow - (_first30MinHigh - _first30MinLow), _first30MinLow, First30MinZoneColor, First30MinZoneOpacity);

                if (AlertFirst30MinZone)
                {
                    AddAlert("Zone 30min calculée", $"Haut: {_first30MinHigh}, Bas: {_first30MinLow}", AlertType.Info);
                }
            }

            // Gestion du profil de volume
            var volumeProfile = _volumeProfile.Calculate(bar, value);
            if (volumeProfile != null)
            {
                var poc = volumeProfile.POC;
                if (poc != 0)
                {
                    // Tracer le POC
                    DrawLine(bar, poc, PocColor, PocLineThickness);
                    if (AlertPoc) AddAlert("POC", $"Prix: {poc}", AlertType.Info);

                    // Détecter les zones de faible volume
                    var lowVolumeZones = volumeProfile.GetLowVolumeZones(LowVolumeThreshold);
                    foreach (var zone in lowVolumeZones)
                    {
                        DrawRectangle(bar, zone.Low, zone.High, Colors.Gray, 30);
                        if (AlertLowVolumeZones) AddAlert("Zone faible volume", $"Haut: {zone.High}, Bas: {zone.Low}", AlertType.Info);
                    }
                }
            }
        }

        private void DrawRectangle(int bar, decimal low, decimal high, Color color, int opacity)
        {
            var rectangle = new Rectangle
            {
                StartPrice = low,
                EndPrice = high,
                Color = Color.FromArgb((byte)opacity, color.R, color.G, color.B),
                StartBar = bar,
                EndBar = bar + 1000 // Étendre sur 1000 barres
            };
            Draw(rectangle);
        }

        private void DrawLine(int bar, decimal price, Color color, int thickness)
        {
            var line = new Line
            {
                StartPrice = price,
                EndPrice = price,
                Color = color,
                Thickness = thickness,
                StartBar = bar,
                EndBar = bar + 1000 // Étendre sur 1000 barres
            };
            Draw(line);
        }
    }
} 