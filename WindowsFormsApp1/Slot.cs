using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefenceAligner
{
    public class SlotInfo
    {
        public int[] AvailSlot;
        public int NAvailSlot;
        public int EventId;
        public SlotInfo(int ev, int nslot)
        {
            AvailSlot = new int[nslot];
            for (int i = 0; i < nslot; i++)
                AvailSlot[i] = 1;
            EventId = ev;
        }
        public void Summary()
        {
            NAvailSlot = 0;
            for (int i = 0; i < AvailSlot.Length; i++)
                NAvailSlot += AvailSlot[i];
        }
        public bool IsAllocatable()
        {
            return NAvailSlot > 0;
        }
        public int FirstAvailableSlot()
        {
            for (int i = 0; i < AvailSlot.Length; i++)
            {
                if (AvailSlot[i] == 1)
                    return i;
            }
            return -1;
        }
        public IEnumerable<int> EachAvailableSlot()
        {
            for (int i = 0; i < AvailSlot.Length; i++)
            {
                if (AvailSlot[i] == 1)
                    yield return i;
            }
        }
    }
    public class AvailableSlots
    {
        public int NSlot { get; }
        public int NEvent { get; }
        public List<SlotInfo> Info { get; }
        public bool Impossible { get; set; }

        public AvailableSlots(int nevent, int nslot)
        {
            NSlot = nslot;
            NEvent = nevent;
            Info = new List<SlotInfo>(NEvent);
            for (int i = 0; i < NEvent; i++)
                Info.Add(new SlotInfo(i, NSlot));
            Impossible = false;
        }
        public void UnAvailable(int ev, int sl)
        {
            Info[ev].AvailSlot[sl] = 0;
        }
        public void CalcSummary()
        {
            for (int e = 0; e < NEvent; e++)
            {
                Info[e].Summary();
                if (!Info[e].IsAllocatable())
                    Impossible = true;
            }
        }
        public bool IsAllocatable(int ev)
        {
            return Info[ev].IsAllocatable();
        }
        public void Sort()
        {
            Info.Sort((a, b) => a.NAvailSlot - b.NAvailSlot);
        }
    }
}
