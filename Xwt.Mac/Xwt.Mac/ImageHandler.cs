// 
// ImageHandler.cs
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
using System.Linq;
using Xwt.Backends;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xwt.Drawing;
using System.IO;
using MonoMac.CoreGraphics;

namespace Xwt.Mac
{
	public class ImageHandler: ImageBackendHandler
	{
		static readonly IntPtr sel_alloc = new Selector ("alloc").Handle;
		static readonly IntPtr sel_release = new Selector ("release").Handle;
		static readonly IntPtr sel_initWithIconRef = new Selector ("initWithIconRef:").Handle;
		static readonly IntPtr cls_NSImage = new Class (typeof (NSImage)).Handle;

		static Dictionary<string, NSImage> stockIcons = new Dictionary<string, NSImage> ();
		
		public override object LoadFromStream (Stream stream)
		{
			using (NSData data = NSData.FromStream (stream)) {
				return new NSImage (data);
			}
		}
		
		public override object LoadFromFile (string file)
		{
			return new NSImage (file);
		}

		public override object CreateMultiSizeImage (IEnumerable<object> images)
		{
			NSImage res = new NSImage ();
			foreach (NSImage img in images)
				res.AddRepresentations (img.Representations ());
			return res;
		}

		public override object CreateCustomDrawn (ImageDrawCallback drawCallback)
		{
			return new CustomImage (ApplicationContext, drawCallback);
		}

		public override Xwt.Drawing.Image GetStockIcon (string id)
		{
			NSImage img;
			if (!stockIcons.TryGetValue (id, out img)) {
				img = LoadStockIcon (id);
				stockIcons [id] = img;
			}
			return ApplicationContext.Toolkit.WrapImage (img);
		}

		public override void SaveToStream (object backend, System.IO.Stream stream, ImageFileType fileType)
		{
			NSImage img = backend as NSImage;
			if (img == null)
				throw new NotSupportedException ();

			var imageData = img.AsTiff ();
			var imageRep = (NSBitmapImageRep) NSBitmapImageRep.ImageRepFromData (imageData);
			var props = new NSDictionary ();
			imageData = imageRep.RepresentationUsingTypeProperties (fileType.ToMacFileType (), props);
			using (var s = imageData.AsStream ()) {
				s.CopyTo (stream);
			}
		}

		public override bool IsBitmap (object handle)
		{
			NSImage img = handle as NSImage;
			return img != null && img.Representations ().OfType<NSBitmapImageRep> ().Any ();
		}

		public override object ConvertToBitmap (object handle, int pixelWidth, int pixelHeight, ImageFormat format)
		{
			if (handle is CustomImage) {
				var flags = CGBitmapFlags.ByteOrderDefault;
				int bytesPerRow;
				switch (format) {
				case ImageFormat.ARGB32:
					bytesPerRow = pixelWidth * 4;
					flags |= CGBitmapFlags.PremultipliedFirst;
					break;

				case ImageFormat.RGB24:
					bytesPerRow = pixelWidth * 3;
					flags |= CGBitmapFlags.None;
					break;

				default:
					throw new NotImplementedException ("ImageFormat: " + format.ToString ());
				}

				var bmp = new CGBitmapContext (IntPtr.Zero, pixelWidth, pixelHeight, 8, bytesPerRow, Util.DeviceRGBColorSpace, flags);
				bmp.TranslateCTM (0, pixelHeight);
				bmp.ScaleCTM (1, -1);

				var ctx = new CGContextBackend {
					Context = bmp,
					Size = new NSSize (pixelWidth, pixelHeight),
					InverseViewTransform = bmp.GetCTM ().Invert ()
				};

				var ci = (CustomImage)handle;
				ci.DrawInContext (ctx);

				var img = new NSImage (((CGBitmapContext)bmp).ToImage (), new NSSize (pixelWidth, pixelHeight));
				var imageData = img.AsTiff ();
				var imageRep = (NSBitmapImageRep) NSBitmapImageRep.ImageRepFromData (imageData);
				var im = new NSImage ();
				im.AddRepresentation (imageRep);
				return im;
			}
			else
				return handle;
		}
		
		public override Xwt.Drawing.Color GetBitmapPixel (object handle, int x, int y)
		{
			NSImage img = (NSImage)handle;
			NSBitmapImageRep bitmap = img.Representations ().OfType<NSBitmapImageRep> ().FirstOrDefault ();
			if (bitmap != null)
				return bitmap.ColorAt (x, y).ToXwtColor ();
			else
				throw new InvalidOperationException ("Not a bitmnap image");
		}
		
