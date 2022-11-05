﻿using System;

namespace Terminal.Gui {
	/// <summary>
	/// A container for single <see cref="Child"/> that will allow to drawn <see cref="Border"/> in
	///  two ways. If <see cref="UsePanelFrame"/> the borders and the child will be accommodated in the available
	///  panel size, otherwise the panel will be resized based on the child and borders thickness sizes.
	/// </summary>
	public class PanelView : View {
		ChildContentView childContentView;

		private class ChildContentView : View { }

		private class SavedPosDim {
			public Pos X;
			public Pos Y;
			public Dim Width;
			public Dim Height;
		}

		private SavedPosDim savedPanel;
		private SavedPosDim savedChild;

		private View child;
		private bool usePanelFrame;
		private bool useEffect3DAnimation;

		/// <summary>
		/// Initializes a panel with a null child.
		/// </summary>
		public PanelView () : this (null) { }

		/// <summary>
		/// Initializes a panel with a valid child.
		/// </summary>
		/// <param name="child"></param>
		public PanelView (View child)
		{
			childContentView = new ChildContentView ();
			base.Add (childContentView);
			CanFocus = false;
			Child = child;
			if (child != null) {
				Visible = child.Visible;
			}
		}

		/// <summary>
		///   Clicked <see cref="Action"/>, raised when the user clicks the primary mouse button within the Bounds of this <see cref="View"/>
		///   or if the user presses the action key while this view is focused. (TODO: IsDefault)
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event Action Clicked;

		/// <summary>
		/// Gets or sets if the button will have a <see cref="Border.effect3D"/> with animation.
		/// </summary>
		public bool UseEffect3DAnimation {
			get => useEffect3DAnimation;
			set {
				useEffect3DAnimation = value;
				if (value && Child != null) {
					Child.MouseClick += Child_MouseClick;
					Child.KeyPress += Child_KeyPress;
				} else if (!value && Child != null) {
					Child.MouseClick -= Child_MouseClick;
					Child.KeyPress -= Child_KeyPress;
				}
			}
		}

		private void Child_KeyPress (KeyEventEventArgs obj)
		{
			if (obj.KeyEvent.Key == Key.Enter || obj.KeyEvent.Key == Key.Space) {
				if (!HasFocus) {
					SetFocus ();
				}
				OnClicked ();
				obj.Handled = true;
			}
		}

		private void Child_MouseClick (MouseEventArgs obj)
		{
			if (obj.MouseEvent.Flags == MouseFlags.Button1Pressed) {
				if (!HasFocus && SuperView != null) {
					if (!SuperView.HasFocus) {
						SuperView.SetFocus ();
					}
					SetFocus ();
					SetNeedsDisplay ();
				}

				if (UseEffect3DAnimation) {
					Border.Effect3D = false;
					SetNeedsDisplay ();
					Application.GrabMouse (Child);
				}
				obj.Handled = true;
			} else if (obj.MouseEvent.Flags == MouseFlags.Button1Released) {
				if (CanFocus && Enabled) {
					if (UseEffect3DAnimation) {
						Border.Effect3D = true;
						Application.MainLoop.Invoke (() => SetNeedsDisplay ());
					}
					if (obj.MouseEvent.View == this) {
						OnClicked ();
					}
					Application.UngrabMouse ();
				}
				obj.Handled = true;
			}
		}

		/// <summary>
		/// Gets or sets if the panel size will used, otherwise the child size.
		/// </summary>
		public bool UsePanelFrame {
			get => usePanelFrame;
			set {
				usePanelFrame = value;
				AdjustContainer ();
			}
		}

		/// <summary>
		/// The child that will use this panel.
		/// </summary>
		public View Child {
			get => child;
			set {
				if (child != null && value == null) {
					childContentView.Remove (child);
					child = value;
					return;
				}
				child = value;
				savedChild = new SavedPosDim () {
					X = child?.X ?? child?.Frame.X,
					Y = child?.Y ?? child?.Frame.Y,
					Width = child?.Width ?? child?.Frame.Width,
					Height = child?.Height ?? child?.Frame.Height
				};
				if (child == null) {
					Visible = false;
					return;
				}
				if (child?.Border != null) {
					Border = child.Border;
				} else {
					if (Border == null) {
						Border = new Border ();
					}
					Child.Border = Border;
				}
				Border.Child = childContentView;
				if (!child.IsInitialized) {
					child.Initialized += Child_Initialized;
				}
				childContentView.Add (Child);
			}
		}

		/// <inheritdoc />
		public override Border Border {
			get => base.Border;
			set {
				if (base.Border?.Child != null && value.Child == null) {
					value.Child = base.Border.Child;
				}
				base.Border = value;
				if (value == null) {
					return;
				}
				Border.BorderChanged += Border_BorderChanged;
				if (Child != null && (Child?.Border == null || Child?.Border != value)) {
					if (Child?.Border == null) {
						Child.Border = new Border ();
					}
					Child.Border = Border;
					Child.Border.BorderChanged += Border_BorderChanged;
				}
				AdjustContainer ();
			}
		}

