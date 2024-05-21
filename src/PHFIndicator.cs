#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.YSF
{
	public class PriceHitFrequencyPHFIndicator : Indicator
	{
		// Declare variables
		private SortedDictionary<double, double> priceRows = new SortedDictionary<double,double> ();
		private List<double> hvnprices;
		private int count;
		private double vpocprice = 0;
		private double dvah = 0;
		private double dval = 0;
		private double zhimax, zlomin;
		private int hitline = 1;
		private int threshold = 100;
		
		private double highestValue = double.MinValue; // Initialize to a very low value
		private	double maxKeyWithHighestValue = double.MinValue;
		private	double minKeyWithHighestValue = double.MaxValue;
		
		private System.Windows.Controls.Button _button;
        private Chart _chartWindow;
        private bool _isToolBarButtonAdded;
		
		protected override void OnStateChange()
		{
			// Handle different states of the indicator
			if (State == State.SetDefaults)
			{
				// Set default properties
				Description									= @"Indicator that calculates hits on high or low prices of each bar within a specified period.";
				Name										= "PriceHitFrequency PHF Indicator";
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				ArePlotsConfigurable						= false;
				
				
				barcounter = 120;
				minhits = 2;
				
				resistanceStroke = new Stroke(Brushes.Orange,DashStyleHelper.Solid, 2, 100);
				supportStroke = new Stroke(Brushes.RoyalBlue,DashStyleHelper.Solid, 2, 100);
				hitlineStroke = new Stroke(Brushes.Red,DashStyleHelper.Solid, 2, 100);
			}
			else if (State == State.Configure)
			{
   				ClearOutputWindow();
				Print("Configuring indicator."); // Debug statement
				
				// Configure indicator
				Calculate		= Calculate.OnBarClose;
				hvnprices		= new List<double>();
			}
			else if (State == State.DataLoaded)
			{
				Print("Data loaded."); // Debug statement
				
				// Add a button to the toolbar when the data is loaded
				if (ChartControl != null && !_isToolBarButtonAdded)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        AddButtonToToolbar();
                    }));
                }
			}
			else if (State == State.Historical)
		 	{
				Print("Processing historical data."); // Debug statement
				
				// Set Z order for historical data
				SetZOrder(-1);
			}
			else if (State == State.Terminated)
            {
				Print("Terminating."); // Debug statement
				
				// Dispose toolbar button when the indicator is terminated
				if (_chartWindow != null)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        DisposeToolBar();
                    }));
                }
            }
		}

		// Override display name property
		public override string DisplayName
		{	 
			get { if  (State == State.SetDefaults) return Name; else  return "";  }
		}
		
		// Dispose the toolbar button
		private void DisposeToolBar()
        {
			Print("Disposing toolbar."); // Debug statement
			
            if (_button != null)
            {
                _button.Click -= OnButtonClick;
                _chartWindow.MainMenu.Remove(_button);
            }
        }
		
		// Add button to the toolbar
		private void AddButtonToToolbar()
        {
			Print("Adding button to toolbar."); // Debug statement
			
            _chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;

            if (_chartWindow == null)
            {
                Print("chartWindow == null");
                return;
            }
			
			 // Create button style
			 Style btnStyle = new Style();
		     btnStyle.TargetType = typeof(System.Windows.Controls.Button);
				
		     btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
		     btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new System.Windows.Media.FontFamily("Lato")));
		     btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Regular));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.MarginProperty, new Thickness(2, 0, 2, 0)));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.PaddingProperty, new Thickness(2, 2, 2, 2)));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, IsVisible ? Brushes.LimeGreen : Brushes.Red));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.BorderBrushProperty, IsVisible ? Brushes.LimeGreen : Brushes.Red));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.Black));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.BorderThicknessProperty, new Thickness(1, 1, 1, 1)));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.IsEnabledProperty, true));
			 btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center));
			
			if (true)
			{
				// Create and add button to the toolbar
				_button = new System.Windows.Controls.Button();
				
				_button.Content = Name;
				_button.Style = btnStyle;
				_chartWindow.MainMenu.Add(_button);
				_button.Visibility = Visibility.Visible;
				_button.Click += OnButtonClick;
				
				_isToolBarButtonAdded = true;
			}
        }

		// Handle button click event
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
			Print("Button clicked."); // Debug statement
			if (IsVisible)
			{
				// Hide indicator
				IsVisible = false;
				RemoveDrawObjects();
				_button.Foreground = Brushes.Red;
				_button.BorderBrush = Brushes.Red;
			}
			else
			{
				// Show indicator
				IsVisible = true;
				ForceRefresh();
				_button.Foreground = Brushes.LimeGreen;
				_button.BorderBrush = Brushes.LimeGreen;
			}
			Print("Visibility = "+IsVisible.ToString()); // Debug statement
        }

		// Create a button with specified properties
        private System.Windows.Controls.Button CreateButton(string content, string name, System.Windows.Media.Brush foreground,
            System.Windows.Media.Brush background)
        {
            return new System.Windows.Controls.Button
            {
                Name = name,
                Content = content,
                Foreground = foreground,
                Background = background,
            };
        }
		
		protected override void OnBarUpdate()
		{
			Print("OnBarUpdate entered."); // Debug statement
			
			// Exit if there are not enough bars
			if (CurrentBar < barcounter + 2)
				return;
			
			// Clear previous price data
			priceRows.Clear();
			Print("Price rows cleared."); // Debug statement
			
			// Add high and low prices to the priceRows dictionary
			if (!priceRows.ContainsKey(High[0]))
			{priceRows[High[0]] = 1;}
			else
			{priceRows[High[0]] += 1;}
			
			if (!priceRows.ContainsKey(Low[0]))
			{priceRows[Low[0]] = 1;}
			else
			{priceRows[Low[0]] += 1;}
				
			zhimax = High[0];
			zlomin = Low[0];
			
			// Loop through bars and update priceRows
			for (int i = 0; i < barcounter; i++)
			{
				if (!priceRows.ContainsKey(High[i]))
				{priceRows[High[i]] = 1;}
				else
				{priceRows[High[i]] += 1;}
				
				if (!priceRows.ContainsKey(Low[i]))
				{priceRows[Low[i]] = 1;}
				else
				{priceRows[Low[i]] += 1;}
				
				zhimax = Math.Max(High[i],zhimax);
				zlomin = Math.Min(Low[i],zhimax);
			}
			
			// Sort dictionary by value
			var sortedDict = (from entry in priceRows 
				orderby entry.Value descending 
				select entry).Take(priceRows.Count)
				.ToDictionary(pair => pair.Key, pair => pair.Value);
			
			Print("Price rows sorted."); // Debug statement
			
			maxKeyWithHighestValue = double.MinValue;
			minKeyWithHighestValue = double.MaxValue;
			
			// Identify highest and lowest keys with the highest values
			foreach (KeyValuePair<double,double> kv in sortedDict)
			{
				if (kv.Value > minhits)
				{
					highestValue = Math.Max(highestValue, kv.Value);
					if (kv.Value >= highestValue*(1-(threshold/100)))
				    {
				        if (kv.Key > maxKeyWithHighestValue)
				        {
				            maxKeyWithHighestValue = Math.Max(maxKeyWithHighestValue, kv.Key);
							{Print("maxKeyWithHighestValue : key = "+kv.Key.ToString() + " val = "+kv.Value.ToString());}
				        }
				        
				        if (kv.Key < minKeyWithHighestValue)
				        {
				            minKeyWithHighestValue = Math.Min(minKeyWithHighestValue, kv.Key);
							{Print("minKeyWithHighestValue : key = "+kv.Key.ToString() + " val = "+kv.Value.ToString());}
				        }
				    }
				}
			}
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			Print("OnRender entered."); // Debug statement
			
			// Set antialias mode
			SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			
			int x1 = 0;
			int x2 = 0;
			int y1 = 0;
			int y2 = 0;
						
			SharpDX.Vector2 pOne = new SharpDX.Vector2();
			SharpDX.Vector2 pTwo = new SharpDX.Vector2();
			
			// Set line color for drawing
			SharpDX.Direct2D1.Brush lineColor = (hitlineStroke.Brush).ToDxBrush(RenderTarget);
			lineColor.Opacity = (float)hitlineStroke.Opacity/100;
						
			// Sort dictionary and draw lines
			var sortedDict2 = (from entry in priceRows 
				orderby entry.Value descending 
				select entry).Take(hitline)
				.ToDictionary(pair => pair.Key, pair => pair.Value);
			
			foreach (KeyValuePair<double,double> kv in sortedDict2)
			{
				x1 = (int)(ChartControl.GetXByBarIndex(ChartBars, BarsArray[0].Count - barcounter-1)); 
				x2 = chartControl.CanvasRight;
			
				y1 = chartScale.GetYByValue(kv.Key);
				y2 = chartScale.GetYByValue(kv.Key);
				
				pOne.X = (float)x1;
				pOne.Y = (float)y1;
				pTwo.X = (float)x2;
				pTwo.Y = (float)y1;
				
				if (kv.Value > minhits)
				{
					// Draw MAX Hit line
					if (kv.Key != maxKeyWithHighestValue && kv.Key != minKeyWithHighestValue)
						{RenderTarget.DrawLine(pOne, pTwo, lineColor, hitlineStroke.Width, hitlineStroke.StrokeStyle);}
				}
			}

			// Draw resistance line
			lineColor = (resistanceStroke.Brush).ToDxBrush(RenderTarget);
			lineColor.Opacity = (float)resistanceStroke.Opacity/100;
			
			y1 = chartScale.GetYByValue(maxKeyWithHighestValue);
			y2 = chartScale.GetYByValue(maxKeyWithHighestValue);
			
			pOne.X = (float)x1;
			pOne.Y = (float)y1;
			pTwo.X = (float)x2;
			pTwo.Y = (float)y1;
			RenderTarget.DrawLine(pOne, pTwo, lineColor, resistanceStroke.Width, resistanceStroke.StrokeStyle);
			
			// Draw support line
			lineColor = (supportStroke.Brush).ToDxBrush(RenderTarget);
			lineColor.Opacity = (float)supportStroke.Opacity/100;
			
			y1 = chartScale.GetYByValue(minKeyWithHighestValue);
			y2 = chartScale.GetYByValue(minKeyWithHighestValue);
			
			pOne.X = (float)x1;
			pOne.Y = (float)y1;
			pTwo.X = (float)x2;
			pTwo.Y = (float)y1;
			RenderTarget.DrawLine(pOne, pTwo, lineColor, supportStroke.Width, supportStroke.StrokeStyle);
			
			// Restore previous antialias mode
			lineColor.Dispose();
			RenderTarget.AntialiasMode = oldAntialiasMode;
			
			base.OnRender(chartControl, chartScale);
		}
		
		// Define properties with UI attributes
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Lookback Period", Description="", GroupName = "Filters", Order = 20)]
		public int barcounter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Minimum Hits", Description="", GroupName = "Filters", Order = 30)]
		public int minhits
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Resistance", Description="High Zone", Order=40, GroupName="Filters")]
		public Stroke resistanceStroke { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Support", Description="Low Zone", Order=50, GroupName="Filters")]
		public Stroke supportStroke { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="MAX Hit", Description="Hit Zone", Order=60, GroupName="Filters")]
		public Stroke hitlineStroke { get; set; }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private YSF.PriceHitFrequencyPHFIndicator[] cachePriceHitFrequencyPHFIndicator;
		public YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			return PriceHitFrequencyPHFIndicator(Input, barcounter, minhits, resistanceStroke, supportStroke, hitlineStroke);
		}

		public YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(ISeries<double> input, int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			if (cachePriceHitFrequencyPHFIndicator != null)
				for (int idx = 0; idx < cachePriceHitFrequencyPHFIndicator.Length; idx++)
					if (cachePriceHitFrequencyPHFIndicator[idx] != null && cachePriceHitFrequencyPHFIndicator[idx].barcounter == barcounter && cachePriceHitFrequencyPHFIndicator[idx].minhits == minhits && cachePriceHitFrequencyPHFIndicator[idx].resistanceStroke == resistanceStroke && cachePriceHitFrequencyPHFIndicator[idx].supportStroke == supportStroke && cachePriceHitFrequencyPHFIndicator[idx].hitlineStroke == hitlineStroke && cachePriceHitFrequencyPHFIndicator[idx].EqualsInput(input))
						return cachePriceHitFrequencyPHFIndicator[idx];
			return CacheIndicator<YSF.PriceHitFrequencyPHFIndicator>(new YSF.PriceHitFrequencyPHFIndicator(){ barcounter = barcounter, minhits = minhits, resistanceStroke = resistanceStroke, supportStroke = supportStroke, hitlineStroke = hitlineStroke }, input, ref cachePriceHitFrequencyPHFIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			return indicator.PriceHitFrequencyPHFIndicator(Input, barcounter, minhits, resistanceStroke, supportStroke, hitlineStroke);
		}

		public Indicators.YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(ISeries<double> input , int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			return indicator.PriceHitFrequencyPHFIndicator(input, barcounter, minhits, resistanceStroke, supportStroke, hitlineStroke);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			return indicator.PriceHitFrequencyPHFIndicator(Input, barcounter, minhits, resistanceStroke, supportStroke, hitlineStroke);
		}

		public Indicators.YSF.PriceHitFrequencyPHFIndicator PriceHitFrequencyPHFIndicator(ISeries<double> input , int barcounter, int minhits, Stroke resistanceStroke, Stroke supportStroke, Stroke hitlineStroke)
		{
			return indicator.PriceHitFrequencyPHFIndicator(input, barcounter, minhits, resistanceStroke, supportStroke, hitlineStroke);
		}
	}
}

#endregion
