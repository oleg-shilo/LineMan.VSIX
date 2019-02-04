using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using EnvDTE80;

namespace OlegShilo.LineMan
{
    class Global
    {
        public static Func<Type, object> GetService;

        public static DTE2 GetDTE2()
        {
            DTE dte = (DTE)GetService(typeof(DTE));
            DTE2 dte2 = dte as DTE2;

            if (dte2 == null)
            {
                return null;
            }

            return dte2;
        }
    }

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]

    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLineManPkgString)]
    public sealed class LineManPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public LineManPackage()
        {
            Global.GetService = GetService;
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.

                CommandID menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidDuplicateLine);
                MenuCommand menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new DuplicateLineVSX(txtxMgr).Execute()), menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidCommentDuplicateLine);
                menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new DuplicateLineVSX(txtxMgr).Execute(true)), menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidDeleteLine);
                menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new DeleteLineVSX(txtxMgr).Execute()), menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidLineUp);
                menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new MoveLineVSX(txtxMgr).Execute(true)), menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidLineDown);
                menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new MoveLineVSX(txtxMgr).Execute(false)), menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidLineManCmdSet, (int)PkgCmdIDList.cmdidToggleComments);
                menuItem = new MenuCommand((s, e) => ExecutePlugin(txtxMgr => new ToggleCommenting(txtxMgr).Execute()), menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        #endregion Package Members

        private void ExecutePlugin(Action<IVsTextManager> action)
        {
            var txtxMgr = (IVsTextManager)GetService(typeof(SVsTextManager));

            try
            {
                action(txtxMgr);
            }
            catch (Exception ex)
            {
                MessageBoxShow(ex.Message);
            }
        }

        private void MessageBoxShow(string message)
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
    }
}