using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SlotAlignmentOptimizer
{
    // イベントのクラス
    // イベントはイベント名と複数の参加者からなる
    public class Event
    {
        public string name { get; private set;  }
        public List<Attendee> Attendees { get; private set;  }
        public double Weight;
        public bool SlotFixed;
        public int FixedSlot;
        public static Event NullEvent = new Event("");
        public static Event Prohibit = new Event("利用不可");
        public Event(string name)
        {
            this.name = name;
            Attendees = new List<Attendee>();
            Weight = 1;
            SlotFixed = false;
            FixedSlot = -1; // スロット番号が負の場合はスロットが確定していないことをあらわす
        }
        // ２つのイベントで参加者が何人オーバーラップしているか調べる
        public int overlap(Event e)
        {
            int ov = 0;
            foreach (Attendee a1 in Attendees)
            {
                foreach (Attendee a2 in e.Attendees)
                {
                    if (a1.equals(a2))
                        ov++;
                }
            }
            return ov;
        }
        public int numberOfAttendees()
        {
            return Attendees.Count;
        }
        public override string ToString()
        {
            string s = "<event name=\""+name+"\">";
            foreach (Attendee a in Attendees)
            {
                s += a.ToString();
            }
            return s + "</event>";
        }
    }
}
