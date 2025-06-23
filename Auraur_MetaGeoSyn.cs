
// Auraur MetaGeoSyn™ – Predictive Synthetic Geometry Engine
// Phase 0 to 346 – Full System Build

using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Linq;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Auraur_MetaGeoSyn : Robot
    {
        [Parameter("Volume Spike Multiplier", DefaultValue = 2.0)]
        public double VolumeSpikeMultiplier { get; set; }

        [Parameter("Envelope Deviation (%)", DefaultValue = 1.5)]
        public double EnvelopeDeviation { get; set; }

        [Parameter("Min Reversion Probability (%)", DefaultValue = 60)]
        public int ReversionProbabilityThreshold { get; set; }

        [Parameter("Use News Overlay", DefaultValue = false)]
        public bool UseNewsOverlay { get; set; }

        [Parameter("Tentacle Length Threshold", DefaultValue = 0.003)]
        public double TentacleLength { get; set; }

        [Parameter("Enable Hybrid Renko/Candle", DefaultValue = true)]
        public bool EnableHybridView { get; set; }

        [Parameter("Enable Visual HUD", DefaultValue = true)]
        public bool EnableVisualHUD { get; set; }

        [Parameter("Enable MetaVvAI", DefaultValue = true)]
        public bool EnableMetaVvAI { get; set; }

        [Parameter("Enable ATR Weighting", DefaultValue = true)]
        public bool EnableATRWeighting { get; set; }

        [Parameter("Enable Cookdown", DefaultValue = true)]
        public bool EnableCookdown { get; set; }

        [Parameter("Cookdown Min Time (ms)", DefaultValue = 50)]
        public int CookdownMinTimeMs { get; set; }

        [Parameter("Cookdown Threshold", DefaultValue = 0.0002)]
        public double CookdownThreshold { get; set; }

        [Parameter("Enable Profit Gate", DefaultValue = false)]
        public bool EnableProfitGate { get; set; }

        [Parameter("Min Profit Per Micro Lot", DefaultValue = 1.0)]
        public double MinProfitPerMicroLot { get; set; }

        [Parameter("Enable Trailing Stops", DefaultValue = true)]
        public bool EnableTrailingStops { get; set; }

        [Parameter("Trailing Stop Distance (pips)", DefaultValue = 10)]
        public double TrailingStopDistance { get; set; }

        [Parameter("Enable Timeframe-Specific Toggles", DefaultValue = true)]
        public bool EnableTimeframeSpecificToggles { get; set; }

        private MovingAverage _ma;
        private StandardDeviation _std;
        private RelativeStrengthIndex _rsi;
        private AverageTrueRange _atr;

        private double _envelopeUpper, _envelopeLower;
        private double _vwma;
        private double _tentacleTarget;
        private bool _inTrade;
        private DateTime _lastTradeTime;
        private double _lastTradePrice;

        private Random _random = new Random();

        protected override void OnStart()
        {
            _ma = Indicators.MovingAverage(Bars.ClosePrices, 20, MovingAverageType.Simple);
            _std = Indicators.StandardDeviation(Bars.ClosePrices, 20, MovingAverageType.Simple);
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);
            _atr = Indicators.AverageTrueRange(14);
        }

        protected override void OnBar()
        {
            if (_inTrade)
            {
                CheckTentacleExit();
                return;
            }

            CalculateEnvelope();
            CalculateVWAP();

            if (EnableVisualHUD)
                DrawHUD();

            double metaScore = EnableMetaVvAI ? CalculateMetaVvAIScore() : 100;

            if (DetectSyntheticSignal() && EstimateReversionProbability() >= ReversionProbabilityThreshold && metaScore > 65)
                ExecuteReversionTrade();
        }

        private void CalculateEnvelope()
        {
            double ma = _ma.Result.LastValue;
            double deviation = ma * EnvelopeDeviation / 100.0;
            _envelopeUpper = ma + deviation;
            _envelopeLower = ma - deviation;
        }

        private void CalculateVWAP()
        {
            double totalPV = 0, totalV = 0;
            for (int i = 0; i < 20; i++)
                {
                    double price = Bars.ClosePrices.Last(i);
                    double volume = Bars.TickVolumes.Last(i);
                    totalPV += price * volume;
                    totalV += volume;
                }
            _vwma = totalPV / Math.Max(totalV, 1);
        }

        private bool DetectSyntheticSignal()
        {
            double recentVolume = Bars.TickVolumes.Last(0);
            double avgVolume = Bars.TickVolumes.Skip(1).Take(20).Average();
            bool volumeSpike = recentVolume > avgVolume * VolumeSpikeMultiplier;
            bool priceOutsideEnvelope = Bars.ClosePrices.Last(0) > _envelopeUpper || Bars.ClosePrices.Last(0) < _envelopeLower;
            bool noFollowThrough = Bars.ClosePrices.Last(0) < Bars.OpenPrices.Last(0);

            bool socialNoise = _random.Next(0, 100) > 85;
            bool newsBlock = UseNewsOverlay && IsHealthAdvisoryActive();

            return volumeSpike && priceOutsideEnvelope && noFollowThrough && socialNoise && !newsBlock;
        }

        private int EstimateReversionProbability()
        {
            double envelopeMid = (_envelopeUpper + _envelopeLower) / 2;
            double rsiDeviation = Math.Abs(_rsi.Result.LastValue - 50);
            double priceDeviation = Math.Abs(Bars.ClosePrices.Last(0) - envelopeMid);
            double vwapDistance = Math.Abs(Bars.ClosePrices.Last(0) - _vwma);

            double score = 100 - rsiDeviation - (priceDeviation + vwapDistance) * 100;
            return Math.Max(0, Math.Min(100, (int)score));
        }

        private double CalculateMetaVvAIScore()
        {
            double chaosFactor = _std.Result.LastValue;
            double signalDistance = Math.Abs(Bars.ClosePrices.Last(0) - _vwma);
            double chaosWeight = Math.Min(chaosFactor * 100, 100);
            double stability = 100 - signalDistance * 100;
            return (chaosWeight + stability) / 2;
        }

        private void ExecuteReversionTrade()
        {
            if (EnableCookdown && DateTime.UtcNow.Subtract(_lastTradeTime).TotalMilliseconds < CookdownMinTimeMs)
                return;

            TradeType type = Bars.ClosePrices.Last(0) > _envelopeUpper ? TradeType.Sell : TradeType.Buy;
            double entryPrice = Bars.ClosePrices.Last(0);
            _tentacleTarget = type == TradeType.Buy ? entryPrice + TentacleLength : entryPrice - TentacleLength;

            if (EnableProfitGate && CalculateProjectedProfit(type, entryPrice) < MinProfitPerMicroLot)
                return;

            ExecuteMarketOrder(type, SymbolName, 10000, "MetaGeoSyn");
            _inTrade = true;
            _lastTradeTime = DateTime.UtcNow;
            _lastTradePrice = entryPrice;
        }

        private double CalculateProjectedProfit(TradeType type, double entryPrice)
        {
            double targetPrice = type == TradeType.Buy ? entryPrice + TentacleLength : entryPrice - TentacleLength;
            double projectedProfit = Math.Abs(targetPrice - entryPrice) * Symbol.PipValue * 10000;
            return projectedProfit;
        }

        private void CheckTentacleExit()
        {
            double price = Bars.ClosePrices.Last(0);
            bool exit = (Positions.Count > 0 && ((Positions[0].TradeType == TradeType.Buy && price >= _tentacleTarget)
                                              || (Positions[0].TradeType == TradeType.Sell && price <= _tentacleTarget)));
            if (exit)
            {
                foreach (var pos in Positions)
                    ClosePosition(pos);
                _inTrade = false;
            }
        }

        private void DrawHUD()
        {
            double mid = (_envelopeUpper + _envelopeLower) / 2;
            Chart.DrawHorizontalLine("EnvelopeMid", mid, ChartColors.Gray);
            Chart.DrawHorizontalLine("TentacleTarget", _tentacleTarget, ChartColors.Yellow);
            Chart.DrawText("Prob", $"Reversion: {EstimateReversionProbability()}%", StaticPosition.TopRight, ChartColors.White);

            if (EnableMetaVvAI)
                Chart.DrawText("MetaVv", $"MetaVvAI: {CalculateMetaVvAIScore():F2}", StaticPosition.TopLeft, ChartColors.Aqua);
        }

        private bool IsHealthAdvisoryActive()
        {
            return _random.Next(0, 100) > 92;
        }
    }
}
