using System;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;
using EnvDTE;

namespace OlegShilo.LineMan
{
    class ToggleCommenting
    {
        IVsTextManager txtMgr;

        public ToggleCommenting(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute()
        {
            IWpfTextView textView = txtMgr.GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            Extensions.RefreshCommentForCurrentDocument();

            if (!textView.Selection.IsEmpty)
            {
                int areaStart = textView.Selection.Start.Position.GetContainingLine().Start.Position;
                int areaEnd = textView.Selection.End.Position.GetContainingLine().End.Position;

                if (textView.Selection.Start > textView.Selection.End)
                    Extensions.Swap(ref areaStart, ref areaEnd);

                string text = textView.TextBuffer.CurrentSnapshot.GetText(areaStart, areaEnd - areaStart);

                var textLines = text.Replace(Environment.NewLine, "\n").Split('\n')
                                    .Select(x => new
                                    {
                                        Text = x,
                                        IsCommented = x.TrimStart().StartsWith(Extensions.commentPreffix),
                                        TextStart = x.IndexOfNonWhitespace(),
                                        IsEmpty = (x == "")
                                    });

                bool doComment = textLines.Any(x => !x.IsCommented && !x.IsEmpty);
                int indent = textLines.Where(x => !x.IsEmpty).Min(x => x.TextStart);

                string[] replacementLines = textLines.Select(x =>
                                            {
                                                if (x.IsEmpty)
                                                    return x.Text;
                                                else
                                                    return x.Text.CommentText(indent, doComment);
                                            }).ToArray();

                string replacementText = string.Join(Environment.NewLine, replacementLines);

                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(areaStart, areaEnd - areaStart), replacementText);
                    edit.Apply();
                }

                textView.Selection.Clear();
                textView.SetSelection(areaStart, replacementText.Length);
            }
            else
            {
                int lineNum = textView.Caret.ContainingTextViewLine.End.GetContainingLine().LineNumber;
                var line = textView.GetLine(lineNum);

                int caretLineOffset = textView.GetCaretPosition() - textView.Caret.ContainingTextViewLine.Start.Position;

                using (ITextEdit edit = textView.TextBuffer.CreateEdit())
                {
                    string lineText = line.GetText();
                    bool doComment = !lineText.IsCommented();
                    int indent = lineText.IndexOfNonWhitespace();

                    if (caretLineOffset > indent)
                    {
                        if (doComment)
                            caretLineOffset += Extensions.commentPreffix.Length;
                        else
                            caretLineOffset -= Extensions.commentPreffix.Length;
                    }

                    string text = line.GetTextIncludingLineBreak().CommentText(indent, doComment);
                    edit.Replace(new Span(line.Start.Position, line.LengthIncludingLineBreak), text);
                    edit.Apply();
                }

                textView.MoveCaretTo(line.Start.Position + caretLineOffset);
            }
        }
    }
}