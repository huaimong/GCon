namespace VgcApis.Models.Consts
{
    public static class Webs
    {
        public const string LoopbackIP = "127.0.0.1";

        public const string FakeRequestUrl = @"http://localhost:3000/pac/?&t=abc1234";
        public const string GoogleDotCom = @"https://www.google.com";

        public const string BingDotCom = @"https://www.bing.com";

        // https://www.bing.com/search?q=vmess&first=21
        public const string SearchUrlPrefix = BingDotCom + @"/search?q=";
        public const string SearchPagePrefix = @"&first=";


    }
}
