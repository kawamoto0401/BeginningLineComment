using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using BeginningLineComment.option;
using BeginningLineComment.tool;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BeginningLineComment {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("73bbd8a3-b94d-4d61-86b9-258f8cb9c228");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// DTE
        /// </summary>
        private EnvDTE.DTE _dTE = null;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command(package, commandService);

            Instance._dTE = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (null != Instance._dTE) {
                UserDebug.WriteLine(string.Format("Version={0}", Instance._dTE.Version));
                UserDebug.WriteLine(string.Format("Name={0}", Instance._dTE.Name));

                EnvDTE.Solution solution = Instance._dTE.Solution;

                UserDebug.WriteLine(string.Format("FullName={0}", solution.FullName));
                UserDebug.WriteLine(string.Format("FileName={0}", solution.FileName));
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);

            try {
                bool isShiftKey = false;

                if (Keys.Shift == (Control.ModifierKeys & Keys.Shift)) {
                    isShiftKey = true;
                }

                if (_dTE?.ActiveDocument.Object("TextDocument") is TextDocument textDocument) {

                    string headerComment = null;

                    if (!isShiftKey) {
                        headerComment = CreateCommentPatterns(textDocument.Language);
                        if (headerComment == null) {
                            // 対象外
                            System.Windows.Forms.MessageBox.Show("Not compatible TextDocument Language");
                        }
                    }else {
                        // オプション情報を取得
                        IVsPackage vsPackage = package as IVsPackage;
                        if (null == vsPackage) {
                            throw new Exception("Not IVsPackage");
                        }

                        object obj;
                        vsPackage.GetAutomationObject("BeginningLineComment.General", out obj);
                        ToolsOptions options = obj as ToolsOptions;
                        if (null == options) {
                            throw new Exception("Not ToolsOptions");
                        }

                        headerComment = options.HeaderComment;
                    }

                    TextSelection textSelection = textDocument.Selection;

                    string text = textSelection.Text;
                    UserDebug.WriteLine("=============");
                    UserDebug.WriteLine("\n" + text);
                    UserDebug.WriteLine("=============");

                    // 行頭から行終了までを選択する
                    SelectLines(textSelection);

                    EditPoint startPoint = textSelection.TopPoint.CreateEditPoint();
                    EditPoint endPoint = textSelection.BottomPoint.CreateEditPoint();

                    // 文字を変換する
                    string outStr = UserConvert.ConvertComment(textSelection.Text, headerComment);

                    startPoint.ReplaceText(endPoint, outStr, (int)vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines);
                }
            }
            catch (Exception ex) {
                UserDebug.ExceptionMessageBox(ex);
            }
        }


        /// <summary>
        /// 選択中の行を行選択状態にします。
        /// https://github.com/munyabe/ToggleComment
        /// </summary>
        private static void SelectLines(TextSelection selection) {
            ThreadHelper.ThrowIfNotOnUIThread();

            var startPoint = selection.TopPoint.CreateEditPoint();
            startPoint.StartOfLine();

            var endPoint = selection.BottomPoint.CreateEditPoint();
            if (endPoint.AtStartOfLine == false || startPoint.Line == endPoint.Line) {
                endPoint.EndOfLine();
            }

            if (selection.Mode == vsSelectionMode.vsSelectionModeBox) {
                selection.Mode = vsSelectionMode.vsSelectionModeStream;
            }

            selection.MoveToPoint(startPoint);
            selection.MoveToPoint(endPoint, true);
        }

        public static string CreateCommentPatterns(string language)
        {
            switch (language) {
                case "CSharp":
                case "C/C++":
                case "TypeScript":
                case "JavaScript":
                case "F#":
                    return "//";
                case "PowerShell":
                case "Python":
                    return "#";
                case "SQL Server Tools":
                    return "--";
                case "Basic":
                    return "'";
                case "XML":
                case "XAML":
                case "HTMLX":
                case "HTML":
                case "CSS":
                default:
                    return null;
            }
        }
    }

    public class UserConvert
    {
        private UserConvert()
        {
        }

        public static string ConvertComment(string text, string headerComment)
        {
            UserDebug.WriteLine("=============");
            UserDebug.WriteLine("\n" + text);
            UserDebug.WriteLine("=============");

            string outStr = "";
            string[] lineLists = Regex.Split(text, "(\n|\r\n|\r)");

            outStr += headerComment;

            foreach (string line in lineLists) {
                outStr += line;
                if( Regex.IsMatch(line, "(\n|\r\n|\r)")) {
                    outStr += headerComment;
                }
            }

            UserDebug.WriteLine("=============");
            UserDebug.WriteLine("\n" + outStr);
            UserDebug.WriteLine("=============");

            return outStr;
        }
    }
}
