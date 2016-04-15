using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System.Dynamic;

namespace KennethScott.AddDbUpFile
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidAddDbUpFilePkgString)]
    public sealed class AddDbUpFilePackage : ExtensionPointPackage
    {
        public static DTE2 _dte;

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;

            Logger.Initialize(this, Vsix.Name);
            Telemetry.Initialize(_dte, Vsix.Version, "04533DD8-75D9-4457-A524-3DA423AE18C7");

            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID menuCommandID = new CommandID(PackageGuids.guidAddDbUpFileCmdSet, PackageIds.cmdidMyCommand);
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                mcs.AddCommand(menuItem);
            }
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            UIHierarchyItem item = GetSelectedItem();
            var project = item.Object as Project;

            if (project == null || !project.Kind.Equals(EnvDTE.Constants.vsProjectKindSolutionItems, StringComparison.OrdinalIgnoreCase))
                button.Visible = button.Enabled = true;
        }

        private async void MenuItemCallback(object sender, EventArgs e)
        {
            UIHierarchyItem item = GetSelectedItem();

            if (item == null)
                return;

            string folder = FindFolder(item);

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;

            Project project = ProjectHelpers.GetActiveProject();
            if (project == null)
                return;

            dynamic inputValues = PromptForFileName(folder);
            string input = inputValues.Input.TrimStart('/', '\\').Replace("/", "\\");

            if (string.IsNullOrEmpty(input))
                return;

            string[] parsedInputs = GetParsedInput(input);

            foreach (string inputItem in parsedInputs)
            {
                input = inputItem;

                input = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + input;

                if (input.EndsWith("\\", StringComparison.Ordinal))
                {
                    input = input + "__dummy__";
                }

                string file = Path.Combine(folder, input);
                string dir = Path.GetDirectoryName(file);

                PackageUtilities.EnsureOutputPath(dir);

                if (!File.Exists(file))
                {
                    int position = await WriteFile(project, file);

                    try
                    {
                        var projectItem = project.AddFileToProject(file, isEmbeddedResource: (bool)inputValues.IsEmbeddedResource);

                        if (file.EndsWith("__dummy__"))
                        {
                            Telemetry.TrackEvent("Folder added");
                            projectItem.Delete();
                            continue;
                        }

                        VsShellUtilities.OpenDocument(this, file);

                        // Move cursor into position
                        if (position > 0)
                        {
                            var view = ProjectHelpers.GetCurentTextView();

                            if (view != null)
                                view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, position));
                        }

                        _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                        _dte.ActiveDocument.Activate();

                        await Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                        {
                            var command = _dte.Commands.Item("Edit.FormatDocument");

                            if (command.IsAvailable)
                                _dte.ExecuteCommand(command.Name);

                        }), DispatcherPriority.SystemIdle, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("The file '" + file + "' already exist.");
                }
            }
        }

        private static async Task<int> WriteFile(Project project, string file)
        {
            Encoding encoding = new UTF8Encoding(false);
            string extension = Path.GetExtension(file);
            string template = await TemplateMap.GetTemplateFilePath(project, file);

            var props = new Dictionary<string, string>() { { "extension", extension.ToLowerInvariant() } };
            Telemetry.TrackEvent("File added", props);

            if (!string.IsNullOrEmpty(template))
            {
                int index = template.IndexOf('$');
                template = template.Remove(index, 1);
                File.WriteAllText(file, template, encoding);
                return index;
            }

            File.WriteAllText(file, string.Empty, encoding);
            return 0;
        }

        static string[] GetParsedInput(string input)
        {
            // var tests = new string[] { "file1.txt", "file1.txt, file2.txt", ".ignore", ".ignore.(old,new)", "license", "folder/",
            //    "folder\\", "folder\\file.txt", "folder/.thing", "page.aspx.cs", "widget-1.(html,js)", "pages\\home.(aspx, aspx.cs)",
            //    "home.(html,js), about.(html,js,css)", "backup.2016.(old, new)", "file.(txt,txt,,)", "file_@#d+|%.3-2...3^&.txt" };
            Regex pattern = new Regex(@"[,]?([^(,]*)([\.\/\\]?)[(]?((?<=[^(])[^,]*|[^)]+)[)]?");
            List<string> results = new List<string>();
            Match match = pattern.Match(input);

            while (match.Success)
            {
                // Alwasy 4 matches w. Group[3] being the extension, extension list, folder terminator ("/" or "\"), or empty string
                string path = match.Groups[1].Value.Trim() + match.Groups[2].Value;
                string[] extensions = match.Groups[3].Value.Split(',');

                foreach (string ext in extensions)
                {
                    string value = path + ext.Trim();

                    // ensure "file.(txt,,txt)" or "file.txt,,file.txt,File.TXT" retuns as just ["file.txt"]
                    if (value != "" && !value.EndsWith(".", StringComparison.Ordinal) && !results.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        results.Add(value);
                    }
                }
                match = match.NextMatch();
            }
            return results.ToArray();
        }

        private dynamic PromptForFileName(string folder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);
            var dialog = new FileNameDialog(dir.Name);

            var hwnd = new IntPtr(_dte.MainWindow.HWnd);
            var window = (System.Windows.Window)HwndSource.FromHwnd(hwnd).RootVisual;
            dialog.Owner = window;

            var result = dialog.ShowDialog();
            dynamic returnValues = new ExpandoObject();
            returnValues.Input = (result.HasValue && result.Value) ? dialog.Input : string.Empty;
            returnValues.IsEmbeddedResource = dialog.IsEmbeddedResource;
            return returnValues;
        }

        private static string FindFolder(UIHierarchyItem item)
        {
            Window2 window = _dte.ActiveWindow as Window2;

            if (window != null && window.Type == vsWindowType.vsWindowTypeDocument)
            {
                // if a document is active, use the document's containing directory
                Document doc = _dte.ActiveDocument;
                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    ProjectItem docItem = _dte.Solution.FindProjectItem(doc.FullName);

                    if (docItem != null)
                    {
                        string fileName = docItem.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(fileName))
                            return Path.GetDirectoryName(fileName);
                    }
                }
            }

            string folder = null;

            ProjectItem projectItem = item.Object as ProjectItem;
            Project project = item.Object as Project;

            if (projectItem != null)
            {
                string fileName = projectItem.FileNames[0];

                if (File.Exists(fileName))
                {
                    folder = Path.GetDirectoryName(fileName);
                }
                else
                {
                    folder = fileName;
                }
            }
            else if (project != null)
            {
                folder = project.GetRootFolder();
            }

            return folder;
        }

        private static UIHierarchyItem GetSelectedItem()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                return selItem;
            }

            return null;
        }
    }
}
