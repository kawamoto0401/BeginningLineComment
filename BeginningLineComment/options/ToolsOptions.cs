using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeginningLineComment.tool;
using Microsoft.VisualStudio.Shell;

namespace BeginningLineComment.option {

    class ToolsOptions : DialogPage {

        private string headerComment = ":";

        [DisplayName("入力文字：Input String")]
        [Description("Shiftキーをおしたときに先頭に挿入するコメント文字列\r\nComment string to insert at the beginning when the Shift key is pressed")]
        [DefaultValue(":")]
        public string HeaderComment {
            get {
                UserDebug.WriteLine(headerComment);
                return headerComment;
            }
            set {
                UserDebug.WriteLine(value);
                if( 0 >= value.Length) {
                    headerComment = ":";
                }
                else {
                    headerComment = value;
                }
            }
        }

        public override void ResetSettings() {
            UserDebug.WriteLine("");
            base.ResetSettings();
        }
    }
}
