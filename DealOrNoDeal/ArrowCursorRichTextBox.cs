using System;
using System.Windows.Forms;

namespace DealOrNoDeal
{
    /// <summary>
    /// A RichTextBox that always shows the normal arrow cursor. The native
    /// RichEdit control handles WM_SETCURSOR itself and sets its own I-beam
    /// cursor over text - setting Control.Cursor (or re-forcing it on
    /// MouseMove) doesn't stop that and just fights it, message by message,
    /// which is what caused the cursor to flicker. Intercepting
    /// WM_SETCURSOR here and never forwarding it to the native control is
    /// the only way to actually prevent it from setting a cursor at all.
    /// </summary>
    internal sealed class ArrowCursorRichTextBox : RichTextBox
    {
        private const int WM_SETCURSOR = 0x0020;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SETCURSOR)
            {
                Cursor.Current = Cursors.Default;
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
