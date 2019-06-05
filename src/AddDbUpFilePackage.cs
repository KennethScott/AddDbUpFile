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
using Microsoft.VisualStudio.Text;
using System.Dynamic;
using System.Threading;

namespace KennethScott.AddDbUpFile
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidAddDbUpFilePkgString)]
    public sealed class AddDbUpFilePackage : AsyncPackage
    {
        public static DTE2 _dte;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;

            Logger.Initialize(this, Vsix.Name);

            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
            {
                var menuCommandID = new CommandID(PackageGuids.guidAddDbUpFileCmdSet, PackageIds.cmdidMyCommand);
                var menuItem = new OleMenuCommand(ExecuteAsync, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        //private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        //{
        //    var button = (OleMenuCommand)sender;
        //    button.Visible = button.Enabled = false;

        //    UIHierarchyItem item = GetSelectedItem();
        //    var project = item.Object as Project;

        //    if (project == null || !project.Kind.Equals(EnvDTE.Constants.vsProjectKindSolutionItems, StringComparison.OrdinalIgnoreCase))
        //        button.Visible = button.Enabled = true;
        //}

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            object item = ProjectHelpers.GetSelectedItem();
            string folder = FindFolder(item);

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;

            var selectedItem = item as ProjectItem;
            var selectedProject = item as Project;
            Project project = selectedItem?.ContainingProject ?? selectedProject ?? ProjectHelpers.GetActiveProject();

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

                if (input.EndsWith("\\", StringComparison.Ordinal))
                {
                    input = input + "__dummy__";
                }
                else
                {
                    // only prepend the date/time onto filenames (not folders)
                    input = input.Insert(input.LastIndexOf("\\")+1, DateTime.Now.ToString("yyyyMMdd_HHmmss_"));
                }

                var file = new FileInfo(Path.Combine(folder, input));
                string dir = file.DirectoryName;

                PackageUtilities.EnsureOutputPath(dir);

                if (!file.Exists)
                {
                    int position = await WriteFileAsync(project, file.FullName);

                    try
                    {
                        ProjectItem projectItem = null;
                        if (item is ProjectItem projItem)
                        {
                            if ("{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}" == projItem.Kind) // Constants.vsProjectItemKindVirtualFolder
                            {
                                projectItem = projItem.ProjectItems.AddFromFile(file.FullName);
                            }
                        }
                        if (projectItem == null)
                        {
                            project.AddFileToProject(file.FullName, isEmbeddedResource: (bool)inputValues.IsEmbeddedResource);
                        }
                          
                        if (file.FullName.EndsWith("__dummy__"))
                        {
                            projectItem?.Delete();
                            continue;
                        }

                        VsShellUtilities.OpenDocument(this, file.FullName);

                        // Move cursor into position
                        if (position > 0)
                        {
                            Microsoft.VisualStudio.Text.Editor.IWpfTextView view = ProjectHelpers.GetCurentTextView();

                            if (view != null)
                                view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, position));
                        }

                        // I have no idea why but only doing Activate once after SyncwithActiveDocument does not work 100% of the time.
                        _dte.ActiveDocument.Activate();
                        _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                        _dte.ActiveDocument.Activate();

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

        private static async Task<int> WriteFileAsync(Project project, string file)
        {
            string extension = Path.GetExtension(file);
            string template = await TemplateMap.GetTemplateFilePathAsync(project, file);

            if (!string.IsNullOrEmpty(template))
            {
                int index = template.IndexOf("$$");

                if (index > -1)
                {
                    template = template.Remove(index, 2);
                }

                await WriteToDiskAsync(file, template);
                return index;
            }

            await WriteToDiskAsync(file, string.Empty);

            return 0;
        }

        private static Encoding GetFileEncoding(string file)
        {
            string[] noBom = { ".cmd", ".bat", ".json" };
            string ext = Path.GetExtension(file).ToLowerInvariant();

            if (noBom.Contains(ext))
                return new UTF8Encoding(false);

            return new UTF8Encoding(true);
        }

        private static async System.Threading.Tasks.Task WriteToDiskAsync(string file, string content)
        {
            using (var writer = new StreamWriter(file, false, GetFileEncoding(file)))
            {
                await writer.WriteAsync(content);
            }
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

        private static string FindFolder(object item)
        {
            if (item == null)
                return null;


            if (_dte.ActiveWindow is Window2 window && window.Type == vsWindowType.vsWindowTypeDocument)
            {
                // if a document is active, use the document's containing directory
                Document doc = _dte.ActiveDocument;
                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    ProjectItem docItem = _dte.Solution.FindProjectItem(doc.FullName);

                    if (docItem != null && docItem.Properties != null)
                    {
                        string fileName = docItem.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(fileName))
                            return Path.GetDirectoryName(fileName);
                    }
                }
            }

            string folder = null;

            var projectItem = item as ProjectItem;
            if (projectItem != null && "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}" == projectItem.Kind) //Constants.vsProjectItemKindVirtualFolder
            {
                ProjectItems items = projectItem.ProjectItems;
                foreach (ProjectItem it in items)
                {
                    if (File.Exists(it.FileNames[1]))
                    {
                        folder = Path.GetDirectoryName(it.FileNames[1]);
                        break;
                    }
                }
            }
            else
            {
                var project = item as Project;
                if (projectItem != null)
                {
                    string fileName = projectItem.FileNames[1];

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
            }
            return folder;
        }

    }
}
