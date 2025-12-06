using System.Text.RegularExpressions;

namespace YoutubeSearcher.Web.Models
{
    public static class Extensions
    {

        public static string CleanString(this string txt)
        {
            var res = Regex.Replace(txt, @"[^\u0000-\u007F]", String.Empty);
            res = Regex.Replace(res, "[\\\\/:*?<>|\"]", String.Empty);
            res = Regex.Replace(res, @"\s+", " ").Trim();

            return res;
        }

        public static string EscapeForFfmpeg(this string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\\\""); // çift tırnakları kaçır
        }

    }
}
