using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotAlignmentOptimizer
{
    public class AttendeePool
    {
        Dictionary<string,Attendee> pool;
        public int maxid { get; private set; }
        public AttendeePool()
        {
            pool = new Dictionary<string,Attendee>();
            maxid = -1;
        }
        public Attendee Get(string name)
        {
            Attendee a;
            try
            {
                a = pool[name];
            }
            catch (KeyNotFoundException)
            {
                a = new Attendee(name);
                pool.Add(name, a);
            }
            if (a.getId() > maxid)
                maxid = a.getId();
            return a;
        }
        public int Count()
        {
            return pool.Count;
        }
    }
}
