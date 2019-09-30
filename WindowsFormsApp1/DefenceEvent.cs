using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefenceAligner
{
    public class DefenceEvent
    {
        public int Id { get; set; }
        public string Degree { get; set; }
        public string Student_No { get; set; }
        public string Department { get; set; }
        public string Student_Name { get; set; }
        public string Paper_Title { get; set; }
        public int[] Referee_id { get; set; }
        public DefenceEvent(int id, string degree, string student_no, string department, string student_name, string title)
        {
            Id = id;
            Degree = degree;
            Student_No = student_no;
            Department = department;
            Student_Name = student_name;
            Paper_Title = title;
            Referee_id = new int[5];
        }
        private void a(StringBuilder s, string x, bool last = false)
        {
            s.Append("\"");
            s.Append(x);
            s.Append("\"");
            if (!last)
                s.Append(",");
        }
        public string ToCSV(DBManip db)
        {
            var str = new StringBuilder();
            a(str, Id.ToString());
            a(str, Degree);
            a(str, Student_No);
            a(str, Department);
            a(str, Student_Name);
            a(str, Paper_Title);
            for (int i = 0; i < 5; i++)
            {
                if (Referee_id[i] == -1)
                {
                    a(str, "", true);
                    break;
                }
                a(str, db.GetProfessorName(Referee_id[i]), i == 4);
            }
            return str.ToString();
        }
        // 2つのイベントが同時に開催できないかどうか
        // 同じ審査員がいる場合はtrue
        public bool IsConflict(DefenceEvent ev)
        {
            for (int i = 0; i < 5; i++)
            {
                if (Referee_id[i] == -1)
                    break;
                for (int j = 0; j < 5; j++)
                {
                    if (Referee_id[i] == Referee_id[j])
                        return true;
                }
            }
            return false;
        }
    }
}
