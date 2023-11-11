﻿using System;
using System.Linq;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ProgressBar Styles", Description: "Shows the ProgressBar Styles.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Progress")]
	[ScenarioCategory ("Threading")]

	public class ProgressBarStyles : Scenario {
		private Timer _fractionTimer;
		private Timer _pulseTimer;
		private const uint _timerTick = 100;

		public override void Setup ()
		{
			const float fractionStep = 0.01F;
			const int pbWidth = 20;

			var pbFormatEnum = Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();

			var rbPBFormat = new RadioGroup (pbFormatEnum.Select (e => e.ToString ()).ToArray ()) {
				X = Pos.Center (),
				Y = 1
			};
			Win.Add (rbPBFormat);

			var ckbBidirectional = new CheckBox ("BidirectionalMarquee", true) {
				X = Pos.Center (),
				Y = Pos.Bottom (rbPBFormat) + 1
			};
			Win.Add (ckbBidirectional);

			var leftContainer = new View () {
				Y = Pos.Bottom (ckbBidirectional) + 1,
				Width = Dim.Percent (50),
				Height = Dim.Fill ()
			};

			var label = new Label ("Blocks") {
				X = Pos.Center ()
			};
			leftContainer.Add (label);

			var blocksPB = new ProgressBar () {
				X = Pos.Center (),
				Y = Pos.Y (label) + 1,
				Width = pbWidth
			};
			leftContainer.Add (blocksPB);

			label = new Label ("Continuous") {
				X = Pos.Center (),
				Y = Pos.Bottom (blocksPB) + 1
			};
			leftContainer.Add (label);

			var continuousPB = new ProgressBar () {
				X = Pos.Center (),
				Y = Pos.Y (label) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.Continuous
			};
			leftContainer.Add (continuousPB);

			var button = new Button ("Start timer") {
				X = Pos.Center (),
				Y = Pos.Bottom (continuousPB) + 1
			};
			button.Clicked += (s,e) => {
				if (_fractionTimer == null) {
					button.Enabled = false;
					blocksPB.Fraction = 0;
					continuousPB.Fraction = 0;
					float fractionSum = 0;
					_fractionTimer = new Timer ((_) => {
						fractionSum += fractionStep;
						blocksPB.Fraction = fractionSum;
						continuousPB.Fraction = fractionSum;
						if (fractionSum > 1) {
							_fractionTimer.Dispose ();
							_fractionTimer = null;
							button.Enabled = true;
						}
						Application.Wakeup ();
					}, null, 0, _timerTick);
				}
			};
			leftContainer.Add (button);
			Win.Add (leftContainer);

			var rightContainer = new View () {
				X = Pos.Right (leftContainer),
				Y = Pos.Bottom (ckbBidirectional) + 1,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			label = new Label ("Marquee Blocks") {
				X = Pos.Center ()
			};
			rightContainer.Add (label);

			var marqueesBlocksPB = new ProgressBar () {
				X = Pos.Center (),
				Y = Pos.Y (label) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
			};
			rightContainer.Add (marqueesBlocksPB);

			label = new Label ("Marquee Continuous") {
				X = Pos.Center (),
				Y = Pos.Bottom (marqueesBlocksPB) + 1
			};
			rightContainer.Add (label);

			var marqueesContinuousPB = new ProgressBar () {
				X = Pos.Center (),
				Y = Pos.Y (label) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.MarqueeContinuous
			};
			rightContainer.Add (marqueesContinuousPB);
			Win.Add (rightContainer);

			rbPBFormat.SelectedItemChanged += (s,e) => {
				blocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				continuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				marqueesBlocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				marqueesContinuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
			};

			ckbBidirectional.Toggled += (s,e) => {
				ckbBidirectional.Checked = marqueesBlocksPB.BidirectionalMarquee = marqueesContinuousPB.BidirectionalMarquee = (bool)!e.OldValue;
			};

			_pulseTimer = new Timer ((_) => {
				marqueesBlocksPB.Text = marqueesContinuousPB.Text = DateTime.Now.TimeOfDay.ToString ();
				marqueesBlocksPB.Pulse ();
				marqueesContinuousPB.Pulse ();
				Application.Wakeup ();
			}, null, 0, 300);

			Application.Top.Unloaded += Top_Unloaded;

			void Top_Unloaded (object sender, EventArgs args)
			{
				if (_fractionTimer != null) {
					_fractionTimer.Dispose ();
					_fractionTimer = null;
				}
				if (_pulseTimer != null) {
					_pulseTimer.Dispose ();
					_pulseTimer = null;
				}
				Application.Top.Unloaded -= Top_Unloaded;
			}
		}
	}
}