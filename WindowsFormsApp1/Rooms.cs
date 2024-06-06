using System;
using System.Collections.Generic;
using System.IO;
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
            bool rooms_ok(Room r1, Room r2)
            {
                if (!r1.changable || !r2.changable)
                    return false;
                return r1.online == r2.online;
            }
            do
            {
                do
                {
                    do
                    {
                        room[0] = rs.rand_room();
                        room[1] = rs.rand_room(rs.rooms[room[0]].online);
                    } while (!rooms_ok(rooms.rooms[room[0]], rooms.rooms[room[1]]));
                    slot[0] = rs.rand_slot();
                    var firstEvent = rs.rooms[room[0]].Get(slot[0]);
                    if (inSlot || firstEvent.SlotFixed)
                    {
                        slot[1] = slot[0];
                        Event event1 = rs.rooms[room[1]].Get(slot[1]);
                        if (event1.SlotFixed && event1.FixedSlot != slot[1])
                            continue;
                    }
                    else
                        slot[1] = rs.rand_slot();
                    //Console.WriteLine("Event1:({0},{1})  Event2:({2},{3})", slot[0], room[0], slot[1], room[1]);
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
        const double GAP_PENALTY = 0.01;    // 同じ審査員の審査にあいだが空いているペナルティ
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
        int cont_score(int x)
        {
            x--;
            if (x < 0) x = 0;
            return x * x;
        }
        // n_prof は審査委員の数
        // prev_mag は主査として連続した場合の倍率
        int continue_count(int n_prof, int prev_mag = 5)
        {
            int[] cont_count = new int[n_prof]; //同じ部屋で連続して何回現れたかのカウンタ
            int[] prim_cont_count = new int[n_prof]; //同じ部屋で連続して主査になるカウンタ
            bool[] appear = new bool[n_prof];     //直前のスロットでその人が現れたかどうか
            int[] prevroom = new int[n_prof];     //直前のスロットでその人が現れた部屋
            int[] prevorder = new int[n_prof];    //直前のスロットで何番目だったか
            int val = 0; //評価値
            for (int i = 0; i < n_prof; i++)
            {
                appear[i] = false;
                prevroom[i] = -1;
                prevorder[i] = -1; 
            }
            for (int t = 0; t < max_events; t++)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (!rooms[i].changable)
                        continue;
                    Event ev = rooms[i].Get(t);
                    for (int k = 0; k < ev.Attendees.Count; k++)
                    {
                        var a = ev.Attendees[k];
                        int id = a.getId();
                        appear[id] = true;
                        if (prevroom[id] != i)
                        {
                            // 直前のスロットにその人が出現したが部屋が違う場合
                            // 直前までのスコアを加算
                            val += cont_score(cont_count[id] - 1);
                            val += cont_score(prim_cont_count[id]-1)*prev_mag;
                            // 出現カウントを1にする
                            cont_count[id] = 1;
                            // 出現した部屋はi番目
                            prevroom[id] = i;
                            // 審査委員リストのk番目
                            prevorder[id] = k;
                            if (k == 0)
                            {
                                prim_cont_count[id] = 1;
                            } else
                            {
                                prim_cont_count[id] = 0;
                            }
                        } else
                        {
                            // 同じ部屋の直前のスロットに同じ人がいる (prevroom[id] == i)
                            cont_count[id]++;
                            if (k == 0 && prevorder[id] == 0)
                            {
                                //この部屋で主査であり、直前でも主査だった
                                prim_cont_count[id]++;
                            }
                            prevorder[id] = k;
                        }
                    }
                }
                for (int i = 0; i < n_prof; i++)
                {
                    if (!appear[i] && cont_count[i] > 1)
                    {
                        // 今回は現れていないが、直前のスロットまで複数回連続して現れていた
                        val += cont_score(cont_count[i] - 1);
                        val += cont_score(prim_cont_count[i] - 1) * prev_mag;
                        cont_count[i] = 0;
                        prim_cont_count[i] = 0;
                        prevroom[i] = -1;
                        prevorder[i] = -1;
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
        // 同じ審査員のイベントであいだが空いていることに対するペナルティの計算
        int gap_count(int n_prof)
        {
            int[][] appear = new int[n_prof][];
            int val = 0;
            for (int i = 0; i < n_prof; i++)
            {
                appear[i] = new int[max_events];
                for (int j = 0; j < max_events; j++)
                    appear[i][j] = 0;
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
                        appear[id][t] = 1;
                    }
                }
            }
            // 最後に直前までの連続出現をカウント
            for (int i = 0; i < n_prof; i++)
            {
                int prev = -1;
                for (int t = 0; t < max_events; t++)
                {
                    if (appear[i][t] == 1)
                    {
                        if (prev != -1)
                        {
                            int gap = prev - t - 1;
                            if (gap >= 4) gap = 4; //4スロット以上空いていたら、それ以上いくら空いていても同じ
                            val += gap;
                        }
                        prev = t;
                    }
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
        // param: online
        //  -1: any
        //  0: real
        //  1: online
        public int rand_room(int online = -1)
        {
            int r;
            do
            {
                r = rand.Next(rooms.Count);
            } while (!rooms[r].changable ||
                     (online >= 0 && rooms[r].online != online));
            return r;
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
            int gap_loss = gap_count(Nprof);
            double loss = exist_loss*OVERLAP_PENALTY+change_loss*CHANGE_PENALTY
                +gap_loss*GAP_PENALTY-cont_bonus*CONTINUE_BONUS;
            double temp = inittemp;
            StreamWriter log = new StreamWriter("process.log",true);
            for (int j = 0; j < Nepoch; j++)
            {
                for (int i = 0; i < Niter; i++)
                {
                    Eventpair p = new Eventpair(this, inSlot);
                    p.swap();
                    exist_loss = exist_count();
                    if (complexloss)
                    {
                        change_loss = change_count();
                        cont_bonus = continue_count(Nprof);
                        gap_loss = gap_count(Nprof);
                    } else
                    {
                        change_loss = cont_bonus = gap_loss = 0;
                    }
                    double newloss = exist_loss * OVERLAP_PENALTY + change_loss * CHANGE_PENALTY
                        + gap_loss*GAP_PENALTY - cont_bonus * CONTINUE_BONUS;
                    DateTime now = DateTime.Now;
                    if (newloss < loss)
                    {
                        loss = newloss;
                        log.WriteLine("Anneal: {0} {1} {2} {3}", j, i, newloss,now.ToString());
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
                            log.WriteLine("Anneal: {0} {1} {2} {3}", j, i, newloss,now.ToString());
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
            log.Close();
        }

    }
}
