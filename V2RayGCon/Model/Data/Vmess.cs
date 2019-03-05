using Newtonsoft.Json;

namespace V2RayGCon.Model.Data
{
    public class Vmess
    {
        public string ps, add, port, id, aid, net, type, host, tls, v, path;

        public Vmess()
        {
            v = string.Empty;       // v1:"" v2:"2"
            ps = string.Empty;      // alias
            add = string.Empty;     // ip,hostname
            port = string.Empty;    // port
            id = string.Empty;      // user id
            aid = string.Empty;
            net = string.Empty;     // ws,tcp,kcp
            type = string.Empty;    // kcp->header
            host = string.Empty;    // v1: ws->path v2: ws->host h2->["host1","host2"]
            path = string.Empty;    // v1: "" v2: ws->path h2->path
            tls = string.Empty;     // streamSettings->security
        }

        public string ToVmessLink()
        {
            var vmess = this;
            if (vmess == null)
            {
                return null;
            }

            string content = JsonConvert.SerializeObject(vmess);
            return Lib.Utils.AddLinkPrefix(
                Lib.Utils.Base64Encode(content),
                VgcApis.Models.Datas.Enum.LinkTypes.vmess);
        }
    }
}
