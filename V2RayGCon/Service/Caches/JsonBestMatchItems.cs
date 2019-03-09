using AutocompleteMenuNS;
using ScintillaNET;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        private IEnumerable<AutocompleteItem> BuildList()
        {
            var line = GetCurrentLineText();
            var fragment = VgcApis.Libs.Utils.GetFragment(line, searchPattern);

            var table = new Dictionary<string, int>();

            foreach (var keyword in keywords)
            {

            }

            //return autocomplete items
            foreach (var word in words.Keys)
                yield return new AutocompleteItem(word);
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
