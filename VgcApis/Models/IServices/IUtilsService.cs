using System.Collections.Generic;

namespace VgcApis.Models.IServices
{
    public interface IUtilsService
    {
        List<string> ExtractLinks(string text, Datas.Enum.LinkTypes linkType);
    }
}
