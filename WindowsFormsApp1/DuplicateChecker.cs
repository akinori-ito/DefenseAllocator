using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefenceAligner
{
    class DuplicateChecker
    {
        Dictionary<string, int> members;
        List<string> keys;
        public DuplicateChecker(DBManip db)
        {
            members = new Dictionary<string, int>();
            keys = new List<string>();
            foreach (var prof in db.Examiners())
            {
                members.Add(prof, 0);
                keys.Add(prof);
            }
        }
        public void clear()
        {
            foreach (var p in keys)
            {
                members[p] = 0;
            }
        }
        public void check(string persons)
        {
            foreach (var s in persons.Split(new char[] { '\n'}))
            {
                if (members.ContainsKey(s))
                    members[s]++;
            }
        }
        public List<string> duplicates()
        {
            var res = new List<string>();
            foreach (var p in keys)
            {
                if (members[p] > 1)
                {
                    res.Add(p);
                }
            }
            return (res);
        }
    }
}
