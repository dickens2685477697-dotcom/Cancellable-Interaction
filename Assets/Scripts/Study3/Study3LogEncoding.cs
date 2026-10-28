using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Shared by the new sidecars only. Existing CSV writers keep their original contract.
public static class Study3LogEncoding
{
    public static string Csv(object value)
    {
        string text = NumberOrText(value);
        return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
            ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static string Row(params object[] fields)
    {
        var result = new string[fields.Length];
        for (int i = 0; i < fields.Length; i++) result[i] = Csv(fields[i]);
        return string.Join(",", result);
    }

    public static string NumberOrText(object value)
    {
        if (value == null) return "NA";
        if (value is double d) return double.IsNaN(d) || double.IsInfinity(d) ? "NA" : d.ToString("R", CultureInfo.InvariantCulture);
        if (value is float f) return float.IsNaN(f) || float.IsInfinity(f) ? "NA" : f.ToString("R", CultureInfo.InvariantCulture);
        if (value is bool b) return b ? "1" : "0";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    public static string Json(object value)
    {
        if (value == null) return "null";
        if (value is bool b) return b ? "true" : "false";
        if (value is float || value is double)
        {
            string number = NumberOrText(value);
            return number == "NA" ? "null" : number;
        }
        if (value is byte || value is short || value is int || value is uint || value is long || value is ulong || value is decimal)
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        if (value is IDictionary<string, object> dict)
        {
            var keys = new List<string>(dict.Keys); keys.Sort(StringComparer.Ordinal);
            var pairs = new List<string>();
            foreach (string key in keys) pairs.Add(Json(key) + ":" + Json(dict[key]));
            return "{" + string.Join(",", pairs) + "}";
        }
        if (value is IEnumerable values && !(value is string))
        {
            var items = new List<string>();
            foreach (object item in values) items.Add(Json(item));
            return "[" + string.Join(",", items) + "]";
        }
        var text = new StringBuilder("\"");
        foreach (char c in Convert.ToString(value, CultureInfo.InvariantCulture))
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default: if (c < 32) text.Append("\\u" + ((int)c).ToString("x4")); else text.Append(c); break;
            }
        }
        return text.Append('"').ToString();
    }

    public static List<string> ParseCsv(string row)
    {
        var result = new List<string>(); var field = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if (c == '"')
            {
                if (quoted && i + 1 < row.Length && row[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { result.Add(field.ToString()); field.Clear(); }
            else field.Append(c);
        }
        if (quoted) throw new FormatException("Unclosed CSV quote.");
        result.Add(field.ToString()); return result;
    }
}
