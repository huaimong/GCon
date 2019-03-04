using AutocompleteMenuNS;

namespace Luna.Libs.LuaSnippet
{
    internal sealed class LuaFuncSnippets : AutocompleteItem
    {
        string lowerText;

        public LuaFuncSnippets(string luaFuncStr)
            : base(luaFuncStr)
        {
            if (luaFuncStr == null)
            {
                throw new System.ArgumentException(
                    @"luaFuncStr is null!");
            }

            ImageIndex = 0;
            ToolTipTitle = GenTitle(luaFuncStr);
            ToolTipText = @"";
            Text = GenTitle(luaFuncStr);

            lowerText = Text.ToLower();
        }

        string GenTitle(string fnName) =>
            $"{fnName}()";

        public override CompareResult Compare(string fragmentText)
        {
            if (fragmentText == Text)
                return CompareResult.VisibleAndSelected;
            if (lowerText.StartsWith(fragmentText.ToLower()))
                return CompareResult.Visible;
            return CompareResult.Hidden;
        }
    }
}
