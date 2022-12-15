using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace OlegShilo.LineMan
{
    public class Options : DialogPage
    {
        public Options()
        {
            this.Load();
        }

        static Options()
        {
            Instance = new Options();
        }

        static public Options Instance;

        public enum Placement
        {
            Below,
            Above
        }

        public string ExecuteOnSave = "Edit.FormatDocument";
        // public bool FormatOnSave = false;

        public bool MultiLineSelectionOnly = false;
        public Placement DuplicationPlacement = Placement.Below;

        [Category("Duplication options")]
        [DisplayName("Selection Only")]
        [Description("Duplicate only selected part of the text. Otherwise whole line(s).")]
        public bool MultiLineSelectionOnlyProp
        {
            get { return MultiLineSelectionOnly; }
            set { MultiLineSelectionOnly = value; this.Save(); }
        }

        [Category("Duplication options")]
        [DisplayName("Selection Placement")]
        [Description("Set default placement of duplication above the line with the caret.")]
        public Placement DuplicationPlacementProp
        {
            get { return DuplicationPlacement; }
            set { DuplicationPlacement = value; this.Save(); }
        }

        // [Category("Automation options")]
        // [DisplayName("Format On Save")]
        // [Description("Invoke IDE 'Format Document' command on document saving. Works for all document types. " +
        //     "Even for those that are not supported by other 'FormatOnSave' extensions (e.g. CodeMaid and razor files).")]
        // public bool FormatOnSaveProp
        // {
        //     get { return FormatOnSave; }
        //     set { FormatOnSave = value; this.Save(); }
        // }

        [Category("Automation options")]
        [DisplayName("Execute On Save")]
        [Description("Invoke IDE command(s) on document saving. " +
            "You can specify multiple commands (e.g. 'Edit.FormatDocument,CodeMaid.CleanupActiveDocument,Tools.CM+Format').")]
        public string ExecuteOnSaveProp
        {
            get { return ExecuteOnSave; }
            set { ExecuteOnSave = value; this.Save(); }
        }
    }

    static class OptionsStorage
    {
        static string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LineMan.settings");

        // VS does not load settings until the options dialog is opened.
        // interestingly enough `LoadSettingsFromStorage`does not read the same data that is loaded/saved from options dialog
        public static Options Load(this Options options)
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    // JSON serialization is problematic as OptionPageGrid is a self-referencing object
                    var lines = File.ReadAllLines(settingsFile);
                    options.MultiLineSelectionOnly = bool.Parse(lines[0]);
                    options.DuplicationPlacement = (Options.Placement)Enum.Parse(typeof(Options.Placement), lines[1]);
                    options.ExecuteOnSaveProp = lines[2];
                    // options.FormatOnSaveProp = bool.Parse(lines[3]);
                }
            }
            catch { }
            return options;
        }

        public static Options Save(this Options options)
        {
            try
            {
                // JSON serialization is problematic as OptionPageGrid is a self-referencing object
                var lines = new StringBuilder();
                lines.AppendLine(options.MultiLineSelectionOnly.ToString());
                lines.AppendLine(options.DuplicationPlacement.ToString());
                lines.AppendLine(options.ExecuteOnSave);
                // lines.AppendLine(options.FormatOnSave.ToString());
                File.WriteAllText(settingsFile, lines.ToString());
            }
            catch { }
            return options;
        }
    }
}