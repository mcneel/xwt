// 
// ImageTableCell.cs
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
using MonoMac.AppKit;
using MonoMac.Foundation;

using Xwt.Drawing;
using Xwt.Backends;

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
	class ImageTableCell: NSImageCell, ICellRenderer
	{
		ImageCellView cellView;
		
		public ImageTableCell ()
		{
		}
		
		public ImageTableCell (IntPtr p): base (p)
		{
		}
		
		public ImageTableCell (ImageCellView cellView)
		{
			this.cellView = cellView;
		}
		
		public void Fill (ICellDataSource source)
		{
			cellView.Initialize (source);
			var img = cellView.Image;
			if (img != null)
				ObjectValue = (NSImage) Toolkit.GetBackend (img);
		}
		
		public override NSSize CellSize {
			get {
				NSImage img = ObjectValue as NSImage;
				if (img != null)
					return img.Size;
				else
					return base.CellSize;
			}
		}
		
		public void CopyFrom (object other)
		{
			var ob = (ImageTableCell)other;
			cellView = ob.cellView;
		}
	}
}

