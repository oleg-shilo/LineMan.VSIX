using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using LineMan;
using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace OlegShilo.LineMan
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the IVsPackage interface and register itself with the shell. This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it: it derives from
    /// the Package class that provides the implementation of the IVsPackage interface and uses the
    /// registration attributes defined in the framework to register itself and its components with
    /// the shell. These attributes tell the pkgdef creation utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset
    /// Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(LineManPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionPageGrid), "LineMan", "Options", 0, 0, true)]
    public sealed class LineManPackage : AsyncPackage
    {
        static public Func<Type, object> GetService;

        public LineManPackage()
        {
            LineManPackage.GetService = base.GetService;
        }

        /// <summary>
        /// LineManPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "05ba352a-dc04-46b4-bbd2-0c8b6459ebeb";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token to monitor for initialization cancellation, which can occur when VS
        /// is shutting down.
        /// </param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>
        /// A task representing the async work of package initialization, or an already completed
        /// task if there is none. Do not return null from this method.
        /// </returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at
            // this point. Do any initialization that requires the UI thread after switching to the
            // UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await LineManCommand.InitializeAsync(this);
        }

        public static void MessageBoxShow(string message)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                      0,
                      ref clsid,
                      "LineMan",
                      message,
                      string.Empty,
                      0,
                      OLEMSGBUTTON.OLEMSGBUTTON_OK,
                      OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                      OLEMSGICON.OLEMSGICON_INFO,
                      0,        // false
                      out result));
        }

        #endregion Package Members
    }

    static class OptionsStorage
    {
        static string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LineMan.settings");

        // VS does not load settings until the options dialog is opened.
        // interestingly enough `LoadSettingsFromStorage`does not read the same data that is loaded/saved from options dialog
        public static OptionPageGrid Load(this OptionPageGrid options)
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    var lines = File.ReadAllLines(settingsFile);
                    options.MultiLineSelectionOnly = bool.Parse(lines[0]);

                    options.DuplicationPlacement = (OptionPageGrid.Placement)Enum.Parse(typeof(OptionPageGrid.Placement), lines[1]);
                }
            }
            catch { }
            return options;
        }

        public static OptionPageGrid Save(this OptionPageGrid options)
        {
            try
            {
                var lines = new StringBuilder();
                lines.AppendLine(options.MultiLineSelectionOnly.ToString());
                lines.AppendLine(options.DuplicationPlacement.ToString());
                File.WriteAllText(settingsFile, lines.ToString());
            }
            catch { }
            return options;
        }
    }

    public class OptionPageGrid : DialogPage
    {
        public OptionPageGrid()
        {
            this.Load();
        }

        static OptionPageGrid()
        {
            Instance = new OptionPageGrid();
        }

        static public OptionPageGrid Instance;

        public enum Placement
        {
            Below,
            Above
        }

        public bool MultiLineSelectionOnly = false;
        public Placement DuplicationPlacement = Placement.Below;

        [Category("Duplication options")]
        [DisplayName("Selection Only")]
        [Description("Duplicate only the selection content. Otherwise whole line.")]
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
    }
}