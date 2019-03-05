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
            var imageList = new System.Windows.Forms.ImageList();
            imageList.Images.Add(Properties.Resources.KeyDown_16x);
            imageList.Images.Add(Properties.Resources.Method_16x);
            imageList.Images.Add(Properties.Resources.Class_16x);

            var acm = new AutocompleteMenu()
            {
                SearchPattern = @"[\w\.:]",
                MaximumSize = new Size(300, 200),
                ToolTipDuration = 20000,
                ImageList = imageList,
            };

            acm.TargetControlWrapper = new ScintillaWrapper(editor);
            acm.SetAutocompleteItems(snippets);
        }

        #endregion

        #region private methods
        string GetFilteredLuaKeywords() =>
            VgcApis.Models.Consts.Lua.LuaKeywords
            .Replace("do", "")
            .Replace("then", "")
            .Replace("end", "");

        List<string> GenKeywords(IEnumerable<string> initValues) =>
            new StringBuilder(VgcApis.Models.Consts.Lua.LuaModules)
            .Append(@" ")
            .Append(GetFilteredLuaKeywords())
            .ToString()
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Union(initValues)
            .OrderBy(e => e)
            .ToList();

        List<AutocompleteItem> GenLuaFunctionSnippet() =>
            VgcApis.Models.Consts.Lua.LuaFunctions
            .Replace("dofile", "")
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(s => s)
            .Select(e =>
            {
                try
                {
                    var item = new LuaFuncSnippets(e);
                    return item as AutocompleteItem;
                }
                catch { }
                return null;
            })
            .Where(e => e != null)
            .ToList();

        List<AutocompleteItem> GenLuaSubFunctionSnippet() =>
            VgcApis.Models.Consts.Lua.LuaSubFunctions
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(s => s)
            .Select(e =>
            {
                try
                {
                    var item = new LuaSubFuncSnippets(e);
                    return item as AutocompleteItem;
                }
                catch { }
                return null;
            })
            .Where(e => e != null)
            .ToList();

        List<AutocompleteItem> GenSnippetItems()
        {
            var apis = new List<Tuple<string, Type>>
            {
                new Tuple<string,Type>("Api", typeof(VgcApis.Models.Interfaces.ILuaApis)),
                new Tuple<string,Type>("Signal", typeof(VgcApis.Models.Interfaces.ILuaSignal)),
                new Tuple<string, Type>("coreServ",typeof(VgcApis.Models.Interfaces.ICoreServCtrl)),
                new Tuple<string, Type>("coreConfiger",typeof(VgcApis.Models.Interfaces.CoreCtrlComponents.IConfiger)),
                new Tuple<string, Type>("coreCtrl",typeof(VgcApis.Models.Interfaces.CoreCtrlComponents.ICoreCtrl)),
                new Tuple<string, Type>("coreState",typeof(VgcApis.Models.Interfaces.CoreCtrlComponents.ICoreStates)),
                new Tuple<string, Type>("coreLogger",typeof(VgcApis.Models.Interfaces.CoreCtrlComponents.ILogger)),
            };

            return GenKeywordSnippetItems(GenKeywords(apis.Select(e => e.Item1)))
                .Concat(GenLuaFunctionSnippet())
                .Concat(GenLuaSubFunctionSnippet())
                .Concat(apis.SelectMany(
                    api => GenApiFunctionSnippetItems(api.Item1, api.Item2)))
                .ToList();
        }

        List<AutocompleteItem> GenKeywordSnippetItems(IEnumerable<string> keywords) =>
            keywords
            .OrderBy(k => k)
            .Select(e => new LuaKeywordSnippets(e) as AutocompleteItem)
            .ToList();

        List<AutocompleteItem> GenApiFunctionSnippetItems(
            string apiName, Type type) =>
            VgcApis.Libs.Utils.GetPublicMethodNameAndParam(type)
            .OrderBy(info => info.Item2)  // item2 = method name
            .Select(info => new ApiFunctionSnippets(
                info.Item1, // return type
                apiName,
                info.Item2, // methodName,
                info.Item3, // paramStr,
                info.Item4, // paramWithType,
                @"") as AutocompleteItem
            )
            .ToList();

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
