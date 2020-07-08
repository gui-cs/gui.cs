﻿//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimziations
//   - Add rendering limitation to the exposed area
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Determines the LayoutStyle for a view, if Absolute, during LayoutSubviews, the
	/// value from the Frame will be used, if the value is Computed, then the Frame
	/// will be updated from the X, Y Pos objects and the Width and Height Dim objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based on the Frame value.
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on the
		/// X, Y, Width and Height properties and set on the Frame.
		/// </summary>
		Computed
	}

	/// <summary>
	/// View is the base class for all views on the screen and represents a visible element that can render itself and contains zero or more nested views.
	/// </summary>
	/// <remarks>
	/// <para>
	///    The View defines the base functionality for user interface elements in Terminal.Gui.  Views
	///    can contain one or more subviews, can respond to user input and render themselves on the screen.
	/// </para>
	/// <para>
	///    Views supports two layout styles: Absolute or Computed. The choice as to which layout style is used by the View 
	///    is determined when the View is initizlied. To create a View using Absolute layout, call a constructor that takes a
	///    Rect parameter to specify the absolute position and size (the <c>View.<see cref="Frame "/></c>)/. To create a View 
	///    using Computed layout use a constructor that does not take a Rect parametr and set the X, Y, Width and Height 
	///    properties on the view. Both approaches use coordinates that are relative to the container they are being added to. 
	/// </para>
	/// <para>
	///    To switch between Absolute and Computed layout, use the <see cref="LayoutStyle"/> property. 
	/// </para>
	/// <para>
	///    Computed layout is more flexible and supports dynamic console apps where controls adjust layout
	///    as the terminal resizes or other Views change size or position. The X, Y, Width and Height 
	///    properties are Dim and Pos objects that dynamically update the position of a view.
	///    The X and Y properties are of type <see cref="Pos"/>
	///    and you can use either absolute positions, percentages or anchor
	///    points.   The Width and Height properties are of type
	///    <see cref="Dim"/> and can use absolute position,
	///    percentages and anchors.  These are useful as they will take
	///    care of repositioning views when view's frames are resized or
	///    if the terminal size changes.
	/// </para>
	/// <para>
	///    Absolute layout requires specifying coordiantes and sizes of Views explicitly, and the
	///    View will typcialy stay in a fixed position and size. To change the position and size use the
	///    <see cref="Frame"/> property.
	/// </para>
	/// <para>
	///    Subviews (child views) can be added to a View by calling the <see cref="Add(View)"/> method.   
	///    The container of a View can be accessed with the <see cref="SuperView"/> property.
	/// </para>
	/// <para>
	///    To flag a region of the View's <see cref="Bounds"/> to be redrawn call <see cref="SetNeedsDisplay(Rect)"/>. To flag the entire view
	///    for redraw call <see cref="SetNeedsDisplay()"/>.
	/// </para>
	/// <para>
	///    Views have a <see cref="ColorScheme"/> property that defines the default colors that subviews
	///    should use for rendering.   This ensures that the views fit in the context where
	///    they are being used, and allows for themes to be plugged in.   For example, the
	///    default colors for windows and toplevels uses a blue background, while it uses
	///    a white background for dialog boxes and a red background for errors.
	/// </para>
	/// <para>
	///    Subclasses should not rely on <see cref="ColorScheme"/> being
	///    set at construction time. If a <see cref="ColorScheme"/> is not set on a view, the view will inherit the
	///    value from its <see cref="SuperView"/> and the value might only be valid once a view has been
	///    added to a SuperView. 
	/// </para>
	/// <para>
	///    By using  <see cref="ColorScheme"/> applications will work both
	///    in color as well as black and white displays.
	/// </para>
	/// <para>
	///    Views that are focusable should implement the <see cref="PositionCursor"/> to make sure that
	///    the cursor is placed in a location that makes sense.  Unix terminals do not have
	///    a way of hiding the cursor, so it can be distracting to have the cursor left at
	///    the last focused view.   So views should make sure that they place the cursor
	///    in a visually sensible place.
	/// </para>
	/// <para>
	///    The <see cref="LayoutSubviews"/> method is invoked when the size or layout of a view has
	///    changed.   The default processing system will keep the size and dimensions
	///    for views that use the <see cref="LayoutStyle.Absolute"/>, and will recompute the
	///    frames for the vies that use <see cref="LayoutStyle.Computed"/>.
	/// </para>
	/// </remarks>
	public partial class View : Responder, IEnumerable {

		internal enum Direction {
			Forward,
			Backward
		}

		// container == SuperView
		View container = null;
		View focused = null;
		Direction focusDirection;

		TextFormatter textFormatter;

		/// <summary>
		/// Event fired when a subview is being added to this view.
		/// </summary>
		public Action<View> Added;

		/// <summary>
		/// Event fired when a subview is being removed from this view.
		/// </summary>
		public Action<View> Removed;

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public Action<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public Action<FocusEventArgs> Leave;

		/// <summary>
		/// Event fired when the view receives the mouse event for the first time.
		/// </summary>
		public Action<MouseEventArgs> MouseEnter;

		/// <summary>
		/// Event fired when the view receives a mouse event for the last time.
		/// </summary>
		public Action<MouseEventArgs> MouseLeave;

		/// <summary>
		/// Event fired when a mouse event is generated.
		/// </summary>
		public Action<MouseEventArgs> MouseClick;

		/// <summary>
		/// Gets or sets the HotKey defined for this view. A user pressing HotKey on the keyboard while this view has focus will cause the Clicked event to fire.
		/// </summary>
		public Key HotKey { get => textFormatter.HotKey; set => textFormatter.HotKey = value; }

		/// <summary>
		/// Gets or sets the specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public Rune HotKeySpecifier { get => textFormatter.HotKeySpecifier; set => textFormatter.HotKeySpecifier = value; }

		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					focusDirection = value;
			}
		}

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver { get { return Application.Driver; } }

		static IList<View> empty = new List<View> (0).AsReadOnly ();

		// This is null, and allocated on demand.
		List<View> subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => subviews == null ? empty : subviews.AsReadOnly ();

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => subviews ?? empty;

		// This is null, and allocated on demand.
		List<View> tabIndexes;

		/// <summary>
		/// This returns a tab index list of the subviews contained by this view.
		/// </summary>
		/// <value>The tabIndexes.</value>
		public IList<View> TabIndexes => tabIndexes == null ? empty : tabIndexes.AsReadOnly ();

		int tabIndex = -1;

		/// <summary>
		/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list.
		/// </summary>
		public int TabIndex {
			get { return tabIndex; }
			set {
				if (!CanFocus) {
					tabIndex = -1;
					return;
				} else if (SuperView?.tabIndexes == null || SuperView?.tabIndexes.Count == 1) {
					tabIndex = 0;
					return;
				} else if (tabIndex == value) {
					return;
				}
				tabIndex = value > SuperView.tabIndexes.Count - 1 ? SuperView.tabIndexes.Count - 1 : value < 0 ? 0 : value;
				tabIndex = GetTabIndex (tabIndex);
				if (SuperView.tabIndexes.IndexOf (this) != tabIndex) {
					SuperView.tabIndexes.Remove (this);
					SuperView.tabIndexes.Insert (tabIndex, this);
					SetTabIndex ();
				}
			}
		}

		private int GetTabIndex (int idx)
		{
			int i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1 || v == this) {
					continue;
				}
				i++;
			}
			return Math.Min (i, idx);
		}

		private void SetTabIndex ()
		{
			int i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1) {
					continue;
				}
				v.tabIndex = i;
				i++;
			}
		}

		bool tabStop = true;

		/// <summary>
		/// This only be <c>true</c> if the <see cref="CanFocus"/> is also <c>true</c> and the focus can be avoided by setting this to <c>false</c>
		/// </summary>
		public bool TabStop {
			get { return tabStop; }
			set {
				if (tabStop == value) {
					return;
				}
				tabStop = CanFocus && value;
			}
		}

		/// <inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set {
				if (base.CanFocus != value) {
					base.CanFocus = value;
					if (!value && tabIndex > -1) {
						TabIndex = -1;
					} else if (value && tabIndex == -1) {
						TabIndex = SuperView != null ? SuperView.tabIndexes.IndexOf (this) : -1;
					}
					TabStop = value;
				}
			}
		}

		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object. Superview relative.
		Rect frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		/// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
		public ustring Id { get; set; } = "";

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop {
			get {
				return Application.Current == this;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.
		/// </summary>
		/// <value><c>true</c> if want mouse position reports; otherwise, <c>false</c>.</value>
		public virtual bool WantMousePositionReports { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.
		/// </summary>
		public virtual bool WantContinuousButtonPressed { get; set; } = false;

		/// <summary>
		/// Gets or sets the frame for the view. The frame is relative to the view's container (<see cref="SuperView"/>).
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		/// <para>
		///    Change the Frame when using the <see cref="LayoutStyle.Absolute"/> layout style to move or resize views. 
		/// </para>
		/// <para>
		///    Altering the Frame of a view will trigger the redrawing of the
		///    view as well as the redrawing of the affected regions of the <see cref="SuperView"/>.
		/// </para>
		/// </remarks>
		public virtual Rect Frame {
			get => frame;
			set {
				if (SuperView != null) {
					SuperView.SetNeedsDisplay (frame);
					SuperView.SetNeedsDisplay (value);
				}
				frame = value;

				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Gets an enumerator that enumerates the subviews in this view.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator GetEnumerator ()
		{
			foreach (var v in InternalSubviews)
				yield return v;
		}

		LayoutStyle layoutStyle;

		/// <summary>
		/// Controls how the View's <see cref="Frame"/> is computed during the LayoutSubviews method, if the style is set to <see cref="LayoutStyle.Absolute"/>, 
		/// LayoutSubviews does not change the <see cref="Frame"/>. If the style is <see cref="LayoutStyle.Computed"/> the <see cref="Frame"/> is updated using
		/// the <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => layoutStyle;
			set {
				layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The bounds represent the View-relative rectangle used for this view; the area inside of the view.
		/// </summary>
		/// <value>The bounds.</value>
		/// <remarks>
		/// <para>
		/// Updates to the Bounds update the <see cref="Frame"/>,
		/// and has the same side effects as updating the <see cref="Frame"/>.
		/// </para>
		/// <para>
		/// Because <see cref="Bounds"/> coordinates are relative to the upper-left corner of the <see cref="View"/>, 
		/// the coordinates of the upper-left corner of the rectangle returned by this property are (0,0). 
		/// Use this property to obtain the size and coordinates of the client area of the 
		/// control for tasks such as drawing on the surface of the control.
		/// </para>
		/// </remarks>
		public Rect Bounds {
			get => new Rect (Point.Empty, Frame.Size);
			set {
				Frame = new Rect (frame.Location, value.Size);
			}
		}

		Pos x, y;

		/// <summary>
		/// Gets or sets the X position for the view (the column). Only used whe <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The X Position.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos X {
			get => x;
			set {
				x = value;
				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Gets or sets the Y position for the view (the row). Only used whe <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The y position (line).</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos Y {
			get => y;
			set {
				y = value;
				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		Dim width, height;

		/// <summary>
		/// Gets or sets the width of the view. Only used whe <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The width.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Dim Width {
			get => width;
			set {
				width = value;
				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Gets or sets the height of the view. Only used whe <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The height.</value>
		/// If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		public Dim Height {
			get => height;
			set {
				height = value;
				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public View SuperView => container;

		/// <summary>
		/// Initializes a new instance of a <see cref="LayoutStyle.Absolute"/> <see cref="View"/> class with the absolute
		/// dimensions specified in the <c>frame</c> parameter. 
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		/// <remarks>
		/// This constructor intitalize a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use <see cref="View()"/> to 
		/// initialize a View with  <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/> 
		/// </remarks>
		public View (Rect frame)
		{
			textFormatter = new TextFormatter ();
			this.Text = ustring.Empty;

			this.Frame = frame;
			LayoutStyle = LayoutStyle.Absolute;
		}

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		///   The <see cref="Label"/> will be created using <see cref="LayoutStyle.Computed"/>
		///   coordinates. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <c>Height</c> is greater than one, word wrapping is provided.
		/// </para>
		/// <para>
		///   This constructor intitalize a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		/// </para>
		/// </remarks>
		public View () : this (text: string.Empty) { }


		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="x">column to locate the Label.</param>
		/// <param name="y">row to locate the Label.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (int x, int y, ustring text) : this (TextFormatter.CalcRect (x, y, text), text) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <c>rect.Height</c> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="rect">Location.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (Rect rect, ustring text) : this (rect)
		{
			textFormatter = new TextFormatter ();
			this.Text = text;
		}

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created using <see cref="LayoutStyle.Computed"/>
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/> will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <c>Height</c> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (ustring text) : base ()
		{
			textFormatter = new TextFormatter ();
			this.Text = text;

			CanFocus = false;
			TabIndex = -1;
			TabStop = false;
			LayoutStyle = LayoutStyle.Computed;
			// BUGBUG: CalcRect doesn't account for line wrapping
			var r = TextFormatter.CalcRect (0, 0, text);
			x = Pos.At (0);
			y = Pos.At (0);
			Width = r.Width;
			Height = r.Height;
		}

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			SetNeedsDisplay (Bounds);
		}

		internal bool layoutNeeded = true;

		internal void SetNeedsLayout ()
		{
			if (layoutNeeded)
				return;
			layoutNeeded = true;
			if (SuperView == null)
				return;
			SuperView.SetNeedsLayout ();
			textFormatter.NeedsFormat = true;
		}

		/// <summary>
		/// Flags the view-relative region on this View as needing to be repainted.
		/// </summary>
		/// <param name="region">The view-relative region that must be flagged for repaint.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (NeedDisplay == null || NeedDisplay.IsEmpty)
				NeedDisplay = region;
			else {
				var x = Math.Min (NeedDisplay.X, region.X);
				var y = Math.Min (NeedDisplay.Y, region.Y);
				var w = Math.Max (NeedDisplay.Width, region.Width);
				var h = Math.Max (NeedDisplay.Height, region.Height);
				NeedDisplay = new Rect (x, y, w, h);
			}
			if (container != null)
				container.ChildNeedsDisplay ();
			if (subviews == null)
				return;
			foreach (var view in subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		internal bool childNeedsDisplay;

		/// <summary>
		/// Indicates that any child views (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void ChildNeedsDisplay ()
		{
			childNeedsDisplay = true;
			if (container != null)
				container.ChildNeedsDisplay ();
		}

		/// <summary>
		///   Adds a subview (child) to this view.
		/// </summary>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null)
				return;
			if (subviews == null) {
				subviews = new List<View> ();
			}
			if (tabIndexes == null) {
				tabIndexes = new List<View> ();
			}
			subviews.Add (view);
			tabIndexes.Add (view);
			view.container = this;
			OnAdded (view);
			if (view.CanFocus) {
				CanFocus = true;
				view.tabIndex = tabIndexes.IndexOf (view);
			}

			SetNeedsLayout ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Adds the specified views (children) to the view.
		/// </summary>
		/// <param name="views">Array of one or more views (can be optional parameter).</param>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public void Add (params View [] views)
		{
			if (views == null)
				return;
			foreach (var view in views)
				Add (view);
		}

		/// <summary>
		///   Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		public virtual void RemoveAll ()
		{
			if (subviews == null)
				return;

			while (subviews.Count > 0) {
				Remove (subviews [0]);
				Remove (tabIndexes [0]);
			}
		}

		/// <summary>
		///   Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || subviews == null)
				return;

			SetNeedsLayout ();
			SetNeedsDisplay ();
			var touched = view.Frame;
			subviews.Remove (view);
			tabIndexes.Remove (view);
			view.container = null;
			OnRemoved (view);
			view.tabIndex = -1;
			if (subviews.Count < 1)
				this.CanFocus = false;

			foreach (var v in subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
		}

		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (subviews.Contains (subview)) {
				action (subview);
			}

			SetNeedsDisplay ();
			subview.SetNeedsDisplay ();
		}

		/// <summary>
		/// Brings the specified subview to the front so it is drawn on top of any other views.
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="SendSubviewToBack"/>.
		/// </remarks>
		public void BringSubviewToFront (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Add (x);
			});
		}

		/// <summary>
		/// Sends the specified subview to the front so it is the first view drawn
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="BringSubviewToFront(View)"/>.
		/// </remarks>
		public void SendSubviewToBack (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Insert (0, subview);
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void SendSubviewBackwards (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx > 0) {
					subviews.Remove (x);
					subviews.Insert (idx - 1, x);
				}
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void BringSubviewForward (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx + 1 < subviews.Count) {
					subviews.Remove (x);
					subviews.Insert (idx + 1, x);
				}
			});
		}

		/// <summary>
		///   Clears the view region with the current color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the entire region used by this view.
		///   </para>
		/// </remarks>
		public void Clear ()
		{
			var h = Frame.Height;
			var w = Frame.Width;
			for (int line = 0; line < h; line++) {
				Move (0, line);
				for (int col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		///   Clears the specified region with the current color. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="regionScreen">The screen-relative region to clear.</param>
		public void Clear (Rect regionScreen)
		{
			var h = regionScreen.Height;
			var w = regionScreen.Width;
			for (int line = regionScreen.Y; line < regionScreen.Y + h; line++) {
				Driver.Move (regionScreen.X, line);
				for (int col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Converts a view-relative (col,row) position to a screen-relative position (col,row). The values are optionally clamped to the screen dimensions.
		/// </summary>
		/// <param name="col">View-relative column.</param>
		/// <param name="row">View-relative row.</param>
		/// <param name="rcol">Absolute column; screen-relative.</param>
		/// <param name="rrow">Absolute row; screen-relative.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method, if set to <c>true</c>, the rcol, rrow values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		internal void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
		{
			// Computes the real row, col relative to the screen.
			rrow = row + frame.Y;
			rcol = col + frame.X;
			var ccontainer = container;
			while (ccontainer != null) {
				rrow += ccontainer.frame.Y;
				rcol += ccontainer.frame.X;
				ccontainer = ccontainer.container;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clipped) {
				rrow = Math.Min (rrow, Driver.Rows - 1);
				rcol = Math.Min (rcol, Driver.Cols - 1);
			}
		}

		/// <summary>
		/// Converts a point from screen-relative coordinates to view-relative coordinates.
		/// </summary>
		/// <returns>The mapped point.</returns>
		/// <param name="x">X screen-coordinate point.</param>
		/// <param name="y">Y screen-coordinate point.</param>
		public Point ScreenToView (int x, int y)
		{
			if (SuperView == null) {
				return new Point (x - Frame.X, y - frame.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - frame.X, parent.Y - frame.Y);
			}
		}

		/// <summary>
		/// Converts a region in view-relative coordinates to screen-relative coordinates.
		/// </summary>
		internal Rect ViewToScreen (Rect region)
		{
			ViewToScreen (region.X, region.Y, out var x, out var y, clipped: false);
			return new Rect (x, y, region.Width, region.Height);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		internal Rect ScreenClip (Rect regionScreen)
		{
			var x = regionScreen.X < 0 ? 0 : regionScreen.X;
			var y = regionScreen.Y < 0 ? 0 : regionScreen.Y;
			var w = regionScreen.X + regionScreen.Width >= Driver.Cols ? Driver.Cols - regionScreen.X : regionScreen.Width;
			var h = regionScreen.Y + regionScreen.Height >= Driver.Rows ? Driver.Rows - regionScreen.Y : regionScreen.Height;

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Sets the <see cref="ConsoleDriver"/>'s clip region to the current View's <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The existing driver's clip region, which can be then re-eapplied by setting <c><see cref="Driver"/>.Clip</c> (<see cref="ConsoleDriver.Clip"/>).</returns>
		/// <remarks>
		/// <see cref="Bounds"/> is View-relative.
		/// </remarks>
		public Rect ClipToBounds ()
		{
			return SetClip (Bounds);
		}

		/// <summary>
		/// Sets the clip region to the specified view-relative region.
		/// </summary>
		/// <returns>The previous screen-relative clip region.</returns>
		/// <param name="region">View-relative clip region.</param>
		public Rect SetClip (Rect region)
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (region));
			return previous;
		}

		/// <summary>
		/// Draws a frame in the current view, clipped by the boundary of this view
		/// </summary>
		/// <param name="region">View-relative region for the frame to be drawn.</param>
		/// <param name="padding">The padding to add around the outside of the drawn frame.</param>
		/// <param name="fill">If set to <c>true</c> it fill will the contents.</param>
		public void DrawFrame (Rect region, int padding = 0, bool fill = false)
		{
			var scrRect = ViewToScreen (region);
			var savedClip = ClipToBounds ();
			Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: fill);
			Driver.Clip = savedClip;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey.
		/// </summary>
		/// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		/// <remarks>
		/// <para>The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by default.</para>
		/// <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
		/// </remarks>
		public void DrawHotString (ustring text, Attribute hotColor, Attribute normalColor)
		{
			var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
			Application.Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == hotkeySpec) {
					Application.Driver.SetAttribute (hotColor);
					continue;
				}
				Application.Driver.AddRune (rune);
				Application.Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscoore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <c>true</c> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (ustring text, bool focused, ColorScheme scheme)
		{
			if (focused)
				DrawHotString (text, scheme.HotFocus, scheme.Focus);
			else
				DrawHotString (text, scheme.HotNormal, scheme.Normal);
		}

		/// <summary>
		/// This moves the cursor to the specified column and row in the view.
		/// </summary>
		/// <returns>The move.</returns>
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		public void Move (int col, int row)
		{
			ViewToScreen (col, row, out var rcol, out var rrow);
			Driver.Move (rcol, rrow);
		}

		/// <summary>
		///   Positions the cursor in the right position based on the currently focused view in the chain.
		/// </summary>
		///    Views that are focusable should override <see cref="PositionCursor"/> to ensure
		///    the cursor is placed in a location that makes sense. Unix terminals do not have
		///    a way of hiding the cursor, so it can be distracting to have the cursor left at
		///    the last focused view. Views should make sure that they place the cursor
		///    in a visually sensible place.
		public virtual void PositionCursor ()
		{
			if (focused != null)
				focused.PositionCursor ();
			else {
				if (CanFocus && HasFocus) {
					Move (textFormatter.HotKeyPos == -1 ? 1 : textFormatter.HotKeyPos, 0);
				} else {
					Move (frame.X, frame.Y);
				}
			}
		}

		bool hasFocus;
		/// <inheritdoc/>
		public override bool HasFocus {
			get {
				return hasFocus;
			}
		}

		void SetHasFocus (bool value, View view)
		{
			if (hasFocus != value) {
				hasFocus = value;
			}
			if (value) {
				OnEnter (view);
			} else {
				OnLeave (view);
			}
			SetNeedsDisplay ();

			// Remove focus down the chain of subviews if focus is removed
			if (!value && focused != null) {
				focused.OnLeave (view);
				focused.SetHasFocus (false, view);
				focused = null;
			}
		}

		/// <summary>
		/// Defines the event arguments for <see cref="SetFocus(View)"/>
		/// </summary>
		public class FocusEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="view">The view that gets or loses focus.</param>
			public FocusEventArgs (View view) { View = view; }
			/// <summary>
			/// Indicates if the current focus event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; }
			/// <summary>
			/// Indicates the current view that gets or loses focus.
			/// </summary>
			public View View { get; set; }
		}

		/// <summary>
		/// Method invoked  when a subview is being added to this view.
		/// </summary>
		/// <param name="view">The subview being added.</param>
		public virtual void OnAdded (View view)
		{
			view.Added?.Invoke (this);
		}

		/// <summary>
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="view">The subview being removed.</param>
		public virtual void OnRemoved (View view)
		{
			view.Removed?.Invoke (this);
		}

		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			FocusEventArgs args = new FocusEventArgs (view);
			Enter?.Invoke (args);
			if (args.Handled)
				return true;
			if (base.OnEnter (view))
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool OnLeave (View view)
		{
			FocusEventArgs args = new FocusEventArgs (view);
			Leave?.Invoke (args);
			if (args.Handled)
				return true;
			if (base.OnLeave (view))
				return true;

			return false;
		}

		/// <summary>
		/// Returns the currently focused view inside this view, or null if nothing is focused.
		/// </summary>
		/// <value>The focused.</value>
		public View Focused => focused;

		/// <summary>
		/// Returns the most focused view in the chain of subviews (the leaf view that has the focus).
		/// </summary>
		/// <value>The most focused.</value>
		public View MostFocused {
			get {
				if (Focused == null)
					return null;
				var most = Focused.MostFocused;
				if (most != null)
					return most;
				return Focused;
			}
		}

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s
		/// color scheme.
		/// </summary>
		public ColorScheme ColorScheme {
			get {
				if (colorScheme == null)
					return SuperView?.ColorScheme;
				return colorScheme;
			}
			set {
				colorScheme = value;
			}
		}

		ColorScheme colorScheme;

		/// <summary>
		/// Displays the specified character in the specified column and row of the View.
		/// </summary>
		/// <param name="col">Column (view-relative).</param>
		/// <param name="row">Row (view-relative).</param>
		/// <param name="ch">Ch.</param>
		public void AddRune (int col, int row, Rune ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > frame.Height - 1 || col > frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Removes the <see cref="SetNeedsDisplay()"/> and the <see cref="ChildNeedsDisplay"/> setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			NeedDisplay = Rect.Empty;
			childNeedsDisplay = false;
		}

		/// <summary>
		/// Redraws this view and its subviews; only redraws the views that have been flagged for a re-display.
		/// </summary>
		/// <param name="bounds">The bounds (view-relative region) to redraw.</param>
		/// <remarks>
		/// <para>
		///    Always use <see cref="Bounds"/> (view-relative) when calling <see cref="Redraw(Rect)"/>, NOT <see cref="Frame"/> (superview-relative).
		/// </para>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globaly on the driver.
		/// </para>
		/// <para>
		///    Overrides of <see cref="Redraw"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
		///    larger than the <c>region</c> parameter.
		/// </para>
		/// </remarks>
		public virtual void Redraw (Rect bounds)
		{
			var clipRect = new Rect (Point.Empty, frame.Size);

			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);

			if (!ustring.IsNullOrEmpty (Text)) {
				Clear ();
				// Draw any Text
				if (textFormatter != null) {
					textFormatter.NeedsFormat = true;
				}
				textFormatter?.Draw (ViewToScreen (Bounds), HasFocus ? ColorScheme.Focus : ColorScheme.Normal, HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
			}

			// Invoke DrawContentEvent
			OnDrawContent (bounds);

			if (subviews != null) {
				foreach (var view in subviews) {
					if (view.NeedDisplay != null && (!view.NeedDisplay.IsEmpty || view.childNeedsDisplay)) {
						if (view.Frame.IntersectsWith (clipRect) && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
							if (view.layoutNeeded)
								view.LayoutSubviews ();
							Application.CurrentView = view;

							if (!view.initialized) {
								view.OnInitialized ();
							}

							// Draw the subview
							// Use the view's bounds (view-relative; Location will always be (0,0) because
							view.Redraw (view.Bounds);
						}
						view.NeedDisplay = Rect.Empty;
						view.childNeedsDisplay = false;
					}
				}
			}
			ClearNeedsDisplay ();
		}

		bool initialized;

		/// <summary>
		/// Event called only once when the content area of the Visualization will actually be drawn for the first time.
		/// </summary>
		public Action Initialized;

		/// <summary>
		/// Allows configurations and assignments to be performed only once before the view is drawn for the first time.
		/// </summary>
		public virtual void OnInitialized ()
		{
			initialized = true;
			Initialized?.Invoke ();
		}

		/// <summary>
		/// Event invoked when the content area of the View is to be drawn.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public Action<Rect> DrawContent;

		/// <summary>
		/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls. 
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn. 
		/// </remarks>
		public virtual void OnDrawContent (Rect viewport)
		{
			DrawContent?.Invoke (viewport);
		}

		/// <summary>
		/// Causes the specified subview to have focus.
		/// </summary>
		/// <param name="view">View.</param>
		public void SetFocus (View view)
		{
			if (view == null)
				return;
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus)
				return;
			if (focused == view)
				return;

			// Make sure that this view is a subview
			View c;
			for (c = view.container; c != null; c = c.container)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (focused != null)
				focused.SetHasFocus (false, view);

			var f = focused;
			focused = view;
			focused.SetHasFocus (true, f);
			focused.EnsureFocus ();

			// Send focus upwards
			SuperView?.SetFocus (this);
		}

		/// <summary>
		/// Defines the event arguments for <see cref="KeyEvent"/>
		/// </summary>
		public class KeyEventEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="ke"></param>
			public KeyEventEventArgs (KeyEvent ke) => KeyEvent = ke;
			/// <summary>
			/// The <see cref="KeyEvent"/> for the event.
			/// </summary>
			public KeyEvent KeyEvent { get; set; }
			/// <summary>
			/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; } = false;
		}

		/// <summary>
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public Action<KeyEventEventArgs> KeyPress;

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (args);
			if (args.Handled)
				return true;
			if (Focused?.ProcessKey (keyEvent) == true)
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessHotKey (keyEvent))
					return true;
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed
		/// </summary>
		public Action<KeyEventEventArgs> KeyDown;

		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.HasFocus && view.OnKeyDown (keyEvent))
					return true;

			return false;
		}

		/// <summary>
		/// Invoked when a key is released
		/// </summary>
		public Action<KeyEventEventArgs> KeyUp;

		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.HasFocus && view.OnKeyUp (keyEvent))
					return true;

			return false;
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, it does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (focused == null)
				if (FocusDirection == Direction.Forward)
					FocusFirst ();
				else
					FocusLast ();
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in tabIndexes) {
				if (view.CanFocus && view.tabStop) {
					SetFocus (view);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the last focusable subview if one exists.
		/// </summary>
		public void FocusLast ()
		{
			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (int i = tabIndexes.Count; i > 0;) {
				i--;

				View v = tabIndexes [i];
				if (v.CanFocus && v.tabStop) {
					SetFocus (v);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the previous view.
		/// </summary>
		/// <returns><c>true</c>, if previous was focused, <c>false</c> otherwise.</returns>
		public bool FocusPrev ()
		{
			FocusDirection = Direction.Backward;
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusLast ();
				return focused != null;
			}
			int focused_idx = -1;
			for (int i = tabIndexes.Count; i > 0;) {
				i--;
				View w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1 && w.tabStop) {
					focused.SetHasFocus (false, w);

					if (w != null && w.CanFocus && w.tabStop)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
			}
			return false;
		}

		/// <summary>
		/// Focuses the next view.
		/// </summary>
		/// <returns><c>true</c>, if next was focused, <c>false</c> otherwise.</returns>
		public bool FocusNext ()
		{
			FocusDirection = Direction.Forward;
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusFirst ();
				return focused != null;
			}
			int n = tabIndexes.Count;
			int focused_idx = -1;
			for (int i = 0; i < n; i++) {
				View w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1 && w.tabStop) {
					focused.SetHasFocus (false, w);

					if (w != null && w.CanFocus && w.tabStop)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
			}
			return false;
		}

		/// <summary>
		/// Sets the View's <see cref="Frame"/> to the relative coordinates if its container, given the <see cref="Frame"/> for its container.
		/// </summary>
		/// <param name="hostFrame">The screen-relative frame for the host.</param>
		/// <remarks>
		/// Reminder: <see cref="Frame"/> is superview-relative; <see cref="Bounds"/> is view-relative.
		/// </remarks>
		internal void SetRelativeLayout (Rect hostFrame)
		{
			int w, h, _x, _y;

			if (x is Pos.PosCenter) {
				if (width == null)
					w = hostFrame.Width;
				else
					w = width.Anchor (hostFrame.Width);
				_x = x.Anchor (hostFrame.Width - w);
			} else {
				if (x == null)
					_x = 0;
				else
					_x = x.Anchor (hostFrame.Width);
				if (width == null)
					w = hostFrame.Width;
				else if (width is Dim.DimFactor && !((Dim.DimFactor)width).IsFromRemaining ())
					w = width.Anchor (hostFrame.Width);
				else
					w = Math.Max (width.Anchor (hostFrame.Width - _x), 0);
			}

			if (y is Pos.PosCenter) {
				if (height == null)
					h = hostFrame.Height;
				else
					h = height.Anchor (hostFrame.Height);
				_y = y.Anchor (hostFrame.Height - h);
			} else {
				if (y == null)
					_y = 0;
				else
					_y = y.Anchor (hostFrame.Height);
				if (height == null)
					h = hostFrame.Height;
				else if (height is Dim.DimFactor && !((Dim.DimFactor)height).IsFromRemaining ())
					h = height.Anchor (hostFrame.Height);
				else
					h = Math.Max (height.Anchor (hostFrame.Height - _y), 0);
			}
			Frame = new Rect (_x, _y, w, h);
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		List<View> TopologicalSort (HashSet<View> nodes, HashSet<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var S = new HashSet<View> (nodes.Where (n => edges.All (e => e.To.Equals (n) == false)));

			while (S.Any ()) {
				//  remove a node n from S
				var n = S.First ();
				S.Remove (n);

				// add n to tail of L
				if (n != this?.SuperView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => me.To.Equals (m) == false) && m != this?.SuperView) {
						// insert m into S
						S.Add (m);
					}
				}
			}

			if (edges.Any ()) {
				if (!object.ReferenceEquals (edges.First ().From, edges.First ().To)) {
					throw new InvalidOperationException ($"TopologicalSort (for Pos/Dim) cannot find {edges.First ().From}. Did you forget to add it to {this}?");
				} else {
					throw new InvalidOperationException ("TopologicalSort encountered a recursive cycle in the relative Pos/Dim in the views of " + this);
				}
			} else {
				// return L (a topologically sorted order)
				return result;
			}
		}

		/// <summary>
		/// Event arguments for the <see cref="LayoutComplete"/> event.
		/// </summary>
		public class LayoutEventArgs : EventArgs {
			/// <summary>
			/// The view-relative bounds of the <see cref="View"/> before it was laid out.
			/// </summary>
			public Rect OldBounds { get; set; }
		}

		/// <summary>
		/// Fired after the Views's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public Action<LayoutEventArgs> LayoutStarted;

		/// <summary>
		/// Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews have been laid out.
		/// </summary>
		internal virtual void OnLayoutStarted (LayoutEventArgs args)
		{
			LayoutStarted?.Invoke (args);
		}

		/// <summary>
		/// Fired after the Views's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public Action<LayoutEventArgs> LayoutComplete;

		/// <summary>
		/// Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views have been laid out.
		/// </summary>
		internal virtual void OnLayoutComplete (LayoutEventArgs args)
		{
			LayoutComplete?.Invoke (args);
		}

		/// <summary>
		/// Invoked when a view starts executing or when the dimensions of the view have changed, for example in
		/// response to the container view or terminal resizing.
		/// </summary>
		/// <remarks>
		/// Calls <see cref="OnLayoutComplete"/> (which raises the <see cref="LayoutComplete"/> event) before it returns.
		/// </remarks>
		public virtual void LayoutSubviews ()
		{
			if (!layoutNeeded) {
				return;
			}

			Rect oldBounds = Bounds;
			OnLayoutStarted (new LayoutEventArgs () { OldBounds = oldBounds });

			textFormatter.Size = Bounds.Size;


			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();

			foreach (var v in InternalSubviews) {
				nodes.Add (v);
				if (v.LayoutStyle == LayoutStyle.Computed) {
					if (v.X is Pos.PosView vX)
						edges.Add ((vX.Target, v));
					if (v.Y is Pos.PosView vY)
						edges.Add ((vY.Target, v));
					if (v.Width is Dim.DimView vWidth)
						edges.Add ((vWidth.Target, v));
					if (v.Height is Dim.DimView vHeight)
						edges.Add ((vHeight.Target, v));
				}
			}

			var ordered = TopologicalSort (nodes, edges);

			foreach (var v in ordered) {
				if (v.LayoutStyle == LayoutStyle.Computed) {
					v.SetRelativeLayout (Frame);
				}

				v.LayoutSubviews ();
				v.layoutNeeded = false;

			}

			if (SuperView == Application.Top && layoutNeeded && ordered.Count == 0 && LayoutStyle == LayoutStyle.Computed) {
				SetRelativeLayout (Frame);
			}

			layoutNeeded = false;

			OnLayoutComplete (new LayoutEventArgs () { OldBounds = oldBounds });
		}

		/// <summary>
		///   The text displayed by the <see cref="View"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///  If provided, the text will be drawn before any subviews are drawn.
		/// </para>
		/// <para>
		///  The text will be drawn starting at the view origin (0, 0) and will be formatted according
		///  to the <see cref="TextAlignment"/> property. If the view's height is greater than 1, the
		///  text will word-wrap to additional lines if it does not fit horizontally. If the view's height
		///  is 1, the text will be clipped.
		/// </para>
		/// <para>
		///  Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set <see cref="HotKeySpecifier"/> to
		///  <c>(Rune)0xffff</c>.
		/// </para>
		/// </remarks>
		public virtual ustring Text {
			get => textFormatter.Text;
			set {
				textFormatter.Text = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextAlignment TextAlignment {
			get => textFormatter.Alignment;
			set {
				textFormatter.Alignment = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Pretty prints the View
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}

		/// <summary>
		/// Specifies the event arguments for <see cref="MouseEvent"/>
		/// </summary>
		public class MouseEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="me"></param>
			public MouseEventArgs (MouseEvent me) => MouseEvent = me;
			/// <summary>
			/// The <see cref="MouseEvent"/> for the event.
			/// </summary>
			public MouseEvent MouseEvent { get; set; }
			/// <summary>
			/// Indicates if the current mouse event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; }
		}

		/// <inheritdoc/>
		public override bool OnMouseEnter (MouseEvent mouseEvent)
		{
			MouseEventArgs args = new MouseEventArgs (mouseEvent);
			MouseEnter?.Invoke (args);
			if (args.Handled)
				return true;
			if (base.OnMouseEnter (mouseEvent))
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool OnMouseLeave (MouseEvent mouseEvent)
		{
			MouseEventArgs args = new MouseEventArgs (mouseEvent);
			MouseLeave?.Invoke (args);
			if (args.Handled)
				return true;
			if (base.OnMouseLeave (mouseEvent))
				return true;

			return false;
		}

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public virtual bool OnMouseEvent (MouseEvent mouseEvent)
		{
			MouseEventArgs args = new MouseEventArgs (mouseEvent);
			MouseClick?.Invoke (args);
			if (args.Handled)
				return true;
			if (MouseEvent (mouseEvent))
				return true;


			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus && SuperView != null) {
					SuperView.SetFocus (this);
					SetNeedsDisplay ();
				}

				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			for (int i = InternalSubviews.Count - 1; i >= 0; i--) {
				View subview = InternalSubviews [i];
				Remove (subview);
				subview.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}
