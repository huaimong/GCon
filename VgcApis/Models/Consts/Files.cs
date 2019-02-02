namespace VgcApis.Models.Consts
{
    public static class Files
    {
        static string GenExtString(string extension, bool appendAllFile = true)
        {
            var l = extension.ToLower();
            var e = $"{l} file|*.{l}";
            var a = "|All File|*.*";
            return appendAllFile ? e : e + a;
        }

        public static readonly string JsExt = GenExtString("js");
        public static readonly string JsonExt = GenExtString("json");
        public static readonly string PacExt = GenExtString("pac");
        public static readonly string LuaExt = GenExtString("lua");
        public static readonly string TxtExt = GenExtString("txt");
    }
}
