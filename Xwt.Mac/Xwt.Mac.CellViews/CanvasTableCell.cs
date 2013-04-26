//
// CanvasTableCell.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoMac.AppKit;
using System.Drawing;
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
	public class CanvasTableCell: NSCell, ICellRenderer
	{
		CanvasCellView cellView;

		public CanvasTableCell (IntPtr p): base (p)
		{
		}

		public CanvasTableCell ()
		{
		}

		public CanvasTableCell (CanvasCellView cellView)
		{
			this.cellView = cellView;
		}
		
		public void CopyFrom (object other)
		{
			var ob = (CanvasTableCell)other;
			cellView = ob.cellView;
		}

		public void Fill (ICellDataSource source)
		{
			cellView.Initialize (source);
		}

		public override NSSize CellSizeForBounds (NSRect bounds)
		{
			var r = (ICanvasCellRenderer)cellView;
			var size = new NSSize ();
			r.ApplicationContext.InvokeUserCode (delegate {
				var s = r.GetRequiredSize ();
				size = new NSSize ((float)s.Width, (float)s.Height);
			});
			if (size.Width > bounds.Width)
				size.Width = bounds.Width;
			if (size.Height > bounds.Height)
				size.Height = bounds.Height;
			return size;
		}

		public override void DrawInteriorWithFrame (NSRect cellFrame, NSView inView)
		{
			var r = (ICanvasCellRenderer)cellView;

			CGContext ctx = NSGraphicsContext.CurrentContext.GraphicsPort;
			
			var backend = new CGContextBackend {
				Context = ctx,
				InverseViewTransform = ctx.GetCTM ().Invert ()
			};
			r.ApplicationContext.InvokeUserCode (delegate {
				r.Draw (backend, new Rectangle (cellFrame.X, cellFrame.Y, cellFrame.Width, cellFrame.Height));
			});
		}
	}
}

