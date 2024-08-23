using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfTwitch.API
{
    public class PaginationRequest
    {
        public string Cursor { get; }

        public bool IsBefore { get; }

        public uint? PageSizeOverride { get; }

        PaginationRequest(string cursor, bool isBefore, uint? pageSizeOverride)
        {
            if (string.IsNullOrWhiteSpace(cursor))
                throw new ArgumentException($"'{nameof(cursor)}' cannot be null or whitespace.", nameof(cursor));

            Cursor = cursor;
            IsBefore = isBefore;
            PageSizeOverride = pageSizeOverride;
        }

        public static PaginationRequest Before(string cursor, uint? pageSizeOverride = null)
        {
            return new PaginationRequest(cursor, true, pageSizeOverride);
        }

        public static PaginationRequest After(string cursor, uint? pageSizeOverride = null)
        {
            return new PaginationRequest(cursor, false, pageSizeOverride);
        }

        public string GetRequestUriSuffix()
        {
            List<string> queries = [];

            if (IsBefore)
            {
                queries.Add($"before={Cursor}");
            }
            else
            {
                queries.Add($"after={Cursor}");
            }

            if (PageSizeOverride.HasValue)
            {
                queries.Add($"first={PageSizeOverride.Value}");
            }

            return string.Join("&", queries);
        }
    }
}
