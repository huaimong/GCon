using System;
using System.Net;

namespace V2RayGCon.Lib.Nets
{
    // use string s = new TimedWebClient { Timeout = 500 }.DownloadString(URL);

    public class TimedWebClient : WebClient
    {
        // https://stackoverflow.com/questions/12878857/how-to-limit-the-time-downloadstringurl-allowed-by-500-milliseconds
        // Timeout in milliseconds
        const int TIMEOUT = 30000;

        public int Timeout { get; set; }

        public TimedWebClient()
        {
            this.Timeout = TIMEOUT;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout =
                this.Timeout < 1 ? TIMEOUT : this.Timeout;
            return objWebRequest;
        }
    }


}
