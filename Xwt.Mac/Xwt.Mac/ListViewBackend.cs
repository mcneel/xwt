// 
// ListViewBackend.cs
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
using Xwt.Backends;
using System.Collections.Generic;
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
	public class ListViewBackend: TableViewBackend<NSTableView, IListViewEventSink>, IListViewBackend
	{
		IListDataSource source;
		ListSource tsource;

		protected override NSTableView CreateView ()
		{
			return new NSTableView ();
		}

		protected override string SelectionChangeEventName {
			get { return "NSTableViewSelectionDidChangeNotification"; }
		}
		
		public void SetSource (IListDataSource source, IBackend sourceBackend)
		{
			this.source = source;
			tsource = new ListSource (source);
			Table.DataSource = tsource;
		}
		
		public int[] SelectedRows {
			get {
				int[] sel = new int [Table.SelectedRowCount];
				int i = 0;
				foreach (int r in Table.SelectedRows)
					sel [i++] = r;
				return sel;
			}
		}
		
		public void SelectRow (int pos)
		{
			Table.SelectRow ((ulong)pos, false);
		}
		
		public void UnselectRow (int pos)
		{
			Table.DeselectRow (pos);
		}
		
		public override object GetValue (object pos, int nField)
		{
			return source.GetValue ((int)pos, nField);
		}

		// TODO
		public bool BorderVisible { get; set; }
	}
	
	class TableRow: NSObject, ITablePosition
	{
		public int Row;
		
		public object Position {
			get { return Row; }
		}
	}
	
	class ListSource: NSTableViewDataSource
	{
		IListDataSource source;
		
		public ListSource (IListDataSource source)
		{
			this.source = source;
		}

		public override bool AcceptDrop (NSTableView tableView, NSDraggingInfo info, NSInteger row, NSTableViewDropOperation dropOperation)
		{
			return false;
		}

		public override string[] FilesDropped (NSTableView tableView, MonoMac.Foundation.NSUrl dropDestination, MonoMac.Foundation.NSIndexSet indexSet)
		{
			return new string [0];
		}

		public override MonoMac.Foundation.NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, NSInteger row)
		{
			return NSObject.FromObject (row);
		}

		public override NSInteger GetRowCount (NSTableView tableView)
		{
			return source.RowCount;
		}

		public override void SetObjectValue (NSTableView tableView, MonoMac.Foundation.NSObject theObject, NSTableColumn tableColumn, NSInteger row)
		{
		}

		public override void SortDescriptorsChanged (NSTableView tableView, MonoMac.Foundation.NSSortDescriptor[] oldDescriptors)
		{
		}

		public override NSDragOperation ValidateDrop (NSTableView tableView, NSDraggingInfo info, NSInteger row, NSTableViewDropOperation dropOperation)
		{
			return NSDragOperation.None;
		}

		public override bool WriteRows (NSTableView tableView, MonoMac.Foundation.NSIndexSet rowIndexes, NSPasteboard pboard)
		{
			return false;
		}
	}
}

