using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using EnvDTE80;
using LineMan;

namespace OlegShilo.LineMan
{
    class Global
    {
        public static DTE2 GetDTE2()
        {
            DTE dte = (DTE)LineManPackage.GetService(typeof(DTE));
            DTE2 dte2 = dte as DTE2;

            if (dte2 == null)
            {
                return null;
            }

            return dte2;
        }
    }

    static class Extensions
    {
        internal static void RefreshCommentForCurrentDocument()
        {
            try
            {
                var dte = Global.GetDTE2();
                var textDocument = dte.ActiveDocument.Object("TextDocument") as TextDocument;

                switch (textDocument.Language)
                {
                    case "CSharp":
                    case "C/C++":
                    case "TypeScript":
                    case "JavaScript":
                    case "F#":
                        {
                            commentPreffix = "// ";
                            break;
                        }
                    case "Python":
                    case "PowerShell":
                        {
                            commentPreffix = "# ";
                            break;
                        }
                    case "SQL Server Tools":
                        {
                            commentPreffix = "-- ";
                            break;
                        }
                    case "Basic":
                        {
                            commentPreffix = "' ";
                            break;
                        }
                    default:
                        {
                            commentPreffix = "// ";
                            break;
                        }
                }
            }
            catch
            {
                commentPreffix = "// ";
            }
        }

        internal static string commentPreffix = "// ";

        static public void Swap<T>(ref T a, ref T b)
        {
            T temp = b;
            b = a;
            a = temp;
        }

        static public string Comment(this string text)
        {
            var textLines = text.Replace(Environment.NewLine, "\n").Split('\n')
                                .Select(x => new
                                {
                                    Text = x,
                                    TextStart = x.IndexOfNonWhitespace(),
                                    IsEmpty = (x == "")
                                });

            int indent = textLines.Where(x => !x.IsEmpty).Min(x => x.TextStart);
            string[] replacementLines = textLines.Select(x =>
            {
                if (x.IsEmpty)
                    return x.Text;
                else
                    return x.Text.CommentText(indent, true);
            }).ToArray();

            var commentedText = string.Join(Environment.NewLine, replacementLines);
            return commentedText;
        }

        static public int IndexOfNonWhitespace(this string text)
        {
            return text.TakeWhile(c => char.IsWhiteSpace(c)).Count();
        }

        static public bool IsCommented(this string text)
        {
            return text.TrimStart().StartsWith(commentPreffix);
        }

        static public string TrimCommentPreffix(this string text)
        {
            if (text.StartsWith(commentPreffix))
                return text.Substring(commentPreffix.Length);
            else
                return text;
        }

        static public string CommentText(this string text, int indent, bool doComment)
        {
            int textStart = text.IndexOfNonWhitespace();

            if (doComment)
            {
                if (textStart < indent)
                    return new string(' ', indent - textStart) + commentPreffix + text.Substring(textStart);
                else
                    return text.Substring(0, indent) + commentPreffix + text.Substring(indent);
            }
            else
            {
                return text.Substring(0, textStart) + text.Substring(textStart).TrimCommentPreffix();
            }
        }

        static public IWpfTextView GetTextView(this IVsTextManager txtMgr)
        {
            return txtMgr.GetViewHost().TextView;
        }

        static public IWpfTextViewHost GetViewHost(this IVsTextManager txtMgr)
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            txtMgr.GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost)holder;
        }

        public static IVsUserData GetUserData(this IVsTextManager txtMgr)
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