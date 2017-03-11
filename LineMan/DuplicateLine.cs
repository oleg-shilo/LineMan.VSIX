using System;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;

namespace OlegShilo.LineMan
{
    class DuplicateLineVSX
    {
        IVsTextManager txtMgr;

        public DuplicateLineVSX(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute()
        {
            IWpfTextView textView = GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            string sourceText = "";
            int selectionLastLineNumber = 0;

            int caretLineOffset = 0;
            int insertionPosition = 0;
            int selectionStartOffset = 0;
            bool noInitialSelection = false;

            if (!textView.Selection.IsEmpty)
            {
                string textOffset = "";

                if (textView.Selection.Start < textView.Selection.End)
                {
                    caretLineOffset = textView.GetCaretPosition() - textView.Selection.Start.Position.GetContainingLine().Start.Position;
                    selectionStartOffset = textView.Selection.Start.Position - textView.Selection.Start.Position.GetContainingLine().Start.Position;
                    textOffset = textView.GetText().Substring(textView.Selection.Start.Position.GetContainingLine().Start.Position, selectionStartOffset);
                }
                else
                {
                    caretLineOffset = textView.GetCaretPosition() - textView.Selection.End.Position.GetContainingLine().Start.Position;
                    selectionStartOffset = textView.Selection.End.Position - textView.Selection.End.Position.GetContainingLine().Start.Position;
                    textOffset = textView.GetText().Substring(textView.Selection.End.Position.GetContainingLine().Start.Position, selectionStartOffset);
                }

                textOffset = textOffset.ToWhiteSpaceString();
                selectionLastLineNumber = textView.Selection.End.Position.GetContainingLine().LineNumber;

                sourceText = textOffset + textView.TextBuffer.CurrentSnapshot.GetText(textView.Selection.Start.Position, textView.Selection.End.Position - textView.Selection.Start.Position) + "\r\n";

                textView.Selection.Clear();
            }
            else
            {
                noInitialSelection = true;
                selectionStartOffset = 0;
                selectionLastLineNumber = textView.Caret.ContainingTextViewLine.End.GetContainingLine().LineNumber;
                caretLineOffset = textView.GetCaretPosition() - textView.Caret.ContainingTextViewLine.Start.Position;

                sourceText = textView.Caret.ContainingTextViewLine.ExtentIncludingLineBreak.GetText();
            }

            try
            {
                ITextSnapshotLine nextLine = textView.GetLine(selectionLastLineNumber + 1);
                insertionPosition = nextLine.Start.Position;
            }
            catch
            {
                textView.Insert(textView.TextSnapshot.Length, "\r\n");
                insertionPosition = textView.TextSnapshot.Length;
            }

            textView.Insert(insertionPosition, sourceText);

            if (!noInitialSelection)
                textView.SetSelection(insertionPosition + selectionStartOffset, sourceText.Length - selectionStartOffset - 2); //all insertion s end with "\r\n" so exclude it from selection
            textView.MoveCaretTo(insertionPosition + caretLineOffset);
        }

        IWpfTextView GetTextView()
        {
            return GetViewHost().TextView;
        }

        IWpfTextViewHost GetViewHost()
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost)holder;
        }

        IVsUserData GetUserData()
        {
            int mustHaveFocus = 1;//means true
            IVsTextView currentTextView;
            txtMgr.GetActiveView(mustHaveFocus, null, out currentTextView);

            if (currentTextView is IVsUserData)
                return currentTextView as IVsUserData;
            else
                throw new ApplicationException("No text view is currently open");
            // Console.WriteLine("No text view is currently open"); return;
        }
    }

    static class Extensions
    {
        public static void SetSelection(this IWpfTextView obj, int start, int length)
        {
            SnapshotPoint selectionStart = new SnapshotPoint(obj.TextSnapshot, start);
            var selectionSpan = new SnapshotSpan(selectionStart, length);

            obj.Selection.Select(selectionSpan, false);
        }

        public static void MoveCaretTo(this IWpfTextView obj, int position)
        {
            obj.Caret.MoveTo(new SnapshotPoint(obj.TextSnapshot, position));
        }

        public static ITextSnapshotLine GetLine(this IWpfTextView obj, int lineNumber)
        {
            return obj.TextSnapshot.GetLineFromLineNumber(lineNumber);
        }

        public static ITextSnapshotLine GetLineFromPosition(this IWpfTextView obj, int position)
        {
            return obj.TextSnapshot.GetLineFromPosition(position);
        }

        public static int GetCaretPosition(this IWpfTextView obj)
        {
            return obj.Caret.Position.BufferPosition;
        }

        public static bool IsCaretClosToHorizontalEdge(this IWpfTextView textView, bool top)
        {
            var caretPos = textView.Caret.Position.BufferPosition;
            var charBounds = textView.GetTextViewLineContainingBufferPosition(caretPos)
                                     .GetCharacterBounds(caretPos);

            if (top)
                return (charBounds.Top - textView.ViewportTop) < 50;
            else
                return (textView.ViewportBottom - charBounds.Bottom) < 50;
        }

        public static ITextViewLine GetCaretLine(this IWpfTextView obj)
        {
            return obj.Caret.ContainingTextViewLine;
        }

        public static void Insert(this IWpfTextView obj, int position, string text)
        {
            ITextEdit edit = obj.TextSnapshot.TextBuffer.CreateEdit();
            edit.Insert(position, text);
            edit.Apply();
        }

        public static string GetText(this IWpfTextView obj)
        {
            return obj.TextSnapshot.GetText();
        }

        public static string ToWhiteSpaceString(this string obj)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in obj.ToCharArray())
                if (c == '\n' || c == '\r' || c == '\t' || c == ' ')
                    sb.Append(c);
                else
                    sb.Append(' ');

            return sb.ToString();
        }
    }
}