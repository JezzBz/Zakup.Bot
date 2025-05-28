using Newtonsoft.Json;

namespace Zakup.WebHost.Helpers;

public static class CacheHelper
{
    public static string ToCache<T>(T data)
    {
       return JsonConvert.SerializeObject(data);
    }

    public static T? ToData<T>(string cache)
    {
        return JsonConvert.DeserializeObject<T>(cache);
    }
}