using System.Collections.Generic;

namespace V2RayGCon.Lib.Lua.ApiComponents
{
    public class UtilsApi :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.IServices.IUtilsService
    {
        public List<string> ExtractLinks(
            string text,
            VgcApis.Models.Datas.Enum.LinkTypes linkType) =>
            Lib.Utils.ExtractLinks(text, linkType);
    }
}
