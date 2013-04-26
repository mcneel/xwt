// 
// ScrollViewBackend.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Xwt.Backends;
using MonoMac.AppKit;
using MonoMac.Foundation;

#if MAC64
using NSInteger = System.Int64;
using NSUInteger = System.UInt64;
using CGFloat = System.Double;
#else
using NSInteger = System.Int32;
using NSUInteger = System.UInt32;
using NSPoint = System.Drawing.PointF;
using NSSize = System.Drawing.SizeF;
using NSRect = System.Drawing.RectangleF;
using CGFloat = System.Single;
#endif

namespace Xwt.Mac
{
	public class ScrollViewBackend: ViewBackend<NSScrollView,IScrollViewEventSink>, IScrollViewBackend
	{
		IWidgetBackend child;
		ScrollPolicy verticalScrollPolicy;
		ScrollPolicy horizontalScrollPolicy;

		public override void Initialize ()
		{
			ViewObject = new CustomScrollView ();
			Widget.HasHorizontalScroller = true;
			Widget.HasVerticalScroller = true;
			Widget.AutoresizesSubviews = true;
		}
		
		protected override Size GetNaturalSize ()
		{
			return EventSink.GetDefaultNaturalSize ();
		}
		
		public void SetChild (IWidgetBackend child)
		{
			this.child = child;
			ViewBackend backend = (ViewBackend) child;
			if (backend.EventSink.SupportsCustomScrolling ()) {
				var vs = new ScrollAdjustmentBackend (Widget, true);
				var hs = new ScrollAdjustmentBackend (Widget, false);
				CustomClipView clipView = new CustomClipView (hs, vs);
				Widget.ContentView = clipView;
				var dummy = new DummyClipView ();
				dummy.AddSubview (backend.Widget);
				backend.Widget.Frame = new NSRect (0, 0, clipView.Frame.Width, clipView.Frame.Height);
				clipView.DocumentView = dummy;
				backend.EventSink.SetScrollAdjustments (hs, vs);
			}
			else {
				Widget.DocumentView = backend.Widget;
				UpdateChildSize ();
			}
		}
		
		public ScrollPolicy VerticalScrollPolicy {
			get {
				return verticalScrollPolicy;
			}
			set {
				verticalScrollPolicy = value;
			}
		}

		public ScrollPolicy HorizontalScrollPolicy {
			get {
				return horizontalScrollPolicy;
			}
			set {
				horizontalScrollPolicy = value;
			}
		}
		
		public Rectangle VisibleRect {
			get {
				return Rectangle.Zero;
			}
		}
		
		public bool BorderVisible {
			get {
				return false;
			}
			set {
			}
		}

		void UpdateChildSize ()
		{
			if (child == null)
				return;

			if (Widget.ContentView is CustomClipView) {
			} else {
				NSView view = (NSView)Widget.DocumentView;
				ViewBackend c = (ViewBackend)child;
				var pw = c.Frontend.Surface.GetPreferredWidth ();
				var w = Math.Max (pw.NaturalSize, Widget.ContentView.Frame.Width);
				var ph = c.Frontend.Surface.GetPreferredHeightForWidth (w);
				var h = Math.Max (ph.NaturalSize, Widget.ContentView.Frame.Height);
				view.Frame = new NSRect (view.Frame.X, view.Frame.Y, (float)w, (float)h);
			}
		}
		
		public void SetChildSize (Size s)
		{
			UpdateChildSize ();
		}
	}
	
	class CustomScrollView: NSScrollView, IViewObject
	{
		public NSView View {
			get {
				return this;
			}
		}

		public ViewBackend Backend { get; set; }
		
		public override bool IsFlipped {
			get {
				return true;
			}
		}
	}

	class DummyClipView: NSView
	{
		public override bool IsFlipped {
			get {
				return true;
			}
		}
	}
	
	class CustomClipView: NSClipView
	{
		ScrollAdjustmentBackend hScroll;
		ScrollAdjustmentBackend vScroll;
		double currentX;
		double currentY;
		double ratioX = 1, ratioY = 1;

		public CustomClipView (ScrollAdjustmentBackend hScroll, ScrollAdjustmentBackend vScroll)
		{
			this.hScroll = hScroll;
			this.vScroll = vScroll;
			CopiesOnScroll = false;
		}

		public double CurrentX {
			get {
				return hScroll.LowerValue + (currentX / ratioX);
			}
			set {
				ScrollToPoint (new NSPoint ((float)(value - hScroll.LowerValue) * ratioX, currentY));
			}
		}

		public double CurrentY {
			get {
				return vScroll.LowerValue + (currentY / ratioY);
			}
			set {
				ScrollToPoint (new NSPoint (currentX, (float)(value - vScroll.LowerValue) * ratioY));
			}
		}

		public override bool IsFlipped {
			get {
				return true;
			}
		}

		public override void SetFrameSize (NSSize newSize)
		{
			base.SetFrameSize (newSize);
			var v = DocumentView.Subviews [0];
			v.Frame = new NSRect (v.Frame.X, v.Frame.Y, newSize.Width, newSize.Height);
		}
		
		public override void ScrollToPoint (NSPoint newOrigin)
		{
			base.ScrollToPoint (newOrigin);
			var v = DocumentView.Subviews [0];

			currentX = newOrigin.X >= 0 ? newOrigin.X : 0;
			currentY = newOrigin.Y >= 0 ? newOrigin.Y : 0;
			if (currentX + v.Frame.Width > DocumentView.Frame.Width)
				currentX = DocumentView.Frame.Width - v.Frame.Width;
			if (currentY + v.Frame.Height > DocumentView.Frame.Height)
				currentY = DocumentView.Frame.Height - v.Frame.Height;

			v.Frame = new NSRect (currentX, currentY, v.Frame.Width, v.Frame.Height);

			hScroll.NotifyValueChanged ();
			vScroll.NotifyValueChanged ();
		}

		public void UpdateDocumentSize ()
		{
			var vr = DocumentVisibleRect ();
			ratioX = hScroll.PageSize != 0 ? vr.Width / (float)hScroll.PageSize : 1;
			ratioY = vScroll.PageSize != 0 ? vr.Height / (float)vScroll.PageSize : 1;
			DocumentView.Frame = new NSRect (0, 0, (float)(hScroll.UpperValue - hScroll.LowerValue) * ratioX, (float)(vScroll.UpperValue - vScroll.LowerValue) * ratioY);
		}
	}
}

