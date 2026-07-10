using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace mortar.windows
{
    [Guid("8ab0fe32-6cbd-41c1-a370-891f44b6d0ef")]
    public class mortarWindow : ToolWindowPane, IVsSolutionEvents
    {
        private uint _solutionEventsCookie;

        public mortarWindow() : base(null)
        {
            this.Caption = "mortar: a file linking system";
            this.Content = new mortarWindowControl();
        }

        protected override void Initialize()
        {
            base.Initialize();
            ThreadHelper.ThrowIfNotOnUIThread();

            var solution = Microsoft.VisualStudio.Shell.Package
                .GetGlobalService(typeof(SVsSolution))
                as IVsSolution;

            solution?.AdviseSolutionEvents(this, out _solutionEventsCookie);

            TryLoadSolutionDir();
        }

        private void TryLoadSolutionDir()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solution = Microsoft.VisualStudio.Shell.Package
                .GetGlobalService(typeof(SVsSolution))
                as IVsSolution;

            if (solution == null) return;

            solution.GetSolutionInfo(
                out string solutionDir,
                out string solutionFile,
                out string optsFile);

            if (!string.IsNullOrEmpty(solutionDir))
            {
                var control = (mortarWindowControl)this.Content;
                control.setSolutionDir(solutionDir);
            }
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            TryLoadSolutionDir();
            return 0;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            var control = (mortarWindowControl)this.Content;
            control.setSolutionDir(string.Empty);
            return 0;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => 0;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => 0;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => 0;
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => 0;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => 0;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => 0;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => 0;
        public int OnBeforeCloseSolution(object pUnkReserved) => 0;

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (disposing && _solutionEventsCookie != 0)
            {
                var solution = Microsoft.VisualStudio.Shell.Package
                    .GetGlobalService(typeof(SVsSolution))
                    as IVsSolution;
                solution?.UnadviseSolutionEvents(_solutionEventsCookie);
            }
            base.Dispose(disposing);
        }
    }
}