using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;

#if !HAVE_NATIVE_TYPES
using nint = global::System.Int32;
using nuint = global::System.UInt32;
using nfloat = global::System.Single;

using CGSize = global::System.Drawing.SizeF;
using CGPoint = global::System.Drawing.PointF;
using CGRect = global::System.Drawing.RectangleF;
#endif

namespace MonoTouch.Dialog {

	public class MessageSummaryView : UIView {
		static UIFont SenderFont = UIFont.BoldSystemFontOfSize (19);
		static UIFont SubjectFont = UIFont.SystemFontOfSize (14);
		static UIFont TextFont = UIFont.SystemFontOfSize (13);
		static UIFont CountFont = UIFont.BoldSystemFontOfSize (13);
		public string Sender { get; private set; }
		public string Body { get; private set; }
		public string Subject { get; private set; }
		public DateTime Date { get; private set; }
		public bool NewFlag  { get; private set; }
		public int MessageCount  { get; private set; }
		
		static CGGradient gradient;
		
		static MessageSummaryView ()
		{
			using (var colorspace = CGColorSpace.CreateDeviceRGB ()){
#if ARCH_32 || HAVE_NATIVE_TYPES
				gradient = new CGGradient (colorspace, new nfloat [] { /* first */ .52f, .69f, .96f, 1, /* second */ .12f, .31f, .67f, 1 }, null); //new float [] { 0, 1 });
#else
				gradient = new CGGradient (colorspace, new double [] { /* first */ .52f, .69f, .96f, 1, /* second */ .12f, .31f, .67f, 1 }, null); //new float [] { 0, 1 });
#endif
			}
		}
		
		public MessageSummaryView ()
		{
			BackgroundColor = UIColor.White;
		}
		
		public void Update (string sender, string body, string subject, DateTime date, bool newFlag, int messageCount)
		{
			Sender = sender;
			Body = body;
			Subject = subject;
			Date = date;
			NewFlag = newFlag;
			MessageCount = messageCount;
			SetNeedsDisplay ();
		}
		
		public override void Draw (CGRect rect)
		{
			const int padright = 21;
			var ctx = UIGraphics.GetCurrentContext ();
			nfloat boxWidth;
			CGSize ssize;
			
			if (MessageCount > 0){
				var ms = MessageCount.ToString ();
				ssize = StringSize (ms, CountFont);
				boxWidth = (nfloat)Math.Min (22 + ssize.Width, 18);
				var crect = new CGRect (Bounds.Width-20-boxWidth, 32, boxWidth, 16);
				
				UIColor.Gray.SetFill ();
				GraphicsUtil.FillRoundedRect (ctx, crect, 3);
				UIColor.White.SetColor ();
				crect.X += 5;
				DrawString (ms, crect, CountFont);
				
				boxWidth += padright;
			} else
				boxWidth = 0;
			
			UIColor.FromRGB (36, 112, 216).SetColor ();
			var diff = DateTime.Now - Date;
			string label;
			if (DateTime.Now.Day == Date.Day)
				label = Date.ToShortTimeString ();
			else if (diff <= TimeSpan.FromHours (24))
				label = "Yesterday".GetText ();
			else if (diff < TimeSpan.FromDays (6))
				label = Date.ToString ("dddd");
			else
				label = Date.ToShortDateString ();
			ssize = StringSize (label, SubjectFont);
			nfloat dateSize = ssize.Width + padright + 5;
			DrawString (label, new CGRect (Bounds.Width-dateSize, 6, dateSize, 14), SubjectFont, UILineBreakMode.Clip, UITextAlignment.Left);
			
			const int offset = 33;
			nfloat bw = Bounds.Width-offset;
			
			UIColor.Black.SetColor ();
			DrawString (Sender, new CGPoint (offset, 2), (float)(bw-dateSize), SenderFont, UILineBreakMode.TailTruncation);
			DrawString (Subject, new CGPoint (offset, 23), (float)(bw-offset-boxWidth), SubjectFont, UILineBreakMode.TailTruncation);
			
			//UIColor.Black.SetFill ();
			//ctx.FillRect (new CGRect (offset, 40, bw-boxWidth, 34));
			UIColor.Gray.SetColor ();
			DrawString (Body, new CGRect (offset, 40, bw-boxWidth, 34), TextFont, UILineBreakMode.TailTruncation, UITextAlignment.Left);
			
			if (NewFlag){
				ctx.SaveState ();
				ctx.AddEllipseInRect (new CGRect (10, 32, 12, 12));
				ctx.Clip ();
				ctx.DrawLinearGradient (gradient, new CGPoint (10, 32), new CGPoint (22, 44), CGGradientDrawingOptions.DrawsAfterEndLocation);
				ctx.RestoreState ();
			}
			
#if WANT_SHADOWS
			ctx.SaveState ();
			UIColor.FromRGB (78, 122, 198).SetStroke ();
			ctx.SetShadow (new CGSize (1, 1), 3);
			ctx.StrokeEllipseInRect (new CGRect (10, 32, 12, 12));
			ctx.RestoreState ();
#endif
		}
	}
		
	public class MessageElement : Element, IElementSizing {
		static NSString mKey = new NSString ("MessageElement");
		
		public string Sender, Body, Subject;
		public DateTime Date;
		public bool NewFlag;
		public int MessageCount;
		
		class MessageCell : UITableViewCell {
			MessageSummaryView view;
			
			public MessageCell () : base (UITableViewCellStyle.Default, mKey)
			{
				view = new MessageSummaryView ();
				ContentView.Add (view);
				Accessory = UITableViewCellAccessory.DisclosureIndicator;
			}
			
			public void Update (MessageElement me)
			{
				view.Update (me.Sender, me.Body, me.Subject, me.Date, me.NewFlag, me.MessageCount);
			}
			
			public override void LayoutSubviews ()
			{
				base.LayoutSubviews ();
				view.Frame = ContentView.Bounds;
				view.SetNeedsDisplay ();
			}
		}
		
		public MessageElement () : base ("")
		{
		}
		
		public MessageElement (Action<DialogViewController,UITableView,NSIndexPath> tapped) : base ("")
		{
			Tapped += tapped;
		}
		
		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (mKey) as MessageCell;
			if (cell == null)
				cell = new MessageCell ();
			cell.Update (this);
			return cell;
		}
		
		public nfloat GetHeight (UITableView tableView, NSIndexPath indexPath)
		{
			return 78;
		}
		
		public event Action<DialogViewController, UITableView, NSIndexPath> Tapped;
		
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			if (Tapped != null)
				Tapped (dvc, tableView, path);
		}
	}
}

