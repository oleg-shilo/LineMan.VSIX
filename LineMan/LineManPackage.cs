using LineMan;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;

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
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options), "LineMan", "Options", 0, 0, true)]
    public sealed class LineManPackage : AsyncPackage
    {
        static public new Func<Type, object> GetService;

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


            // In the current hosting model of VS auto-loading is problematic
            // Thus we may not be subscribe for OnSave event until the extension loading is triggered by the first use of the
            // extension functionality
            // https://learn.microsoft.com/en-us/visualstudio/extensibility/loading-vspackages?view=vs-2022
            // Note at least in VS v17.4.2 'UIContextGuids80.SolutionExists' does not work.
            var runningDocumentTable = new RunningDocumentTable(this);
            runningDocumentTable.Advise(new FormatDocumentOnBeforeSave());
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

    internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
    {
        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var command in (Options.Instance.ExecuteOnSave ?? "").Split(','))
                try
                {
                    // Global.GetDTE2().ExecuteCommand("Edit.FormatDocument", String.Empty);
                    // may need to run it from different thread otherwise the command is not
                    // executed (e.g. immediately after `ITextEdit.Apply(...)`)

                    Task.Run(() =>
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                            Global.GetDTE2().ExecuteCommand(command, String.Empty)));
                }
                catch { }

            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnAfterSave(uint docCookie) => VSConstants.S_OK;

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;

        public void OnAfterDocumentLockCountChanged(uint docCookie, uint dwRDTLockType, uint dwOldLockCount, uint dwNewLockCount)
        { }
    }
}