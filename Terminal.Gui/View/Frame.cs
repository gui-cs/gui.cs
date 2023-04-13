﻿using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace Terminal.Gui {

	// TODO: v2 - Missing 3D effect - 3D effects will be drawn by a mechanism separate from Frames
	// TODO: v2 - If a Frame has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Frame
	// QUESTION: How does a user navigate out of a Frame to another Frame, or back into the Parent's SubViews?


	/// <summary>
	/// Frames are a special form of <see cref="View"/> that act as adornments; they appear outside of the <see cref="View.Bounds"/>
	/// enabling borders, menus, etc... 
	/// </summary>
	public class Frame : View {
		private Thickness _thickness = Thickness.Empty;

		internal override void CreateFrames () { /* Do nothing - Frames do not have Frames */ }
		internal override void LayoutFrames () { /* Do nothing - Frames do not have Frames */ }

		/// <summary>
		/// The Parent of this Frame (the View this Frame surrounds).
		/// </summary>
		public View Parent { get; set; }

		/// <summary>
		/// Frames cannot be used as sub-views, so this method always throws an <see cref="InvalidOperationException"/>.
		/// TODO: Are we sure?
		/// </summary>
		public override View SuperView {
			get {
				return null;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		/// <inheritdoc/>
		public override void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
		{
			// Frames are *Children* of a View, not SubViews. Thus View.ViewToScreen will not work.
			// To get the screen-relative coordinates of a Frame, we need to know who
			// the Parent is
			var parentFrame = Parent?.Frame ?? Frame;
			rrow = row + parentFrame.Y;
			rcol = col + parentFrame.X;

			// We now have rcol/rrow in coordinates relative to our View's SuperView. If our View's SuperView has
			// a SuperView, keep going...
			Parent?.SuperView?.ViewToScreen (rcol, rrow, out rcol, out rrow, clipped);
		}

		/// <summary>
		/// Frames only render to their Parent or Parent's SuperView's LineCanvas,
		/// so this always throws an <see cref="InvalidOperationException"/>.
		/// </summary>
		public override LineCanvas LineCanvas {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		/// <summary>
		/// Does nothing for Frame
		/// </summary>
		/// <returns></returns>
		public override bool OnDrawFrames () => false;

		/// <summary>
		/// Frames only render to their Parent or Parent's SuperView's LineCanvas,
		/// so this always throws an <see cref="InvalidOperationException"/>.
		/// </summary>
		public override bool SuperViewRendersLineCanvas {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="clipRect"></param>
		public virtual void OnDrawSubViews (Rect clipRect)
		{
			// TODO: Enable subviews of Frames (adornments).
			//	if (Subviews == null) {
			//		return;
			//	}

			//	foreach (var view in Subviews) {
			//		// BUGBUG: v2 - shouldn't this be !view.LayoutNeeded? Why draw if layout is going to happen and we'll just draw again?
			//		if (view.LayoutNeeded) {
			//			view.LayoutSubviews ();
			//		}
			//		if ((view.Visible && !view.NeedDisplay.IsEmpty && view.Frame.Width > 0 && view.Frame.Height > 0) || view.ChildNeedsDisplay) {
			//			view.Redraw (view.Bounds);

			//			view.NeedDisplay = Rect.Empty;
			//			// BUGBUG - v2 why does this need to be set to false?
			//			// Shouldn't it be set when the subviews draw?
			//			view.ChildNeedsDisplay = false;
			//		}
			//	}

		}

		/// <summary>
		/// Redraws the Frames that comprise the <see cref="Frame"/>.
		/// </summary>
		/// <param name="bounds"></param>
		public override void Redraw (Rect bounds)
		{
			if (Thickness == Thickness.Empty) return;

			if (ColorScheme != null) {
				Driver.SetAttribute (ColorScheme.Normal);
			} else {
				Driver.SetAttribute (Parent.GetNormalColor ());
			}

			//Driver.SetAttribute (Colors.Error.Normal);

			var prevClip = SetClip (Frame);

			var screenBounds = ViewToScreen (Frame);
			// TODO: Figure out if we should be clearing the Bounds here, like this
			//Thickness.Draw (screenBounds, (string)(Data != null ? Data : string.Empty));

			//OnDrawSubviews (bounds); 

			// TODO: v2 - this will eventually be two controls: "BorderView" and "Label" (for the title)

			var borderBounds = new Rect (
				screenBounds.X + Thickness.Left - 1,
				screenBounds.Y + Thickness.Top - 1,
				screenBounds.Width - Thickness.Horizontal + 2,
				screenBounds.Height - Thickness.Vertical + 2);

			if (Id == "Border" && Thickness.Top > 0 && Frame.Width > 1 && !ustring.IsNullOrEmpty (Parent?.Title)) {
				var prevAttr = Driver.GetAttribute ();
				Driver.SetAttribute (Parent.HasFocus ? Parent.GetHotNormalColor () : Parent.GetNormalColor ());
				DrawTitle (borderBounds, Parent?.Title);
				Driver.SetAttribute (prevAttr);
			}

			if (Id == "Border" && BorderStyle != LineStyle.None) {
				// If View's parent has a SuperView, the border will be rendered with all our View's peers
				// If not, then it will be rendered just for our View.
				LineCanvas lc = Parent?.LineCanvas;
				//if (Parent?.SuperViewRendersLineCanvas == true) {
				//	lc = Parent?.SuperView?.LineCanvas;
				//}

				var drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height > 1;
				var drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
				var drawBottom = Thickness.Bottom > 0 && Frame.Width > 1;
				var drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

				if (drawTop) {
					// ╔╡Title╞═════╗
					// ╔╡╞═════╗
					if (Frame.Width < 4 || ustring.IsNullOrEmpty (Parent?.Title)) {
						// ╔╡╞╗ should be ╔══╗
						lc.AddLine (borderBounds.Location, Frame.Width, Orientation.Horizontal, BorderStyle);
					} else {
						var titleWidth = Math.Min (Parent.Title.ConsoleWidth, Frame.Width - 4);

						if (Thickness.Top > 1) {
							lc.AddLine (new Point (borderBounds.X + 1, borderBounds.Location.Y - 1), (titleWidth + 2), Orientation.Horizontal, BorderStyle);
						}

						// ╔╡Title╞═════╗
						// Add a short horiz line for ╔╡
						lc.AddLine (borderBounds.Location, 2, Orientation.Horizontal, BorderStyle);
						// Add a zero length vert line for ╔╡
						lc.AddLine (new Point (borderBounds.X + 1, borderBounds.Location.Y), Thickness.Top > 1 ? -2 : 0, Orientation.Vertical, LineStyle.Single);
						// Add a zero length line for ╞
						lc.AddLine (new Point (borderBounds.X + 1 + (titleWidth + 1), borderBounds.Location.Y), Thickness.Top > 1 ? -2 : 0, Orientation.Vertical, LineStyle.Single);
						// Add the right hand line for ╞═════╗
						lc.AddLine (new Point (borderBounds.X + 1 + (titleWidth + 1), borderBounds.Location.Y), Frame.Width - (titleWidth + 2), Orientation.Horizontal, BorderStyle);
					}
				}
				if (drawLeft) {
					lc.AddLine (borderBounds.Location, borderBounds.Height, Orientation.Vertical, BorderStyle);
				}
				if (drawBottom) {
					lc.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1), borderBounds.Width, Orientation.Horizontal, BorderStyle);
				}
				if (drawRight) {
					lc.AddLine (new Point (borderBounds.X + borderBounds.Width - 1, borderBounds.Y), borderBounds.Height, Orientation.Vertical, BorderStyle);
				}

				//if (Parent?.SuperViewRendersLineCanvas == false) {
					//foreach (var p in lc.GetMap ()) {
					//	Driver.Move (p.Key.X, p.Key.Y);
					//	Driver.AddRune (p.Value);
					//}
					//lc.Clear ();
				//}

				// TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
				if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler) {
					// Top
					var hruler = new Ruler () { Length = screenBounds.Width, Orientation = Orientation.Horizontal };
					if (drawTop) {
						hruler.Draw (new Point (screenBounds.X, screenBounds.Y));
					}

					// Redraw title 
					if (drawTop && Id == "Border" && !ustring.IsNullOrEmpty (Parent?.Title)) {
						var prevAttr = Driver.GetAttribute ();
						Driver.SetAttribute (Parent.HasFocus ? Parent.GetHotNormalColor () : Parent.GetNormalColor ());
						DrawTitle (screenBounds, Parent?.Title);
						Driver.SetAttribute (prevAttr);
					}

					//Left
					var vruler = new Ruler () { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };
					if (drawLeft) {
						vruler.Draw (new Point (screenBounds.X, screenBounds.Y + 1), 1);
					}

					// Bottom
					if (drawBottom) {
						hruler.Draw (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
					}

					// Right
					if (drawRight) {
						vruler.Draw (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
					}

				}
			}


			Driver.Clip = prevClip;
		}

		// TODO: v2 - Frame.BorderStyle is temporary - Eventually the border will be drawn by a "BorderView" that is a subview of the Frame.
		/// <summary>
		/// 
		/// </summary>
		public new LineStyle BorderStyle { get; set; } = LineStyle.None;

		/// <summary>
		/// Defines the rectangle that the <see cref="Frame"/> will use to draw its content. 
		/// </summary>
		public Thickness Thickness {
			get { return _thickness; }
			set {
				var prev = _thickness;
				_thickness = value;
				if (prev != _thickness) {

					Parent?.LayoutFrames ();
					OnThicknessChanged ();
				}

			}
		}

		/// <summary>
		/// Called whenever the <see cref="Thickness"/> property changes.
		/// </summary>
		public virtual void OnThicknessChanged ()
		{
			ThicknessChanged?.Invoke (this, new ThicknessEventArgs () { Thickness = Thickness });
		}

		/// <summary>
		/// Fired whenever the <see cref="Thickness"/> property changes.
		/// </summary>
		public event EventHandler<ThicknessEventArgs> ThicknessChanged;

		/// <summary>
		/// Gets the rectangle that describes the inner area of the frame. The Location is always (0,0).
		/// </summary>
		public override Rect Bounds {
			get {
				return Thickness?.GetInside (new Rect (Point.Empty, Frame.Size)) ?? new Rect (Point.Empty, Frame.Size);
			}
			set {
				throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
			}
		}

		/// <summary>
		/// Draws the title for a Window-style view.
		/// </summary>
		/// <param name="region">Screen relative region where the title will be drawn.</param>
		/// <param name="title">The title.</param>
		public void DrawTitle (Rect region, ustring title)
		{
			var width = region.Width;
			if (!ustring.IsNullOrEmpty (title) && width > 2) {
				Driver.Move (region.X + 2, region.Y);
				//Driver.AddRune (' ');
				var str = title.Sum (r => Math.Max (Rune.ColumnWidth (r), 1)) >= width
					? TextFormatter.Format (title, width - 2, false, false) [0] : title;
				Driver.AddStr (str);
			}
		}

		/// <summary>
		/// Draws a frame in the current view, clipped by the boundary of this view
		/// </summary>
		/// <param name="region">View-relative region for the frame to be drawn.</param>
		/// <param name="clear">If set to <see langword="true"/> it clear the region.</param>
		[ObsoleteAttribute ("This method is obsolete in v2. Use use LineCanvas or Frame instead.", false)]
		public void DrawFrame (Rect region, bool clear)
		{
			var savedClip = ClipToBounds ();
			var screenBounds = ViewToScreen (region);

			if (clear) {
				Driver.FillRect (region);
			}

			var lc = new LineCanvas ();
			var drawTop = region.Width > 1 && region.Height > 1;
			var drawLeft = region.Width > 1 && region.Height > 1;
			var drawBottom = region.Width > 1 && region.Height > 1;
			var drawRight = region.Width > 1 && region.Height > 1;

			if (drawTop) {
				lc.AddLine (screenBounds.Location, screenBounds.Width, Orientation.Horizontal, BorderStyle);
			}
			if (drawLeft) {
				lc.AddLine (screenBounds.Location, screenBounds.Height, Orientation.Vertical, BorderStyle);
			}
			if (drawBottom) {
				lc.AddLine (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1), screenBounds.Width, Orientation.Horizontal, BorderStyle);
			}
			if (drawRight) {
				lc.AddLine (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y), screenBounds.Height, Orientation.Vertical, BorderStyle);
			}
			foreach (var p in lc.GetMap ()) {
				Driver.Move (p.Key.X, p.Key.Y);
				Driver.AddRune (p.Value);
			}
			lc.Clear ();

			// TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
			if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler) {
				// Top
				var hruler = new Ruler () { Length = screenBounds.Width, Orientation = Orientation.Horizontal };
				if (drawTop) {
					hruler.Draw (new Point (screenBounds.X, screenBounds.Y));
				}

				//Left
				var vruler = new Ruler () { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };
				if (drawLeft) {
					vruler.Draw (new Point (screenBounds.X, screenBounds.Y + 1), 1);
				}

				// Bottom
				if (drawBottom) {
					hruler.Draw (new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
				}

				// Right
				if (drawRight) {
					vruler.Draw (new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
				}
			}

			Driver.Clip = savedClip;
		}

	}
}
