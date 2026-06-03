using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace e_student
{


    public class GradesParser
    {
        public List<GradeItem> Parse(string html)
        {
            List<GradeItem> grades = new();

            var pattern = new Regex(
                @"<td colspan=4><center><b>(?<semester>[^<]+)</b>|<span[^>]*><b>(?<subject>[^<]+)</b>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match m in pattern.Matches(html))
            {
                // SEMESTER
                if (m.Groups["semester"].Success)
                {
                    grades.Add(new GradeItem
                    {
                        Grade = $"== {m.Groups["semester"].Value.Trim()} ==",
                        Subject = "",
                        Color = "White"
                    });

                    continue;
                }

                // SUBJECT
                string subject = WebUtility.HtmlDecode(m.Groups["subject"].Value).Trim();

                int start = m.Index;

                var nextMatch = Regex.Match(
                    html.Substring(start + m.Length),
                    @"<span[^>]*><b>|<td colspan=4><center><b>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                int end = nextMatch.Success
                    ? start + m.Length + nextMatch.Index
                    : html.Length;

                string block = html.Substring(start, end - start);

                var gradeMatch = Regex.Match(
                    block,
                    @"oценк[аи]:<br>\s*([^<]+?\(\d\))",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                string grade = gradeMatch.Success
                    ? WebUtility.HtmlDecode(gradeMatch.Groups[1].Value).Trim()
                    : "Няма оценка";


                // COLOR RULES (same logic, but as strings)
                string color =
                    grade.Contains("Няма оценка") ? "Blue" :
                    grade.Contains("Зачита се") || grade.Contains("(6)") ? "Green" :
                    grade.Contains("(5)") ? "Cyan" :
                    grade.Contains("(4)") ? "Yellow" :
                    grade.Contains("(3)") ? "Orange" :
                    grade.Contains("(2)") ? "Red" :
                    "White";

                grades.Add(new GradeItem
                {
                    Subject = subject,
                    Grade = grade,
                    Color = color
                });
            }

            List<List<GradeItem>> groups = new();

            List<GradeItem> currentGroup = null;

            foreach (GradeItem item in grades)
            {
                if (item.IsSemester)
                {
                    currentGroup = new List<GradeItem>
                    {
                        item
                    };
                    groups.Add(currentGroup);
                }
                else
                {
                    currentGroup?.Add(item);
                }
            }

            groups.Reverse();

            var finalList = groups.SelectMany(g => g).ToList();

            return finalList;
        }
    }
}