		private void Child_Initialized (object sender, EventArgs e)
		{
			savedPanel = new SavedPosDim () {
				X = X,
				Y = Y,
				Width = Width,
				Height = Height
			};
			AdjustContainer ();
			Child.Initialized -= Child_Initialized;
		}

		private void Border_BorderChanged (Border obj)
		{
			AdjustContainer ();
		}

		private void AdjustContainer ()
		{
			if (Child?.IsInitialized == true) {
				if (Child?.Border != null && Child.Border != Border) {
					Border = Child.Border;
				}
				var borderLength = Child.Border.DrawMarginFrame ? 1 : 0;
				var sumPadding = Child.Border.GetSumThickness ();
				var effect3DOffset = Child.Border.Effect3D ? Child.Border.Effect3DOffset : new Point ();
				if (!UsePanelFrame) {
					X = savedPanel.X;
					childContentView.X = borderLength + sumPadding.Left;
					Y = savedPanel.Y;
					childContentView.Y = borderLength + sumPadding.Top;
					if (savedChild.Width is Dim.DimFill) {
						var margin = -savedChild.Width.Anchor (0);
						Width = Dim.Fill (margin);
						childContentView.Width = Dim.Fill (margin + borderLength + sumPadding.Right);
					} else {
						var savedLayout = LayoutStyle;
						LayoutStyle = LayoutStyle.Absolute;
						Width = savedChild.X.Anchor (0) + savedChild.Width + (2 * borderLength) + sumPadding.Right + sumPadding.Left;
						LayoutStyle = savedLayout;
						childContentView.Width = Dim.Fill (borderLength + sumPadding.Right);
					}
					if (savedChild.Height is Dim.DimFill) {
						var margin = -savedChild.Height.Anchor (0);
						Height = Dim.Fill (margin);
						childContentView.Height = Dim.Fill (margin + borderLength + sumPadding.Bottom);
					} else {
						var savedLayout = LayoutStyle;
						LayoutStyle = LayoutStyle.Absolute;
						Height = savedChild.Y.Anchor (0) + savedChild.Height + (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
						LayoutStyle = savedLayout;
						childContentView.Height = Dim.Fill (borderLength + sumPadding.Bottom);
					}
				} else {
					X = savedPanel.X - (effect3DOffset.X < 0 ? effect3DOffset.X : 0);
					childContentView.X = borderLength + sumPadding.Left;
					Y = savedPanel.Y - (effect3DOffset.Y < 0 ? effect3DOffset.Y : 0);
					childContentView.Y = borderLength + sumPadding.Top;
					Width = savedPanel.Width;
					Height = savedPanel.Height;
					if (Width is Dim.DimFill) {
						var margin = -savedPanel.Width.Anchor (0) +
							(effect3DOffset.X > 0 ? effect3DOffset.X : 0);
						Width = Dim.Fill (margin);
						childContentView.Width = Dim.Fill (margin + borderLength + sumPadding.Right +
							(effect3DOffset.X > 0 ? effect3DOffset.X : 0));
					} else {
						childContentView.Width = Dim.Fill (borderLength + sumPadding.Right);
					}
					if (Height is Dim.DimFill) {
						var margin = -savedPanel.Height.Anchor (0) +
							(effect3DOffset.Y > 0 ? effect3DOffset.Y : 0);
						Height = Dim.Fill (margin);
						childContentView.Height = Dim.Fill (margin + borderLength + sumPadding.Bottom +
							(effect3DOffset.Y > 0 ? effect3DOffset.Y : 0));
					} else {
						childContentView.Height = Dim.Fill (borderLength + sumPadding.Bottom);
					}
				}
				Visible = Child.Visible;
			} else {
				Visible = false;
			}
		}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			if (Child != null) {
				Child = null;
			}
			Child = view;
		}

		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == childContentView) {
				base.Remove (view);
				return;
			}
			childContentView.Remove (view);
			if (Child != null) {
				Child = null;
			}
		}

		/// <inheritdoc/>
		public override void RemoveAll ()
		{
			if (Child != null) {
				Child = null;
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (Child.GetNormalColor ());
				Clear ();
				Child.Border.DrawContent (Border.Child);
			}
			var savedClip = childContentView.ClipToBounds ();
			childContentView.Redraw (childContentView.Bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="Clicked"/> event.
		/// </summary>
		public virtual void OnClicked ()
		{
			if (UseEffect3DAnimation) {
				Border.Effect3D = false;
				var x = X;
				if (Border.Effect3DOffset.X > 0) {
					X += 1;
				} else {
					X -= 1;
				}
				SetNeedsDisplay ();
				Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), (m) => {
					Border.Effect3D = true;
					Application.MainLoop.Invoke (() => {
						X = x;
						SetNeedsDisplay ();
					});
					Clicked?.Invoke ();
					return false;
				});
			}
		}
	}
}