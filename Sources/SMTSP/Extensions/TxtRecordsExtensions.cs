using SMTSP.Bonjour;

namespace SMTSP.Extensions;

public static class TxtRecordsExtensions
{
    public static string? GetValue(this ITxtRecord records, string propertyName)
    {
        if (records.Count <= 0)
        {
            return null;
        }

        foreach (TxtRecordItem txtRecord in records)
        {
            if (txtRecord.Key == propertyName)
            {
                return txtRecord.ValueString;
            }
        }

        return null;
    }
}
