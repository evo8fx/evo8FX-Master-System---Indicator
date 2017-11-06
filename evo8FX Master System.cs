using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Levels(32, 50, 68)]
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class evo8FXMasterSystem : Indicator
    {
    
    /*-- START Summary ---------------------------------------*/
        private double SpreadinPips, totPosLoss_Amount;
        private string pairID;
        private bool debug_PrintValues;
        private int totPositions;
        private long totPosVol;

        [Parameter("RiskPercent", DefaultValue = 4.5)]
        public double RiskPercent { get; set; }

    /*-- END Summary ---------------------------------------*/

    /*-- START HeikenAshi Standard ---------------------------------------*/

        private IndicatorDataSeries _haOpen;
        private IndicatorDataSeries _haClose;

        [Parameter("Show HeikenAshi", DefaultValue = true)]
        public bool enable_HeikenAshi { get; set; }

        [Parameter("Candle width", DefaultValue = 5)]
        public int CandleWidth { get; set; }

        [Parameter("Up color", DefaultValue = "Blue")]
        public string UpColor { get; set; }

        [Parameter("Down color", DefaultValue = "Red")]
        public string DownColor { get; set; }

        private Colors _upColor;
        private Colors _downColor;
        private bool _incorrectColors;
        private Random _random = new Random();

    /*-- START HeikenAshi Standard ---------------------------------------*/

        protected override void Initialize()
        {
            // Initialize and create nested indicators ---------------------
            pairID = Symbol.Code;
            debug_PrintValues = false;

            // HeikenAshi Standard -----------------------------------------
            if (enable_HeikenAshi) { 
                _haOpen = CreateDataSeries();
                _haClose = CreateDataSeries();

                if (!Enum.TryParse<Colors>(UpColor, out _upColor) || !Enum.TryParse<Colors>(DownColor, out _downColor))
                    _incorrectColors = true;
            }
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...

            SpreadinPips = Math.Round((Symbol.Spread / Symbol.PipSize), 2);
            WritetoChart();


            totPosVol = 0;
            totPositions = 0;
            totPosLoss_Amount = 0;


            for (int i = 0; i < Positions.Count; i++)
            {
                if (Positions[i].SymbolCode == pairID)
                {
                    totPosVol = totPosVol + Positions[i].Volume;
                    totPositions++;

                    // get value of loss if StopLoss it hit
                    //totPosLoss_Amount = totPosLoss_Amount + Positions[i].StopLoss.
                }
            }

            // HeikenAshi Standard -----------------------------------------
            if (enable_HeikenAshi)
            {
                if (_incorrectColors)
                {
                    var errorColor = _random.Next(2) == 0 ? Colors.Red : Colors.White;
                    ChartObjects.DrawText("Error", "Incorrect colors", StaticPosition.Center, errorColor);
                    return;
                }

                var open = MarketSeries.Open[index];
                var high = MarketSeries.High[index];
                var low = MarketSeries.Low[index];
                var close = MarketSeries.Close[index];

                var haClose = (open + high + low + close) / 4;
                double haOpen;
                if (index > 0)
                    haOpen = (_haOpen[index - 1] + _haClose[index - 1]) / 2;
                else
                    haOpen = (open + close) / 2;

                var haHigh = Math.Max(Math.Max(high, haOpen), haClose);
                var haLow = Math.Min(Math.Min(low, haOpen), haClose);

                var color = haOpen > haClose ? _downColor : _upColor;
                ChartObjects.DrawLine("candle" + index, index, haOpen, index, haClose, color, CandleWidth, LineStyle.Solid);
                ChartObjects.DrawLine("line" + index, index, haHigh, index, haLow, color, 1, LineStyle.Solid);

                _haOpen[index] = haOpen;
                _haClose[index] = haClose;
            }
        }


        private void WritetoChart()
        {

            // -- TOP LEFT OUTPUT
            string chartTL = "Spread: " + SpreadinPips + " pips";
            chartTL = chartTL + "\nTrade Risk Pct: " + RiskPercent + "%";
            chartTL = chartTL + "\nTrade Risk Amt: " + String.Format("{0:C}", Account.Balance * (RiskPercent * 0.01));

            chartTL = chartTL + "\n\n" + pairID + " POSITIONS";
            chartTL = chartTL + "\n - Pos: " + totPositions;
            chartTL = chartTL + "\n - Vol: " + totPosVol;
            chartTL = chartTL + "\n - Qty: " + Symbol.VolumeToQuantity(totPosVol) + " lots";
            ChartObjects.DrawText("TopLeft", chartTL, StaticPosition.TopLeft, Colors.LightGray);

            // -- TOP CENTER OUTPUT
            string chartTC = "";
            ChartObjects.DrawText("TopCenter", chartTC, StaticPosition.TopCenter);

            // -- TOP RIGHT OUTPUT
            string chartTR = "";
            chartTR = chartTR + "Balance: " + String.Format("{0:C}", Account.Balance, 2);
            chartTR = chartTR + "\nEquity: " + String.Format("{0:C}", Account.Equity, 2);
            chartTR = chartTR + "\nDraw Down: " + Math.Round(((Account.Equity / Account.Balance) * 100) - 100, 2) + "%";
            chartTR = chartTR + "\nMargin Level: " + Math.Round(Account.MarginLevel.Value, 2) + "%";
            ChartObjects.DrawText("TopRight", chartTR.PadRight(5), StaticPosition.TopRight, Colors.LightGray);

        }
    }
}
