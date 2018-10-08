using System;
using System.Text;

public static class Utilities
{
    public static string UnBase64String(string value)
    {
        if (value == null || value == "")
        {
            return "";
        }
        byte[] bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string ToBase64String(string value)
    {
        if (value == null || value == "")
        {
            return "";
        }
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }
}
