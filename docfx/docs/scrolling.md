# Scrolling

Terminal.Gui provides a rich system for how [View](View.md) users can scroll content with the keyboard and/or mouse.

## Lexicon & Taxonomy

See [View Deep Dive](View.md) for broader definitions.

* *Scroll* (Verb) - The act of causing content to move either horizontally or vertically within the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml). Also referred to as "Content Scrolling".
* *ScrollSlider* - A visual indicator that shows the proportion of the scrollable content to the size of the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) and allows the user to use the mouse to scroll. 
* *[ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)* -  Indicates the size of scrollable content and controls the position of the visible content, either vertically or horizontally. At each end, a @Terminal.Gui.Button is provided, one to scroll up or left and one to scroll down or right. Between the
 buttons is a @Terminal.Gui.ScrollSlider that can be dragged to control the position of the visible content. The ScrollSlier is sized to show the proportion of the scrollable content to the size of the @Terminal.Gui.View.Viewport.

## Overview

The ability to scroll content is built into View. The [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) represents the scrollable "viewport" into the View's Content Area (which is defined by the return value of [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml)). 

By default, [View](~/api/Terminal.Gui.View.yml), includes no bindings for the typical directional keyboard and mouse input and cause the Content Area.

Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This ability is not enabled by default given how precious TUI screen real estate is.

Scrolling with the mouse and keyboard are enabled by:

1) Making the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) size smaller than the size returned by [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml). 
2) Creating key bindings for the appropriate directional keys (e.g. [Key.CursorDown](~/api/Terminal.Gui.Key)), and calling [View.ScrollHorizontal()](~/api/Terminal.Gui.View.ScrollHorizontal.yml)/[ScrollVertical()](~/api/Terminal.Gui.View.ScrollVertical.yml) as needed.
3) Subscribing to [View.MouseEvent](~/api/Terminal.Gui.View.MouseEvent.yml) and calling calling [View.ScrollHorizontal()](~/api/Terminal.Gui.View.ScrollHorizontal.yml)/[ScrollVertical()](~/api/Terminal.Gui.View.ScrollVertical.yml) as needed.
4) Enabling the [ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)s built into View ([View.HorizontalScrollBar/VerticalScrollBar](~/api/Terminal.Gui.View.HorizontalScrollBar.yml)) by either enabling automatic show/hide behavior (@Terminal.Gui.ScrollBar.AutoShow) or explicitly making them visible (@Terminal.Gui.View.Visible).

While *[ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)* can be used in a standalone manner to provide proportional scrolling, it is typically enabled automatically via the [View.HorizontalScrollBar](~/api/Terminal.Gui.View.HorizontalScrollBar.yml) and  [View.VerticalScrollBar](~/api/Terminal.Gui.View.VerticalScrollBar.yml) properties.

## Examples

These Scenarios illustrate Terminal.Gui scrolling:

* *Scrolling* - Demonstrates the @Terminal.Gui.ScrollBar objects built into-View.
* *ScrollBar Demo* - Demonstrates using @Terminal.Gui.ScrollBar view in a standalone manner.
* *ViewportSettings* - Demonstrates the various [Viewport Settings](~/api/Terminal.Gui.ViewportSettings.yml) (see below) in an interactive manner. Used by the development team to visually verify that convoluted View layout and arrangement scenarios scroll properly.
* *Character Map* - Demonstrates a sophisticated scrolling use-case. The entire set of Unicode code-points can be scrolled and searched. From a scrolling perspective, this Scenario illustrates how to manually configure `Viewport`, `SetContentArea()`, and `ViewportSettings` to enable horizontal and vertical headers (as might appear in a spreadsheet), full keyboard and mouse support, and more. 
* *ListView* and *HexEdit* - The source code to these built-in Views are good references for how to support scrolling and ScrollBars in a re-usable View sub-class. 

## [Viewport Settings](~/api/Terminal.Gui.ViewportSettings.yml)

Use [View.ViewportSettings](~/api/Terminal.Gui.View.ViewportSettings.yml) to adjust the behavior of scrolling. 

* [AllowNegativeX/Y](~/api/Terminal.Gui.ViewportSettings.AllowNegativeXyml) - If set, Viewport.Size can be set to negative coordinates enabling scrolling beyond the top-left of the content area.

* [AllowX/YGreaterThanContentWidth](~/api/Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth) - If set, Viewport.Size can be set values greater than GetContentSize() enabling scrolling beyond the bottom-right of the Content Area. When not set, `Viewport.X/Y` are constrained to the dimension of the content area - 1. This means the last column of the content will remain visible even if there is an attempt to scroll the Viewport past the last column. The practical effect of this is that the last column/row of the content will always be visible.

* [ClipContentOnly](~/api/Terminal.Gui.ViewportSettings.ClipContentOnly) - By default, clipping is applied to [Viewport](~/api/Terminal.Gui.View.Viewport.yml). Setting this flag will cause clipping to be applied to the visible content area.

* [ClearContentOnly](~/api/Terminal.Gui.ViewportSettings.ClearContentOnly) - If set [View.Clear()](~/api/Terminal.Gui.View.Clear.yml) will clear only the portion of the content area that is visible within the Viewport. This is useful for views that have a content area larger than the Viewport and want the area outside the content to be visually distinct.

* [EnableHorizontal/VerticalScrollBar](~/api/Terminal.Gui.ViewportSettings.EnableHorizontalScrollBar) - If set, the scroll bar will be enabled and automatically made visible when the corresponding dimension of [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) is smaller than the dimension of [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml).

