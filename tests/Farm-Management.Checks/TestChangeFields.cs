using System.Text.RegularExpressions;

internal static class TestChangeFields
{
    public static void Apply(Dictionary<string,string> fields,string html)
    {
        fields.TryAdd("ChangedBy","Người kiểm thử QA");fields.TryAdd("ChangeReason","Đối chiếu dữ liệu kiểm thử trong database dùng một lần");
        var revision=Regex.Match(html,"name=\"OriginalRevision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        if(revision.Length>0)fields.TryAdd("OriginalRevision",revision);
    }
}
