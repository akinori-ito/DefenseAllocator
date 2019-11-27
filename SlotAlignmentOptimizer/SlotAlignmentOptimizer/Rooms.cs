using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SlotAlignmentOptimizer
{
    // 2つのイベントを交換する
    public class Eventpair
    {
        int[] room;
        int[] slot;
        Rooms rooms;
        public Eventpair(Rooms rs, bool inSlot = false)
        {
            this.rooms = rs;
            room = new int[2];
            slot = new int[2];
            do
            {
                do
                {
                    room[0] = rs.rand_room();
                    room[1] = rs.rand_room();
                } while (!rooms.rooms[room[0]].changable || 
                         !rooms.rooms[room[1]].changable);
                do
                {
                    slot[0] = rs.rand_slot();
                    if (inSlot || rs.rooms[room[0]].Get(slot[0]).SlotFixed)
                    {
                        slot[1] = slot[0];
                        Event event1 = rs.rooms[room[1]].Get(slot[1]);
                        if (event1.SlotFixed && event1.FixedSlot != slot[1])
                            continue;
                    }
                    else
                        slot[1] = rs.rand_slot();
                } while (rs.rooms[room[0]].Get(slot[0]) == Event.Prohibit ||
                         rs.rooms[room[1]].Get(slot[1]) == Event.Prohibit);
            } while (room[0] == room[1] && slot[0] == slot[1]);
        }
        public Event GetEvent(int i)
        {
            return rooms.rooms[room[i]].Get(slot[i]);
        }
        public void swap()
        {
            Event e0 = rooms.rooms[room[0]].Get(slot[0]);
            Event e1 = rooms.rooms[room[1]].Get(slot[1]);
            rooms.rooms[room[0]].Set(slot[0], e1);
            rooms.rooms[room[1]].Set(slot[1], e0);
        }
    }

    // 複数の部屋のクラス
    // 同じ時間スロットで複数の部屋でイベントが開催される
    public class Rooms
    {
        public List<Room> rooms { get; }
        int max_events;
        Random rand = new Random();
        const double OVERLAP_PENALTY = 10;  // 同時間に同じ人が出現するペナルティ
        const double CHANGE_PENALTY = 0.01; // 連続するスロットに違う人が出現するペナルティ
        const double CONTINUE_BONUS = 0.1;  // 同じ人が連続するスロットに出現するボーナス
        public Rooms(int max)
        {
            max_events = max;
            rooms = new List<Room>();
        }
        // 同時刻に開催されるイベントで参加者が重なっている数 * OVERLAP_PENALTY をペナルティとして返す
        double exist_constraint(int t, int i, int j)
        {
            Event event1 = rooms[i].Get(t);
            Event event2 = rooms[j].Get(t);
            return event1.overlap(event2)*event1.Weight*event2.Weight;
        }
        // 連続する2イベントで異なる参加者数 * CHANGE_PENALTY をペナルティとして返す
        int change_constraint(int t, int i)
        {
            if (t == max_events - 1)
            {
                return 0;
            }
            Room room = rooms[i];
            int overlap = room.Get(t).overlap(room.Get(t + 1));
            int changenum = room.Get(t).numberOfAttendees() + room.Get(t + 1).numberOfAttendees() - overlap;
            return changenum;
        }
        int continue_count(int n_prof)
        {
            int[] cont_count = new int[n_prof]; //同じ部屋で連続して何回現れたかのカウンタ
            bool[] appear = new bool[n_prof];     //直前のスロットでその人が現れたかどうか
            int[] prevroom = new int[n_prof];     //直前のスロットでその人が現れた部屋
            int val = 0;
            for (int i = 0; i < n_prof; i++)
            {
                appear[i] = false;
                prevroom[i] = -1;
            }
            for (int t = 0; t < max_events; t++)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (!rooms[i].changable)
                        continue;
                    Event ev = rooms[i].Get(t);
                    foreach (var a in ev.Attendees)
                    {
                        int id = a.getId();
                        appear[id] = true;
                        if (prevroom[id] != i)
                        {
                            // 直前のスロットにその人が出現したが部屋が違う場合
                            // 食前までのスコアを加算
                            val += (cont_count[id] - 1) * (cont_count[id] - 1);
                            // 出現カウントを1にする
                            cont_count[id] = 1;
                            // 出現した部屋はi番目
                            prevroom[id] = i;
                        } else
                        {
                            // 同じ部屋の直前のスロットに同じ人がいる (prevroom[id] == i)
                            cont_count[id]++;
                        }
                    }
                }
                for (int i = 0; i < n_prof; i++)
                {
                    if (!appear[i] && cont_count[i] > 1)
                    {
                        // 今回は現れていないが、直前のスロットまで複数回連続して現れていた
                        val += (cont_count[i] - 1) * (cont_count[i] - 1);
                        cont_count[i] = 0;
                        prevroom[i] = -1;
                    }
                }
            }
            // 最後に直前までの連続出現をカウント
            for (int i = 0; i < n_prof; i++)
            {
                if (cont_count[i] > 1)
                {
                    val += (cont_count[i] - 1) * (cont_count[i] - 1);
                }
            }
            return val;
        }
        public double exist_count()
        {
            double val = 0;
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                for (int t = 0; t < max_events; t++)
                {
                    for (int j = i + 1; j < rooms.Count; j++)
                    {
                        val += exist_constraint(t, i, j);
                    }
                }
            }
            return val;
        }
        public int change_count()
        {
            int val = 0;
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                if (!rooms[i].changable)
                    continue;
                for (int t = 0; t < max_events; t++)
                {
                    val += change_constraint(t, i);
                }
            }
            return val;
        }
        public Room AddRoom(string name, bool changable = true)
        {
            var r = new Room(name, max_events);
            if (!changable)
                r.unchangable();
            rooms.Add(r);
            return r;
        }
        public int rand_room()
        {
            return rand.Next(rooms.Count);
        }
        public int rand_slot()
        {
            return rand.Next(max_events);
        }
        public override string ToString()
        {
            var str = new StringBuilder("<rooms>");
            for (var i = 0; i < rooms.Count; i++)
            {
                str.Append(rooms[i].ToString());
            }
            return str.Append("</rooms>").ToString();
        }
        // かき混ぜる
        public void shuffle(int Niter)
        {
            for (int i = 0; i < Niter; i++)
            {
                Eventpair p = new Eventpair(this);
                p.swap();
            }
        } 
        // とりあえず書いておく
        public void anneal(double inittemp, int Nepoch, int Niter, double tconst, 
            int Nprof, Action<int,int,double,int> callback = null,
            bool inSlot = false, bool complexloss = true)
        {
            double exist_loss = exist_count();
            int change_loss = change_count();
            int cont_bonus = continue_count(Nprof);
            double loss = exist_loss*OVERLAP_PENALTY+change_loss*CHANGE_PENALTY
                -cont_bonus*CONTINUE_BONUS;
            double temp = inittemp;
            for (int j = 0; j < Nepoch; j++)
            {
                for (int i = 0; i < Niter; i++)
                {
                    Eventpair p = new Eventpair(this,inSlot);
                    p.swap();
                    exist_loss = exist_count();
                    if (complexloss)
                    {
                        change_loss = change_count();
                        cont_bonus = continue_count(Nprof);
                    } else
                    {
                        change_loss = cont_bonus = 0;
                    }
                    double newloss = exist_loss * OVERLAP_PENALTY + change_loss * CHANGE_PENALTY
                        - cont_bonus * CONTINUE_BONUS;
                    if (newloss < loss)
                    {
                        loss = newloss;
                        if (callback != null)
                        {
                            callback(j, i, loss, (int)exist_loss);
                        }
                    }
                    else
                    {
                        double prob = Math.Exp((loss - newloss) / temp);
                        if (rand.NextDouble() < prob)
                        {
                            loss = newloss;
                            if (callback != null)
                            {
                                callback(j, i, loss, (int)exist_loss);
                            }
                        }
                        else
                        {
                            p.swap();
                        }
                    }
                }
                temp /= tconst;
            }
        }

    }
}
