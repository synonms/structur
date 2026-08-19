using System.Text;

namespace Synonms.Structur.Api.Client.Http;

public static class QueryParametersExtensions
{
    public static string ToQueryString(this Dictionary<string, string> queryParameters)
    {
        string queryString = string.Empty;

        if (!queryParameters.Any())
        {
            return queryString;
        }
        
        StringBuilder queryStringBuilder = new("?");
        bool isFirstParameter = true;
        
        foreach ((string key, string value) in queryParameters)
        {
            if (!isFirstParameter)
            {
                queryStringBuilder.Append('&');
            }
            queryStringBuilder.Append($"{key}={value}");
            isFirstParameter = false;
        }
        
        queryString = queryStringBuilder.ToString();

        return queryString;
    }
}