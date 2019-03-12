using Newtonsoft.Json.Linq;

namespace VgcApis.Models.Interfaces.Lua
{
    public interface ILuaJson
    {
        void SetIntValue(JToken json, string path, int value);
        void SetBoolValue(JToken json, string path, bool value);
        void SetStringValue(JToken json, string path, string value);

        string GetString(JToken json, string path);

        void CombineWithRoutingInFront(JObject body, JObject mixin);
        void CombineWithRoutingInTheEnd(JObject body, JObject mixin);
        JToken GetKey(JToken json, string path);
        string GetProtocol(JObject config);
        string JTokenToString(JToken jtoken);
        void Merge(JObject body, JObject mixin);
        JToken ParseJToken(string json);
        JArray ParseJArray(string json);
        JObject ParseJObject(string json);
        void Replace(JToken node, JToken value);
        JArray ToJArray(JToken jtoken);
        JObject ToJObject(JToken jtoken);
        void Union(JObject body, JObject mixin);
    }
}