		public override void SetBitmapPixel (object handle, int x, int y, Xwt.Drawing.Color color)
		{
			NSImage img = (NSImage)handle;
			NSBitmapImageRep bitmap = img.Representations ().OfType<NSBitmapImageRep> ().FirstOrDefault ();
			if (bitmap != null)
				bitmap.SetColorAt (color.ToNSColor (), x, y);
			else
				throw new InvalidOperationException ("Not a bitmnap image");
		}

		public override bool HasMultipleSizes (object handle)
		{
			NSImage img = (NSImage)handle;
			return img.Size.Width == 0 && img.Size.Height == 0;
		}
		
		public override Size GetSize (object handle)
		{
			NSImage img = (NSImage)handle;
			return new Size ((int)img.Size.Width, (int)img.Size.Height);
		}
		
		public override object CopyBitmap (object handle)
		{
			return ((NSImage)handle).Copy ();
		}
		
		public override void CopyBitmapArea (object backend, int srcX, int srcY, int width, int height, object dest, int destX, int destY)
		{
			throw new NotImplementedException ();
		}
		
		public override object CropBitmap (object backend, int srcX, int srcY, int width, int height)
		{
			throw new NotImplementedException ();
		}
		
		static NSImage FromResource (string res)
		{
			var stream = typeof(ImageHandler).Assembly.GetManifestResourceStream (res);
			using (stream)
			using (NSData data = NSData.FromStream (stream)) {
				return new NSImage (data);
			}
		}

		static NSImage NSImageFromResource (string id)
		{
			return (NSImage) Toolkit.GetBackend (Xwt.Drawing.Image.FromResource (typeof(ImageHandler), id));
		}
		
		static NSImage LoadStockIcon (string id)
		{
			switch (id) {
			case StockIconId.ZoomIn: return NSImageFromResource ("zoom-in.png");
			case StockIconId.ZoomOut: return NSImageFromResource ("zoom-out.png");
			}

			NSImage image = null;
			IntPtr iconRef;
			var type = Util.ToIconType (id);
			if (type != 0 && GetIconRef (-32768/*kOnSystemDisk*/, 1835098995/*kSystemIconsCreator*/, type, out iconRef) == 0) {
				try {
					image = new NSImage (Messaging.IntPtr_objc_msgSend_IntPtr (Messaging.IntPtr_objc_msgSend (cls_NSImage, sel_alloc), sel_initWithIconRef, iconRef));
					// NSImage (IntPtr) ctor retains, but since it is the sole owner, we don't want that
					Messaging.void_objc_msgSend (image.Handle, sel_release);
				} finally {
					ReleaseIconRef (iconRef);
				}
			}

			return image;
		}

		[DllImport ("/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/LaunchServices")]
		static extern int GetIconRef (short vRefNum, int creator, int iconType, out IntPtr iconRef);
		[DllImport ("/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/LaunchServices")]
		static extern int ReleaseIconRef (IntPtr iconRef);
	}


	public class CustomImage: NSImage
	{
		ImageDrawCallback drawCallback;
		ApplicationContext actx;
		NSCustomImageRep imgRep;

		public CustomImage (ApplicationContext actx, ImageDrawCallback drawCallback)
		{
			this.actx = actx;
			this.drawCallback = drawCallback;
			imgRep = new NSCustomImageRep (new Selector ("drawIt:"), this);
			AddRepresentation (imgRep);
		}

		[Export ("drawIt:")]
		public void DrawIt (NSObject ob)
		{
			CGContext ctx = NSGraphicsContext.CurrentContext.GraphicsPort;
			DrawInContext (ctx);
		}

		internal void DrawInContext (CGContext ctx)
		{
			var backend = new CGContextBackend {
				Context = ctx,
				InverseViewTransform = ctx.GetCTM ().Invert ()
			};
			DrawInContext (backend);
		}

		internal void DrawInContext (CGContextBackend ctx)
		{
			bool empty = ctx.Size.Width<=0 && ctx.Size.Height<=0;
			var s = empty ? ctx.Size : Size;
			actx.InvokeUserCode (delegate {
				drawCallback (ctx, new Rectangle (0, 0, (int)s.Width, (int)s.Height));
			});
		}

		public override CGImage AsCGImage (ref NSRect proposedDestRect, NSGraphicsContext referenceContext, NSDictionary hints)
		{
			return base.AsCGImage (ref proposedDestRect, referenceContext, hints);
		}

		public CustomImage Clone ()
		{
			return new CustomImage (actx, drawCallback);
		}
	}
}

