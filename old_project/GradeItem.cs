using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace e_student
{
    public class GradeItem
    {
        public string Subject { get; set; }
        public string Grade { get; set; }
        public string Color { get; set; }
        public bool IsSemester =>
            string.IsNullOrWhiteSpace(Subject);
        public bool NotSemester =>
            !string.IsNullOrWhiteSpace(Subject);
    }
}
