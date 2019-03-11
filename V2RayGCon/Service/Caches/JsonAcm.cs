using AutocompleteMenuNS;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace V2RayGCon.Service.Caches
{
    public sealed class JsonAcm
    {
        public JsonAcm() { }

        #region public methods
        public void BindToEditor(Scintilla editor)
        {
            const string SearchPattern =
            VgcApis.Models.Consts.Patterns.JsonSnippetSearchPattern;

            var acm = new AutocompleteMenu()
            {
                SearchPattern = SearchPattern,
                MaximumSize = new Size(320, 200),
                ToolTipDuration = 5000,
            };

            acm.TargetControlWrapper = new ScintillaWrapper(editor);
            var snippets = new JsonBestMatchItems(
                    editor, SearchPattern, GetKeywords());
            acm.SetAutocompleteItems(snippets);
        }

        #endregion

        #region private methods
        List<string> keywordCache = null;
        List<string> GetKeywords()
        {
            if (keywordCache == null)
            {
                keywordCache = Resource.Resx.StrConst.ConfigJsonKeywords
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            return keywordCache;
        }

        #endregion

        #region protected methods

        #endregion
    }
}
