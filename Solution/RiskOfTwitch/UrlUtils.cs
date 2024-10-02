using System.Collections.Generic;
using System.Web;
using System;

namespace RiskOfTwitch
{
    public static class UrlUtils
    {
        public static void SplitUrlQueries(string queries, IDictionary<string, string> destination)
        {
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            if (string.IsNullOrWhiteSpace(queries))
                return;

            foreach (string queryItem in queries.Split(['&'], StringSplitOptions.RemoveEmptyEntries))
            {
                string[] splitQueryItem = queryItem.Split('=');
                if (splitQueryItem.Length != 2)
                    continue;

                string queryName = HttpUtility.UrlDecode(splitQueryItem[0]).Trim();
                string queryValue = HttpUtility.UrlDecode(splitQueryItem[1]).Trim();

                if (string.IsNullOrEmpty(queryName) || string.IsNullOrEmpty(queryValue))
                    continue;

                if (destination.ContainsKey(queryName))
                    continue;

                destination.Add(queryName, queryValue);
            }
        }
    }
}