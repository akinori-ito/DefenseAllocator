using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DefenceAligner
{
    public class SlotEvent
    {
        public int[,] Slotevent { get; private set; }
        int[] eventsInSlot;
        int[] conflictsInSlot;
        double[] slotscore;
        bool[,] conflict;
        bool[,] conflictingEvent;
        int max_slot;
        int n_event;
        Random rand;
        public double MaxScore { get; private set; }
        public int TotalConflict { get; private set; }
        public int MaxEventsInSlot { get; private set; }
        AvailableSlots availslots;
        List<DefenceEvent> all_event;

        const double CONFLICT_SCORE = 100.0;
        public SlotEvent(int max_slot, int n_event, List<DefenceEvent> all_event, AvailableSlots availslots)
        {
            this.max_slot = max_slot;
            this.n_event = n_event;
            this.availslots = availslots;
            this.all_event = all_event;
            conflict = new bool[n_event, n_event];
            conflictingEvent = new bool[max_slot, n_event];
            conflictsInSlot = new int[max_slot];
            rand = new Random();
            for (int i = 0; i < n_event; i++)
            {
                for (int j = i + 1; j < n_event; j++)
                {
                    conflict[i, j] = all_event[i].IsConflict(all_event[j]);
                    conflict[j, i] = conflict[i, j];
                }
            }

            Slotevent = new int[max_slot, n_event];
            slotscore = new double[max_slot];
            eventsInSlot = new int[max_slot];
            for (int i = 0; i < max_slot; i++)
            {
                for (int j = 0; j < n_event; j++)
                {
                    Slotevent[i, j] = 0;
                }
            }
            foreach (var ev in all_event)
            {
                var event_no = ev.Id - 1;
                var slot = availslots.Info[event_no].FirstAvailableSlot();
                if (slot == -1)
                    continue;
                //System.Console.WriteLine("(" + slot.ToString() + "," + event_no.ToString() + ")");
                Slotevent[slot, event_no] = 1;
            }
            calc_score();

        }
        // 一番スコアが高いスロットを選ぶ
        public int HighestSlot()
        {
            int hslot = 0;
            double maxscore = 0;
            for (int i = 0; i < max_slot; i++)
            {
                if (slotscore[i] > maxscore)
                {
                    hslot = i;
                    maxscore = slotscore[i];
                }
            }
            MaxScore = maxscore;
            return hslot;
        }
        // ゆらぎ
        double fluctuation()
        {
            return (4 + rand.NextDouble()) / 5;
            //return rand.NextDouble();
        }
        // 交換すべきイベントを選ぶ
        // スコアが最も高い（コンフリクトが多く，イベント数が多い）スロットの中で，
        // コンフリクトを起こしていて，かつ可能なスロット数が最大のイベントを選ぶ
        public int[] EventToSwap()
        {
            int[] result = new int[2];
            int hslot = HighestSlot();
            result[0] = hslot;
            double maxscore = 0;
            int hevent = 0;
            for (int i = 0; i < n_event; i++)
            {
                double score = 0;
                if (conflictingEvent[hslot, i])
                    score += CONFLICT_SCORE*fluctuation();
                score += availslots.Info[i].NAvailSlot;
                if (score > maxscore)
                {
                    maxscore = score;
                    hevent = i;
                }
            }
            result[1] = hevent;
            return result;
        }

        // イベントの交換
        // 交換先は，交換元のイベントとできるだけコンフリクトを起こしていなくて，かつイベント数の少ないスロット
        public void SwapEvent()
        {
            var s_event = EventToSwap();
            var currentslot = s_event[0];
            var event_id = s_event[1];
            var info = availslots.Info[event_id];
            double minscore = 1e10;
            int minslot = 0;
            for (int i = 0; i < max_slot; i++)
            {
                if (info.AvailSlot[i] == 1 && i != currentslot)
                {
                    double score = eventsInSlot[i];
                    for (int j = 0; j < n_event; j++)
                    {
                        if (Slotevent[i,j] == 1 && all_event[event_id].IsConflict(all_event[j]))
                        {
                            score += CONFLICT_SCORE;
                        }
                    }
                    if (score < minscore)
                    {
                        minscore = score;
                        minslot = i;
                    }
                }
            }
            using(var wr = new StreamWriter("log.txt",true))
            {
                wr.WriteLine("Event " + event_id.ToString() + ": " + currentslot.ToString() + " -> " + minslot.ToString());
            }
            Slotevent[currentslot, event_id] = 0;
            Slotevent[minslot, event_id] = 1;
            calc_score();
        }

        // スコア再計算
        public void calc_score()
        {
            TotalConflict = 0;
            MaxEventsInSlot = 0;
            for (int i = 0; i < max_slot; i++)
            {
                slotscore[i] = 0;
                eventsInSlot[i] = 0;
                conflictsInSlot[i] = 0;
                for (int j = 0; j < n_event; j++)
                    conflictingEvent[i, j] = false;
                for (int j = 0; j < n_event; j++)
                {
                    slotscore[i] += Slotevent[i, j];
                    eventsInSlot[i] += Slotevent[i, j];
                    for (int k = j+1; k < n_event; k++)
                    {
                        if (Slotevent[i,j] == 1 && Slotevent[i,k] == 1 &&
                            conflict[j,k])
                        {
                            slotscore[i] += CONFLICT_SCORE*fluctuation();
                            conflictingEvent[i, j] = true;
                            conflictingEvent[i, k] = true;
                            conflictsInSlot[i]++;
                        }
                    }
                }
                TotalConflict += conflictsInSlot[i];
                if (eventsInSlot[i] > MaxEventsInSlot)
                {
                    MaxEventsInSlot = eventsInSlot[i];
                }
            }
        }
    }
    public class AlignmentOptimizer
    {
        bool[,] prof_availslots;
        AvailableSlots event_availslots;
        int max_slot;
        int n_event;
        int n_prof;
        SlotEvent Slotevent;
        DBManip db;

        public AlignmentOptimizer(DBManip db, int max_slot)
        {
            this.max_slot = max_slot;
            this.db = db;
            n_event = db.EventCount();
            n_prof = db.ProfessorCount();
            event_availslots = new AvailableSlots(n_event, max_slot);
            prof_availslots = new bool[n_prof, max_slot];
            // 初期化
            for (int j = 0; j < max_slot; j++)
            {
                for (int i = 0; i < n_prof; i++)
                {
                    prof_availslots[i, j] = true;
                }
            }
            // 不都合日程読み込み
            for (int slot = 0; slot < max_slot; slot++)
            {
                var profs = db.GetProhibitProfs(slot + 1); // slotはoption base 1
                foreach (var p in profs) // p はoption base 1
                {
                    prof_availslots[p - 1, slot] = false;

                }
            }
            // イベントごとに可能なスロットを計算
            bool[] avail = new bool[max_slot];
            var all_event = new List<DefenceEvent>();
            foreach (DefenceEvent ev in db.EachEvent())
            {
                all_event.Add(ev);
                for (int i = 0; i < max_slot; i++)
                    avail[i] = true;
                foreach (var p in ev.Referee_id) // pはoption base 1
                {
                    if (p == -1)
                        break;
                    for (int i = 0; i < max_slot; i++)
                    {
                        avail[i] = (avail[i] && prof_availslots[p - 1, i]);
                    }
                }
                for (int i = 0; i < max_slot; i++)
                    if (!avail[i])
                        event_availslots.UnAvailable(ev.Id - 1, i);   // ev.Idもoption base 1
            }
            // 可能なスロットがないイベントがあるかどうかチェック
            event_availslots.CalcSummary();

            // 割り当て不能イベントを書き出す
            if (event_availslots.Impossible)
            {
                writeImpossible();
            }
            // 割り当て可能数が小さい方から処理する
            event_availslots.Sort();
            // 同時に開催できないイベントを調査
            // とりあえず最初に可能なスロットに全イベントを割付
            Slotevent = new SlotEvent(max_slot, n_event, all_event, event_availslots);
 
        }
        public void Swap()
        {
            Slotevent.SwapEvent();
        }
        public double Score()
        {
            return Slotevent.MaxScore;
        }
        public int Conflicts()
        {
            return Slotevent.TotalConflict;
        }
        public int MaxEventsInSlot()
        {
            return Slotevent.MaxEventsInSlot;
        }
        public int[,] Result()
        {
            return Slotevent.Slotevent;
        }
        void writeImpossible()
        {

            MessageBox.Show("割り当て不能な審査があります", "Impossible",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            string impossiblefilename = Program.GetFilename("impossible.csv", "Text CSV (*.csv)|*.csv|All files(*.*)|*.*");
            if (impossiblefilename == null)
            {
                MessageBox.Show("割り当て中止しました", "Aborted",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            using (var wr = new StreamWriter(impossiblefilename))
            {
                for (int i = 0; i < n_event; i++)
                {
                    if (event_availslots.Info[i].NAvailSlot == 0)
                    {
                        var ev = db.GetEvent(i + 1);
                        wr.WriteLine(ev.ToCSV(db));
                    }
                }
            }
        }
    }
}
