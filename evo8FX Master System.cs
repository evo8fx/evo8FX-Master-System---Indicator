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



        protected override void Initialize()
        {
            // Initialize and create nested indicators
            pairID = Symbol.Code;
            debug_PrintValues = false;


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
