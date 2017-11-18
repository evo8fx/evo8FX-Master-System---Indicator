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

        [Parameter("Intraday Risk", DefaultValue = 3.5)]
        public double S1_RiskPercent { get; set; }

        [Parameter("Sniper Risk", DefaultValue = 10)]
        public double S2_RiskPercent { get; set; }

        [Parameter("Investment Risk", DefaultValue = 2.5)]
        public double S3_RiskPercent { get; set; }

        /*-- END Summary ---------------------------------------*/


        /*-- START HeikenAshi Standard ---------------------------------------*/

        private IndicatorDataSeries _haOpen;
        private IndicatorDataSeries _haClose;

        [Parameter("- Show HeikenAshi ---------", DefaultValue = true)]
        public bool enable_HeikenAshi { get; set; }

        [Parameter("Candle width", DefaultValue = 5)]
        public int CandleWidth { get; set; }

        [Parameter("Up color", DefaultValue = "Green")]
        public string UpColor { get; set; }

        [Parameter("Down color", DefaultValue = "Red")]
        public string DownColor { get; set; }

        private Colors _upColor;
        private Colors _downColor;
        private bool _incorrectColors;
        private Random _random = new Random();

        /*-- START HeikenAshi Standard ---------------------------------------*/


        /*-- START Moving Average --------------------------------------------*/

        [Parameter("- Show EMA --------------", DefaultValue = true)]
        public bool enable_EMA { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Periods", DefaultValue = 200)]
        public int Periods { get; set; }

        [Output("EMA", Color = Colors.SlateBlue, Thickness = 2)]
        public IndicatorDataSeries Result { get; set; }

        private double exp;

        /*-- END   Moving Average --------------------------------------------*/


        /*-- START SCALPER SIGNAL --------------------------------------------*/

        [Parameter("- Show Scalper Signal -------", DefaultValue = true)]
        public bool enable_ScalperSignal { get; set; }

        [Parameter("Sensitivity", DefaultValue = 2, MinValue = 1, MaxValue = 3, Step = 1)]
        public int Sensitivity { get; set; }

        [Parameter("Signal Bar Color", DefaultValue = "Gold")]
        public string SignalBarColor { get; set; }

        [Output("SS Buy", Color = Colors.LimeGreen, Thickness = 7, PlotType = PlotType.Points)]
        public IndicatorDataSeries BuyIndicator { get; set; }

        [Output("SS Sell", Color = Colors.Red, Thickness = 7, PlotType = PlotType.Points)]
        public IndicatorDataSeries SellIndicator { get; set; }

        [Output("SS SignalBarHigh", Color = Colors.Gold, Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries SignalBarHigh { get; set; }

        [Output("SS SignalBarLow", Color = Colors.Gold, Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries SignalBarLow { get; set; }

        enum Signals
        {
            None,
            Buy,
            Sell
        }

        private Colors signalBarColor;
        private Signals lastSignal;
        private DateTime lastTime;
        private AverageTrueRange ATR;

        /*-- END SCALPER SIGNAL ----------------------------------------------*/

        /*-- START Bollinger Bands -------------------------------------------*/

        [Parameter("- Show BollingerBands -------", DefaultValue = true)]
        public bool enable_BollingerBands { get; set; }

        private MovingAverage _bbmovingAverage;
        private StandardDeviation _bbstandardDeviation;

        [Parameter("Period", DefaultValue = 14)]
        public int Period { get; set; }

        [Parameter("SD Weight Coef", DefaultValue = 2.3)]
        public double K { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MaType { get; set; }

        [Parameter()]
        public DataSeries Price { get; set; }

        [Output("BB Main", Color = Colors.DarkSlateGray, LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries bbMain { get; set; }

        [Output("BB Upper", Color = Colors.SlateGray, LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries bbUpper { get; set; }

        [Output("BB Lower", Color = Colors.SlateGray, LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries bbLower { get; set; }

        /*-- END Bollinger Bands ---------------------------------------------*/

        [Parameter("- Show DailyHighLow -------", DefaultValue = true)]
        public bool enable_DailyHighLow { get; set; }

        protected override void Initialize()
        {
            // Initialize and create nested indicators ---------------------
            pairID = Symbol.Code;
            debug_PrintValues = false;

            // HeikenAshi Standard -----------------------------------------
            if (enable_HeikenAshi)
            {
                _haOpen = CreateDataSeries();
                _haClose = CreateDataSeries();

                if (!Enum.TryParse<Colors>(UpColor, out _upColor) || !Enum.TryParse<Colors>(DownColor, out _downColor))
                    _incorrectColors = true;
            }

            // MOVING AVERAGE -----------------------------------------
            if (enable_EMA)
            {
                exp = 2.0 / (Periods + 1);
            }

            // SCALPER SIGNAL -----------------------------------------------
            if (enable_ScalperSignal)
            {
                if (!Enum.TryParse<Colors>(SignalBarColor, out signalBarColor))
                    signalBarColor = Colors.Gold;

                lastSignal = Signals.None;
                lastTime = new DateTime();
                lastTime = MarketSeries.OpenTime[MarketSeries.Close.Count - 1];
            }

            // BOLLINGER BANDS -------------------------------------------------
            if (enable_BollingerBands)
            {
                _bbmovingAverage = Indicators.MovingAverage(Price, Period, MaType);
                _bbstandardDeviation = Indicators.StandardDeviation(Price, Period, MaType);
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
                HeikenAshi_Standard_Main(index);

            // MOVING AVERAGE -----------------------------------------
            if (enable_EMA)
            {
                var previousValue = Result[index - 1];
                if (double.IsNaN(previousValue))
                {
                    Result[index] = Source[index];
                }
                else
                {
                    Result[index] = Source[index] * exp + previousValue * (1 - exp);
                }
            }


            // SCALPER SIGNAL --------------------------------------------
            if (enable_ScalperSignal)
                SignalScalper_Main(index);


            // Daily HighLow ---------------------------------------------------------------------
            if (enable_DailyHighLow)
                DailyHighLow_Main(index);

            // BOLLINGER BANDS -------------------------------------------------
            if (enable_BollingerBands)
            {
                bbMain[index] = _bbmovingAverage.Result[index];
                bbUpper[index] = _bbmovingAverage.Result[index] + K * _bbstandardDeviation.Result[index];
                bbLower[index] = _bbmovingAverage.Result[index] - K * _bbstandardDeviation.Result[index];
            }

        }


        //-----------------------------------------------------------------------------------------------
        // SCALPER SIGNAL FUNCTION ----------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------
        private void SignalScalper_Main(int index)
        {

            if (!NewBar(index) || (index < 6))
                return;

            ATR = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

            double bs = BuySignal(index);
            double ss = SellSignal(index);

            if (bs > 0)
            {
                BuyIndicator[index] = bs;
                SignalBarHigh[index - 3] = MarketSeries.High[index - 3];
                SignalBarLow[index - 3] = MarketSeries.Low[index - 3];
                ChartObjects.DrawLine("SignalBar" + (index - 3), index - 3, SignalBarHigh[index - 3], index - 3, SignalBarLow[index - 3], Colors.Gold, 3, LineStyle.Solid);
            }
            else if (ss > 0)
            {
                SellIndicator[index] = ss;
                SignalBarHigh[index - 3] = MarketSeries.High[index - 3];
                SignalBarLow[index - 3] = MarketSeries.Low[index - 3];
                ChartObjects.DrawLine("SignalBar" + (index - 3), index - 3, SignalBarHigh[index - 3], index - 3, SignalBarLow[index - 3], Colors.Gold, 3, LineStyle.Solid);

            }

        }
        private double SellSignal(int index)
        {
            bool ok = true;

            if (Sensitivity > 2)
                if (MarketSeries.High[index - 6] >= MarketSeries.High[index - 5])
                    ok = false;

            if (Sensitivity > 1)
                if (MarketSeries.High[index - 5] >= MarketSeries.High[index - 4])
                    ok = false;

            if (Sensitivity > 0)
                if (MarketSeries.High[index - 4] >= MarketSeries.High[index - 3])
                    ok = false;

            if (ok)
                if (MarketSeries.Close[index - 2] < MarketSeries.High[index - 3])
                    if (MarketSeries.Close[index - 1] < MarketSeries.Low[index - 3])
                    {
                        lastSignal = Signals.Sell;
                        return (MarketSeries.High[index] + ATR.Result[index]);
                    }

            return (double.NaN);
        }
        private double BuySignal(int index)
        {
            bool ok = true;

            if (Sensitivity > 2)
                if (MarketSeries.Low[index - 6] <= MarketSeries.Low[index - 5])
                    ok = false;

            if (Sensitivity > 1)
                if (MarketSeries.Low[index - 5] <= MarketSeries.Low[index - 4])
                    ok = false;

            if (Sensitivity > 0)
                if (MarketSeries.Low[index - 4] <= MarketSeries.Low[index - 3])
                    ok = false;

            if (ok)
                if (MarketSeries.Close[index - 2] > MarketSeries.Low[index - 3])
                    if (MarketSeries.Close[index - 1] > MarketSeries.High[index - 3])
                    {
                        lastSignal = Signals.Buy;
                        return (MarketSeries.Low[index] - ATR.Result[index]);
                    }

            return (double.NaN);
        }
        private bool NewBar(int index)
        {
            if (lastTime != MarketSeries.OpenTime[index])
            {
                lastTime = MarketSeries.OpenTime[index];
                return true;
            }

            return false;
        }
        //-----------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------
        // DAILY HIGH LOW FUNCTION ----------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------
        private void HeikenAshi_Standard_Main(int index)
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

            //var color = haOpen > haClose ? _downColor : _upColor;
            var haColor = haOpen > haClose ? _downColor : _upColor;

            ChartObjects.DrawLine("candle" + index, index, haOpen, index, haClose, haColor, CandleWidth, LineStyle.Solid);
            ChartObjects.DrawLine("line" + index, index, haHigh, index, haLow, haColor, 1, LineStyle.Solid);

            _haOpen[index] = haOpen;
            _haClose[index] = haClose;
        }
        //-----------------------------------------------------------------------------------------------


        //-----------------------------------------------------------------------------------------------
        // DAILY HIGH LOW FUNCTION ----------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------
        private void DailyHighLow_Main(int index)
        {
            DateTime today = MarketSeries.OpenTime[index].Date;
            DateTime tomorrow = today.AddDays(1);

            double high = MarketSeries.High.LastValue;
            double low = MarketSeries.Low.LastValue;

            for (int i = MarketSeries.Close.Count - 1; i > 0; i--)
            {
                if (MarketSeries.OpenTime[i].Date < today)
                    break;

                high = Math.Max(high, MarketSeries.High[i]);
                low = Math.Min(low, MarketSeries.Low[i]);
            }

            ChartObjects.DrawLine("high " + today, today, high, tomorrow, high, Colors.Pink);
            ChartObjects.DrawLine("low " + today, today, low, tomorrow, low, Colors.Pink);
        }
        //-----------------------------------------------------------------------------------------------


        private void WritetoChart()
        {

            // -- TOP LEFT OUTPUT
            string chartTL = "";
            chartTL += "" + pairID + " POSITIONS";
            chartTL += "\n - Pos: " + totPositions;
            chartTL += "\n - Vol: " + totPosVol;
            chartTL += "\n - Qty: " + Symbol.VolumeToQuantity(totPosVol) + " lots\n\n\n";
            ChartObjects.DrawText("TopLeft", chartTL, StaticPosition.BottomRight);

            // -- TOP CENTER OUTPUT
            string chartTC = "Spread: " + SpreadinPips + " pips";
            ChartObjects.DrawText("TopCenter", chartTC.PadRight(5), StaticPosition.BottomRight, Colors.MediumPurple);

            // -- TOP RIGHT OUTPUT
            double dd = Math.Round(((Account.Equity / Account.Balance) * 100) - 100, 2);
            Colors dd_color = Colors.Green;
            if (dd < -15){
                dd_color = Colors.Red;
            }else if (dd < -10){
                dd_color = Colors.OrangeRed;
            }else if (dd < 0) {
                dd_color = Colors.Orange;
            }
                

            string chartTR = "Draw Down: " + dd + "%";
            ChartObjects.DrawText("DD", chartTR.PadRight(5), StaticPosition.TopRight, dd_color);

            string cR1 = "\n\n-----------------------------\n";
            cR1 += "Intraday Risk: " + S1_RiskPercent + "%";
            cR1 += "\n" + String.Format("{0:C}", Account.Balance * (S1_RiskPercent * 0.01));
            ChartObjects.DrawText("TR1", cR1.PadRight(5), StaticPosition.TopRight, Colors.Goldenrod);

            string cR2 = "\n\n\n\n\n-----------------------------\nSniper Risk: " + S2_RiskPercent + "%";
            cR2 += "\n" + String.Format("{0:C}", Account.Balance * (S2_RiskPercent * 0.01));
            ChartObjects.DrawText("TR2", cR2.PadRight(5), StaticPosition.TopRight, Colors.Orange);

            string cR3 = "\n\n\n\n\n\n\n\n-----------------------------\nInvest Risk: " + S3_RiskPercent + "%";
            cR3 += "\n" + String.Format("{0:C}", Account.Balance * (S3_RiskPercent * 0.01));
            ChartObjects.DrawText("TR3", cR3.PadRight(5), StaticPosition.TopRight);

        }
    }
}
