using AutocompleteMenuNS;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Luna.Libs.LuaSnippet
{
    internal sealed class LuaAcm :
        VgcApis.Models.BaseClasses.Disposable

    {
        List<AutocompleteItem> snippets;

        public LuaAcm()
        {
            snippets = GenSnippetItems();
        }

        #region public methods
        public void BindToEditor(Scintilla editor)
        {
            var acm = new AutocompleteMenu()
            {
                SearchPattern = @"[\w\.:]",
                MaximumSize = new Size(300, 200),
                ToolTipDuration = 5000,
            };

            acm.TargetControlWrapper = new ScintillaWrapper(editor);
            acm.SetAutocompleteItems(snippets);
        }

        #endregion

        #region private methods

        List<string> GenKeywords()
        {
            var kws = new List<string>();

            var rawString =
                new StringBuilder(VgcApis.Models.Consts.Lua.LuaModules)
                .Append(@" ")
                .Append(VgcApis.Models.Consts.Lua.LuaKeywords)
                .ToString();

            var keywords = rawString.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var kw in keywords)
            {
                if (!kws.Contains(kw))
                {
                    kws.Add(kw);
                }
            }

            return kws;
        }

        List<AutocompleteItem> GenLuaFunctionSnippet()
        {
            var luaFuncList = VgcApis.Models.Consts.Lua.LuaFunctions.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            var sorted = luaFuncList.OrderBy(s => s).ToList();
            var result = new List<AutocompleteItem>();
            foreach (var func in sorted)
            {
                try
                {
                    var item = new LuaFuncSnippets(func);
                    result.Add(item);
                }
                catch { }
            }

            return result;
        }

        List<AutocompleteItem> GenLuaSubFunctionSnippet()
        {
            var subFuncList = VgcApis.Models.Consts.Lua.LuaSubFunctions.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            var sorted = subFuncList.OrderBy(s => s).ToList();
            var result = new List<AutocompleteItem>();
            foreach (var subFunc in sorted)
            {
                try
                {
                    var item = new LuaSubFuncSnippets(subFunc);
                    result.Add(item);
                }
                catch { }
            }

            return result;
        }

        List<AutocompleteItem> GenSnippetItems()
        {
            var finalList = new List<AutocompleteItem>();

            var keywords = GenKeywords();
            foreach (var item in GenKeywordSnippetItems(keywords))
            {
                finalList.Add(item);
            }

            var luaFuncSnippetItems = GenLuaFunctionSnippet();
            foreach (var item in luaFuncSnippetItems)
            {
                finalList.Add(item);
            }

            var luaSubFuncSnippetItems = GenLuaSubFunctionSnippet();
            foreach (var item in luaSubFuncSnippetItems)
            {
                finalList.Add(item);
            }

            var apis = new List<Tuple<string, Type>>
            {
                new Tuple<string,Type>("Api", typeof(VgcApis.Models.Interfaces.ILuaApis)),
                new Tuple<string,Type>("Signal", typeof(VgcApis.Models.Interfaces.ILuaSignal)),
            };

            foreach (var api in apis)
            {
                var apiFuncList = GenApiFunctionSnippetItems(
                    api.Item1, api.Item2);
                foreach (var item in apiFuncList)
                {
                    finalList.Add(item);
                }
            }

            return finalList;
        }

        List<AutocompleteItem> GenKeywordSnippetItems(IEnumerable<string> keywords)
        {
            var sorted = keywords.OrderBy(k => k).ToList();
            var items = new List<AutocompleteItem>();
            foreach (var item in sorted)
                items.Add(new AutocompleteItem(item));
            return items;
        }

        List<AutocompleteItem> GenApiFunctionSnippetItems(
            string name, Type type)
        {
            /// [0: ReturnType 1: MethodName 2: ParamsStr 3: ParamsWithType]
            var apiFunctionInfos =
                VgcApis.Libs.Utils.GetPublicMethodNameAndParam(type);

            var result = new List<AutocompleteItem>();

            var sorted = apiFunctionInfos
                .OrderBy(info => info.Item2)
                .ToList();

            foreach (var info in sorted)
            {
                result.Add(
                    new ApiFunctionSnippets(
                       info.Item1, name, info.Item2, info.Item3, info.Item4, @""));
            }
            return result;
        }

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            // acm will dispose it self.
            // acm?.Dispose();
        }
        #endregion
    }
}
