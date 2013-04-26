// 
// CanvasBackend.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using MonoMac.CoreGraphics;
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
	public class CanvasBackend: ViewBackend<NSView,ICanvasEventSink>, ICanvasBackend
	{
		CanvasView view;
		
		public CanvasBackend ()
		{
		}

		public override void Initialize ()
		{
			view = new CanvasView (EventSink, ApplicationContext);
			ViewObject = view;
		}

		protected override void OnSizeToFit ()
		{
			var w = EventSink.OnGetPreferredWidth ().MinSize;
			var h = EventSink.OnGetPreferredHeightForWidth (w).MinSize;
			Widget.SetFrameSize (new NSSize ((float)w, (float)h)); 
		}

		public Rectangle Bounds {
			get {
				return new Rectangle (0, 0, view.Frame.Width, view.Frame.Height);
			}
		}
		
		public void QueueDraw ()
		{
			view.NeedsDisplay = true;
		}
		
		public void QueueDraw (Rectangle rect)
		{
			view.NeedsToDraw (new NSRect ((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height));
		}
		
		public void AddChild (IWidgetBackend widget, Rectangle rect)
		{
			var v = GetWidget (widget);
			view.AddSubview (v);
			
			// Not using SetWidgetBounds because the view is flipped
			v.Frame = new NSRect ((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);;
			v.NeedsDisplay = true;
		}
		
		public void RemoveChild (IWidgetBackend widget)
		{
			var v = GetWidget (widget);
			v.RemoveFromSuperview ();
		}
		
		public void SetChildBounds (IWidgetBackend widget, Rectangle rect)
		{
			var w = GetWidget (widget);
			
			// Not using SetWidgetBounds because the view is flipped
			w.Frame = new NSRect ((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);;
			w.NeedsDisplay = true;
		}
	}
	
	class CanvasView: WidgetView
	{
		ICanvasEventSink eventSink;
		
		public CanvasView (ICanvasEventSink eventSink, ApplicationContext context): base (eventSink, context)
		{
			this.eventSink = eventSink;
		}

		public override void DrawRect (NSRect dirtyRect)
		{
			context.InvokeUserCode (delegate {
				CGContext ctx = NSGraphicsContext.CurrentContext.GraphicsPort;

				//fill BackgroundColor
				ctx.SetFillColor (Backend.Frontend.BackgroundColor.ToCGColor ());
				var b = new System.Drawing.RectangleF((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height);
				ctx.FillRect (b);

				var backend = new CGContextBackend {
					Context = ctx,
					InverseViewTransform = ctx.GetCTM ().Invert ()
				};
				eventSink.OnDraw (backend, new Rectangle (dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height));
			});
		}
	}
}

