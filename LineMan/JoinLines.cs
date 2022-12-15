using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OlegShilo.LineMan
{
    class JoinLinesVSX
    {
        IVsTextManager txtMgr;

        public JoinLinesVSX(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute()
        {
            IWpfTextView textView = txtMgr.GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            textView.Selection.Clear();

            try
            {
                var currLine = textView.GetCaretLine().End.GetContainingLine();
                var nextLine = textView.GetLine(currLine.LineNumber + 1).End.GetContainingLine();

                string currLineText = currLine.GetText().TrimEnd();
                string nextLineText = nextLine.GetText().TrimStart();

                string replacementText = currLineText + " " + nextLineText;

                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(currLine.Start.Position, nextLine.End.Position - currLine.Start.Position), replacementText);
                    edit.Apply();
                }

                textView.MoveCaretTo(textView.GetCaretLine().Start.Position + currLineText.Length);
            }
            catch
            {
            }
        }
    }
}