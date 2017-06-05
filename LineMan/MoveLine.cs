using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;
using System.Text;

namespace OlegShilo.LineMan
{
    internal class MoveLineVSX
    {
        private IVsTextManager txtMgr;

        public MoveLineVSX(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute(bool up)
        {
            IWpfTextView textView = GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            string sourceText = "";
            int selectionLastLineNumber = 0;
            int selectionFirstLineNumber = 0;

            int selectionStartLineOffset = 0;
            int selectionLength = 0;

            int caretLineOffset = 0;

            bool noInitialSelection = false;
            bool caretAtTheEndOfSelection = true;
            int caretPos = textView.GetCaretPosition();

            caretLineOffset = caretPos - textView.GetCaretLine().Start.Position;

            if (!textView.Selection.IsEmpty)
            {
                caretAtTheEndOfSelection = (textView.Selection.Start.Position != caretPos);

                if (textView.Selection.Start.Position < textView.Selection.End.Position)
                {
                    selectionStartLineOffset = textView.Selection.Start.Position - textView.Selection.Start.Position.GetContainingLine().Start.Position;
                    selectionLength = textView.Selection.End.Position - textView.Selection.Start.Position;
                }
                else
                {
                    selectionStartLineOffset = textView.Selection.End.Position - textView.Selection.Start.Position.GetContainingLine().End.Position;
                    selectionLength = textView.Selection.Start.Position - textView.Selection.End.Position;
                }

                selectionLastLineNumber = textView.Selection.End.Position.GetContainingLine().LineNumber;
                selectionFirstLineNumber = textView.Selection.Start.Position.GetContainingLine().LineNumber;

                var builder = new StringBuilder();

                for (int i = selectionFirstLineNumber; i <= selectionLastLineNumber; i++)
                    builder.AppendLine(snapshot.GetLineFromLineNumber(i).GetText());

                sourceText = builder.ToString();

                textView.Selection.Clear();
            }
            else
            {
                noInitialSelection = true;

                selectionLength =
                selectionStartLineOffset = 0;
                selectionFirstLineNumber =
                selectionLastLineNumber = textView.GetCaretLine().End.GetContainingLine().LineNumber;

                sourceText = textView.Caret.ContainingTextViewLine.ExtentIncludingLineBreak.GetText();
            }

            int insertionPosition;
            //int finalCaretPosition;

            using (ITextEdit edit = textView.TextBuffer.CreateEdit())
            {
                int lineCount = selectionLastLineNumber - selectionFirstLineNumber + 1;
                try
                {
                    for (int i = selectionLastLineNumber; i >= selectionFirstLineNumber; i--)
                    {
                        var line = edit.Snapshot.GetLineFromLineNumber(i);
                        edit.Delete(new Span(line.Start.Position, line.LengthIncludingLineBreak));
                    }
                }
                catch
                {
                }

                if (up)
                {
                    if (selectionFirstLineNumber == 0)
                        insertionPosition = 0;
                    else
                        insertionPosition = edit.Snapshot.GetLineFromLineNumber(selectionFirstLineNumber - 1).Start.Position;
                }
                else
                {
                    insertionPosition = edit.Snapshot.GetLineFromLineNumber(selectionFirstLineNumber + lineCount).ExtentIncludingLineBreak.End.Position;
                }

                edit.Insert(insertionPosition, sourceText);
                edit.Apply();
            }

            int selectionStartUp = (noInitialSelection ? insertionPosition + caretLineOffset : insertionPosition + selectionStartLineOffset);
            int selectionStartDown = (noInitialSelection ? insertionPosition - sourceText.Length + caretLineOffset : (insertionPosition - sourceText.Length) + selectionStartLineOffset);

            int selectionStart = (up ? selectionStartUp : selectionStartDown);

            textView.SetSelection(selectionStart, selectionLength);

            if (caretAtTheEndOfSelection)
                textView.MoveCaretTo(selectionStart + selectionLength);
            else
                textView.MoveCaretTo(selectionStart);

            int caretLineNumber = textView.GetLineFromPosition(textView.GetCaretPosition()).LineNumber;

            if (up)
            {
                if ((caretLineNumber - textView.VisualSnapshot.Lines.First().LineNumber) < 5)
                    textView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            }
            else
            {
                if ((textView.VisualSnapshot.Lines.Last().LineNumber - caretLineNumber) < 5)
                    textView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            }

            if (textView.IsCaretClosToHorizontalEdge(up))
                textView.ViewScroller.ScrollViewportVerticallyByLines(up ? ScrollDirection.Up : ScrollDirection.Down, 1);
        }

        public void Execute_Old(bool up) //non-transactional
        {
            IWpfTextView textView = GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            string sourceText = "";
            int selectionLastLineNumber = 0;
            int selectionFirstLineNumber = 0;

            int selectionStartLineOffset = 0;
            int selectionLength = 0;

            int caretLineOffset = 0;

            bool noInitialSelection = false;

            caretLineOffset = textView.GetCaretPosition() - textView.GetCaretLine().Start.Position;

            if (!textView.Selection.IsEmpty)
            {
                if (textView.Selection.Start.Position < textView.Selection.End.Position)
                {
                    selectionStartLineOffset = textView.Selection.Start.Position - textView.Selection.Start.Position.GetContainingLine().Start.Position;
                    selectionLength = textView.Selection.End.Position - textView.Selection.Start.Position;
                }
                else
                {
                    selectionStartLineOffset = textView.Selection.End.Position - textView.Selection.Start.Position.GetContainingLine().End.Position;
                    selectionLength = textView.Selection.Start.Position - textView.Selection.End.Position;
                }

                selectionLastLineNumber = textView.Selection.End.Position.GetContainingLine().LineNumber;
                selectionFirstLineNumber = textView.Selection.Start.Position.GetContainingLine().LineNumber;

                var builder = new StringBuilder();

                for (int i = selectionFirstLineNumber; i <= selectionLastLineNumber; i++)
                    builder.AppendLine(snapshot.GetLineFromLineNumber(i).GetText());

                sourceText = builder.ToString();

                textView.Selection.Clear();
            }
            else
            {
                noInitialSelection = true;

                selectionLength =
                selectionStartLineOffset = 0;
                selectionFirstLineNumber =
                selectionLastLineNumber = textView.GetCaretLine().End.GetContainingLine().LineNumber;

                sourceText = textView.Caret.ContainingTextViewLine.ExtentIncludingLineBreak.GetText();
            }

            {
                int lineCount = selectionLastLineNumber - selectionFirstLineNumber + 1;
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

                int insertionPosition;

                if (up)
                {
                    if (selectionFirstLineNumber == 0)
                        insertionPosition = 0;
                    else
                        insertionPosition = textView.GetLine(selectionFirstLineNumber - 1).Start.Position;
                }
                else
                {
                    insertionPosition = textView.GetLine(selectionFirstLineNumber).ExtentIncludingLineBreak.End.Position;
                }

                textView.Insert(insertionPosition, sourceText);

                if (!noInitialSelection)
                    textView.SetSelection(insertionPosition + selectionStartLineOffset, selectionLength);

                textView.MoveCaretTo(insertionPosition + caretLineOffset);
            }

            int caretLineNumber = textView.GetLineFromPosition(textView.GetCaretPosition()).LineNumber;

            if (up)
            {
                if ((caretLineNumber - textView.VisualSnapshot.Lines.First().LineNumber) < 5)
                    textView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
            }
            else
            {
                if ((textView.VisualSnapshot.Lines.Last().LineNumber - caretLineNumber) < 5)
                    textView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
            }

            if (textView.IsCaretClosToHorizontalEdge(up))
                textView.ViewScroller.ScrollViewportVerticallyByLines(up ? ScrollDirection.Up : ScrollDirection.Down, 1);
        }

        private IWpfTextView GetTextView()
        {
            return GetViewHost().TextView;
        }

        private IWpfTextViewHost GetViewHost()
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost)holder;
        }

        private IVsUserData GetUserData()
        {
            int mustHaveFocus = 1;//means true
            IVsTextView currentTextView;
            txtMgr.GetActiveView(mustHaveFocus, null, out currentTextView);

            if (currentTextView is IVsUserData)
                return currentTextView as IVsUserData;
            else
                throw new ApplicationException("No text view is currently open");
        }
    }
}