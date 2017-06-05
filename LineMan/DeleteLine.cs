using System;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Formatting;

namespace OlegShilo.LineMan
{
    class DeleteLineVSX
    {
        IVsTextManager txtMgr;

        public DeleteLineVSX(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute()
        {
            IWpfTextView textView = txtMgr.GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            int selectionLastLineNumber = 0;
            int selectionFirstLineNumber = 0;

            if (!textView.Selection.IsEmpty)
            {
                selectionLastLineNumber = textView.Selection.End.Position.GetContainingLine().LineNumber;
                selectionFirstLineNumber = textView.Selection.Start.Position.GetContainingLine().LineNumber;
            }
            else
            {
                selectionFirstLineNumber =
                selectionLastLineNumber = textView.GetCaretLine().End.GetContainingLine().LineNumber;
            }

            textView.Selection.Clear();

            try
            {
                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    for (int i = selectionLastLineNumber; i >= selectionFirstLineNumber; i--)
                    {
                        var line = textView.GetLine(i);
                        edit.Delete(new Span(line.Start.Position, line.LengthIncludingLineBreak));
                    }
                    edit.Apply();
                }
            }
            catch
            {
            }

            ////ITextSnapshotLine currentLineContent = snapshot.GetLineFromPosition(textView.Caret.Position.BufferPosition);
            //ITextViewLine currentLineContent = textView.Caret.ContainingTextViewLine;

            //double initialStartPosition =  textView.Caret.Left;

            //ITextEdit edit = snapshot.TextBuffer.CreateEdit();
            //edit.Delete(currentLineContent.Start.Position, currentLineContent.LengthIncludingLineBreak);
            //edit.Apply();

            //ITextSnapshotLine newCurrentLineContent = snapshot.GetLineFromPosition(textView.Caret.Position.BufferPosition);

            ////make a new selection
            //ITextViewLine line = textView.Caret.ContainingTextViewLine;
            //textView.Caret.MoveTo(line, initialStartPosition - 1);
        }
    }
}