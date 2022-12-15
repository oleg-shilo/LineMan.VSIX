using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using OlegShilo.LineMan;
using Task = System.Threading.Tasks.Task;

namespace LineMan
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class LineManCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        public const int cmdidDuplicateLine = 0x100;
        public const int cmdidDeleteLine = 0x101;
        public const int cmdidLineUp = 0x102;
        public const int cmdidLineDown = 0x103;
        public const int cmdidToggleComments = 0x104;
        public const int cmdidCommentDuplicateLine = 0x105;
        public const int cmdidDuplicateLineAbove = 0x106;
        public const int cmdidJoinLines = 0x107;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5feaef3a-1a8e-423d-bf4c-a1773a7bf4ec");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineManCommand"/> class. Adds our command
        /// handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private LineManCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            //var menuCommandID = new CommandID(CommandSet, CommandId);
            //var menuItem = new MenuCommand(this.Execute, menuCommandID);
            //commandService.AddCommand(menuItem);

            void register(int commandId, Action<IVsTextManager> action)
            {
                var menuCommandID = new CommandID(CommandSet, commandId);
                var menuItem = new MenuCommand((s, e) => ExecutePlugin(action), menuCommandID);
                commandService.AddCommand(menuItem);
            }

            register(cmdidDuplicateLine, txtxMgr => new DuplicateLineVSX(txtxMgr).Execute(commentOriginal: false, forceAbovePlacement: false));
            register(cmdidDuplicateLineAbove, txtxMgr => new DuplicateLineVSX(txtxMgr).Execute(commentOriginal: false, forceAbovePlacement: true));
            register(cmdidCommentDuplicateLine, txtxMgr => new DuplicateLineVSX(txtxMgr).Execute(commentOriginal: true, forceAbovePlacement: false));
            register(cmdidDeleteLine, txtxMgr => new DeleteLineVSX(txtxMgr).Execute());
            register(cmdidLineUp, txtxMgr => new MoveLineVSX(txtxMgr).Execute(true));
            register(cmdidLineDown, txtxMgr => new MoveLineVSX(txtxMgr).Execute(false));
            register(cmdidToggleComments, txtxMgr => new ToggleCommenting(txtxMgr).Execute());
            register(cmdidJoinLines, txtxMgr => new JoinLinesVSX(txtxMgr).Execute());

            Options.Instance.LoadSettingsFromStorage();
        }

        private void ExecutePlugin(Action<IVsTextManager> action)
        {
            var txtxMgr = (IVsTextManager)LineManPackage.GetService(typeof(SVsTextManager));

            try
            {
                action(txtxMgr);
            }
            catch (Exception ex)
            {
                LineManPackage.MessageBoxShow(ex.Message);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static LineManCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in LineManCommand's constructor
            // requires the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new LineManCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "LineManCommand";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}