namespace VgcApis.Models.Consts
{
    public static class Files
    {
        public static readonly string CoreFolderName = "core";

        static string GenExtString(string extension, bool appendAllFile = true)
        {
            var l = extension.ToLower();
            var e = $"{l} file|*.{l}";
            var a = "|All File|*.*";
            return appendAllFile ? e + a : e;
        }

        public static readonly string JsExt = GenExtString("js");
        public static readonly string JsonExt = GenExtString("json");
        public static readonly string PacExt = GenExtString("pac");
        public static readonly string LuaExt = GenExtString("lua");
        public static readonly string TxtExt = GenExtString("txt");

        public const string PatternUrl =
            @"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_=]*)?";

        public const string PatternBase64 =
            @"(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{4})";
    }
}
