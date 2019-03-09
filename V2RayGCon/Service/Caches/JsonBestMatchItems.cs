using AutocompleteMenuNS;
using ScintillaNET;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace V2RayGCon.Service.Caches
{
    internal sealed class JsonBestMatchItems :
        IEnumerable<AutocompleteItem>
    {
        Scintilla editor;
        string searchPattern = VgcApis.Models.Consts.Patterns.JsonSnippetSearchPattern;

        List<string> keywords;

        public JsonBestMatchItems(
            Scintilla editor,
            string matchPattern,
            IEnumerable<string> rawKeywords)
        {
            this.searchPattern = matchPattern;
            this.editor = editor;
            this.keywords = rawKeywords.ToList();
        }

        #region private methods
        string GetFragment()
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            var selectedText = editor.SelectedText;
            if (selectedText.Length > 0)
            {
                return selectedText;
            }

            string text = editor.Text;
            var regex = new Regex(searchPattern);

            var startPos = GetCurPos();

            //go forward
            int i = startPos;
            while (i >= 0 && i < text.Length)
            {
                if (!regex.IsMatch(text[i].ToString()))
                    break;
                i++;
            }

            var endPos = i;

            //go backward
            i = startPos;
            while (i > 0 && (i - 1) < text.Length)
            {
                if (!regex.IsMatch(text[i - 1].ToString()))
                    break;
                i--;
            }
            startPos = i;

            return GetSubString(startPos, endPos, text);
        }

        string GetSubString(int start, int end, string text)
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            if (string.IsNullOrEmpty(text))
                return "";
            if (start >= text.Length)
                return "";
            if (end > text.Length)
                return "";

            return text.Substring(start, end - start);
        }

        private IEnumerable<AutocompleteItem> BuildList()
        {
            var fragment = GetFragment();

            var table = new Dictionary<string, long>();

            foreach (var keyword in keywords)
            {
                var marks = VgcApis.Libs.Utils.MeasureSimilarityCi(
                    keyword, fragment);

                if (marks > 0)
                {
                    table.Add(keyword, marks);
                }
            }

            var sorted = table
                .OrderBy(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Select(kv => kv.Key)
                .ToList();


            //return autocomplete items
            foreach (var word in sorted)
                yield return new JsonKeywordItems(word);
        }

        int GetCurPos() => editor.CurrentPosition;

        string GetCurrentLineText()
        {
            int curPos = GetCurPos();
            int lineNumber = editor.LineFromPosition(curPos);
            int startPos = editor.Lines[lineNumber].Position;
            return editor.GetTextRange(startPos, (curPos - startPos));
        }

        #endregion

        #region IEnumerable thinggy
        public IEnumerator<AutocompleteItem> GetEnumerator() =>
            BuildList().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
