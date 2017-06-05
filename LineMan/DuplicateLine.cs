using System;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;

namespace OlegShilo.LineMan
{
    class DuplicateLineVSX
    {
        IVsTextManager txtMgr;

        public DuplicateLineVSX(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute(bool commentOriginal = false)
        {
            IWpfTextView textView = txtMgr.GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            Extensions.RefreshCommentForCurrentDocument();

            if (!textView.Selection.IsEmpty)
            {
                var selectionStart = textView.Selection.Start;
                var selectionEnd = textView.Selection.End;

                if (textView.Selection.Start > textView.Selection.End)
                    Extensions.Swap(ref selectionStart, ref selectionEnd);

                var selectionStartLine = selectionStart.Position.GetContainingLine();
                var selectionEndLine = selectionEnd.Position.GetContainingLine();

                var blockTextStart = selectionStartLine.Start.Position;
                string blockText = textView.TextBuffer.CurrentSnapshot.GetText(selectionStartLine.Start.Position,
                                                                               selectionEndLine.End.Position - selectionStartLine.Start.Position);

                string selectedText = textView.TextBuffer.CurrentSnapshot.GetText(selectionStart.Position,
                                                                                  selectionEnd.Position - selectionStart.Position);

                var caretPositionWithinBlock = textView.GetCaretPosition() - selectionStartLine.Start.Position;
                var nonSelectedTextLeftOffset = selectionStart.Position - selectionStartLine.Start.Position;

                string textOffset = new string(' ', nonSelectedTextLeftOffset);
                string duplicatedText = textOffset + selectedText;

                string replacementText;

                if (commentOriginal)
                {
                    var commentedText = blockText.Comment();

                    replacementText = commentedText + Environment.NewLine + duplicatedText;
                }
                else
                {
                    replacementText = blockText + Environment.NewLine + duplicatedText;
                }

                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(blockTextStart, blockText.Length), replacementText);
                    edit.Apply();
                }

                var firstDuplicatedTextLine = textView.GetLine(selectionEndLine.LineNumber + 1);

                int newSelectionStart = firstDuplicatedTextLine.Start.Position + nonSelectedTextLeftOffset;
                int newSelectionLength = Math.Min(selectedText.Length, textView.GetText().Length - newSelectionStart);

                textView.Selection.Clear();
                textView.SetSelection(newSelectionStart, newSelectionLength);

                textView.MoveCaretTo(firstDuplicatedTextLine.Start.Position + caretPositionWithinBlock);
            }
            else
            {
                int selectionLastLineNumber = textView.Caret.ContainingTextViewLine.End.GetContainingLine().LineNumber;
                int caretLineOffset = textView.GetCaretPosition() - textView.Caret.ContainingTextViewLine.Start.Position;

                int areaStart = textView.Caret.ContainingTextViewLine.Start.Position;
                int areaEnd = textView.Caret.ContainingTextViewLine.End.Position;

                string text = textView.TextBuffer.CurrentSnapshot.GetText(areaStart, areaEnd - areaStart);
                string replacementText;

                if (commentOriginal)
                    replacementText = text.CommentText(text.IndexOfNonWhitespace(), true) + Environment.NewLine + text;
                else
                    replacementText = text + Environment.NewLine + text;

                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(areaStart, text.Length), replacementText);
                    edit.Apply();
                }

                var line = textView.GetLine(selectionLastLineNumber + 1);

                textView.MoveCaretTo(line.Start.Position + caretLineOffset);
                return;
            }
        }
    }
}