using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SlotAlignmentOptimizer
{
    // 参加者のクラス
    public class Attendee
    {
        string name;
        int id;
        public double weight;
        static int seq = 0;
        public Attendee(string name)
        {
            this.name = name;
            this.id = seq;
            weight = 1.0;
            seq++;
        }
        public string getName()
        {
            return name;
        }
        public int getId()
        {
            return id;
        }
        public bool equals(Attendee a)
        {
            return id == a.id;
        }
        public override string ToString()
        {
            return "<attendee name=\"" + name + "\" id=\"" + id.ToString() + "\"/>";
        }
    }
}
