using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SlotAlignmentOptimizer;
using System.IO;
using System.Xml.Linq;
using System.Text;

namespace DefenceAligner
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(new MyApp()));
        }
        // ファイルを選ぶ
        public static string GetFilename(string default_fn, string filter, string title = "Open File")
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = default_fn;
            dlg.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dlg.Filter = filter;
            dlg.FilterIndex = 1;
            dlg.Title = title;
            dlg.CheckFileExists = false;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.FileName;
            }
            return null;
        }
    }

    public class MyApp
    {
        public Excel2DB excel2DB;
        public Rooms rooms;
        Series series;
        Form1 form;

        const int SlotFixThreshold = 3; // 割り当て可能スロットがこれ以下のイベントは最初に設定してから動かさない

        public MyApp()
        {
            excel2DB = null;
        }
        public void SetForm(Form1 form)
        {
            this.form = form;
        }
        public void Check_DB_is_not_open()
        {
            if (excel2DB == null)
            {
                throw new DatabaseException("データベースが開かれていません");
            }
        }
        public void OpenDB(string filename)
        {
            if (excel2DB != null)
            {
                excel2DB.Close();
            }
            try
            {
                excel2DB = new Excel2DB(filename);
            }
            catch (DatabaseException ex)
            {
                excel2DB = null;
                throw ex;
            }
        }
        public void ReadExcel(string filename)
        {
            Check_DB_is_not_open();
            try
            {
                excel2DB.ReadExcel(filename,form);
            }
            catch (Exception e)
            {
                throw new DatabaseException("データの読み込みでエラーが起きました(" + filename + ") " + e.ToString());
            }
        }
        // 新しい割り当てアルゴリズム実装
        internal void DoAlignment_bak(int max_slot, int nepoch, int niter, double tconst)
        {
            var opt = new AlignmentOptimizer(excel2DB.DB, max_slot);
            Chart chart = (Chart)form.GetControl("グラフ");
            Label countlabel = (Label)form.GetControl("重複数");
            series = new Series();
            chart.Series.Add(series);
            for (int iter = 0; iter < niter; iter++)
            {
                opt.Swap();
                series.Points.AddXY(iter, opt.Score());
                chart.Update();
                countlabel.Text = opt.Conflicts().ToString()+","+opt.MaxEventsInSlot().ToString();
                countlabel.Refresh();
            }
            chart.Series.Clear();
            DoAlignment1(max_slot, nepoch, niter, tconst, opt.Result());
        }
        internal void DoAlignment1(int max_slot, int nepoch, int niter, double tconst, int[,] slotevent)
        {
            Chart chart = (Chart)form.GetControl("グラフ");
            Label countlabel = (Label)form.GetControl("重複数");
            Check_DB_is_not_open();
            // 部屋を準備
            rooms = new Rooms(max_slot);
            var all_room = new List<Room>();
            foreach (var room_name in excel2DB.DB.EachRoom())
            {
                all_room.Add(rooms.AddRoom(room_name));
            }
            // 不都合日程の部屋
            var p_room = rooms.AddRoom("不都合日程");
            p_room.unchangable();
            all_room.Add(p_room);
            // すべてのイベントを部屋に割り当てる
            int n_event = excel2DB.DB.EventCount();
            var pool = new AttendeePool();
            var room_no = new int[max_slot];
            for (int i = 0; i < max_slot; i++)
                room_no[i] = 0;
            int max_room = all_room.Count-1;
            for (int event_id = 0; event_id < n_event; event_id++)
            {
                bool event_allocated = false;
                for (int slot = 0; slot < max_slot; slot++)
                {
                    if (room_no[slot] == max_room)
                        continue;
                    if (slotevent[slot, event_id] == 1)
                    {
                        DefenseEvent d_ev = excel2DB.DB.GetEvent(event_id + 1);
                        var ev = new Event(d_ev.Student_No);
                        for (int i = 0; i < 5; i++)
                        {
                            if (d_ev.Referee_id[i] != -1)
                            {
                                ev.Attendees.Add(pool.Get(excel2DB.DB.GetProfessorName(d_ev.Referee_id[i])));
                            }
                        }
                        all_room[room_no[slot]].addEvent(ev, slot);
                        room_no[slot]++;
                        event_allocated = true;
                        break;
                    }

                }
                if (!event_allocated)
                {
                    MessageBox.Show("審査"+event_id.ToString()+"が割り当てられませんでした", "Impossible",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Information);
                }
            }
            // 不都合日程割り当て
            for (int slot = 0; slot < max_slot; slot++)
            {
                var ev = new Event("不都合" + slot.ToString());
                foreach (var prof_id in excel2DB.DB.GetProhibitProfs(slot+1))
                {
                    var name = excel2DB.DB.GetProfessorName(prof_id);
                    ev.Attendees.Add(pool.Get(name));
                }
                p_room.addEvent(ev);
            }

            // ここから本番
            void callbacklinear(int epoch, int iter, double lossval, int losscount)
            {
                series.Points.AddXY(epoch * niter + iter, lossval);
                chart.Update();
                countlabel.Text = losscount.ToString();
                countlabel.Refresh();
            }
            double inittemp = 50.0;
            for (int failiter = 0; failiter < 3; failiter++)
            {
                series = new Series();
                //chart.ChartAreas.First().AxisX.IsLogarithmic = true;
                chart.Series.Add(series);
                // 全ルームの全スロットで入れ替え
                rooms.anneal(inittemp, nepoch, niter, tconst, pool.maxid + 1, callbacklinear, false);
                chart.Series.Clear();
                // 重複解消できたか
                if (countlabel.Text == "")
                    break;
                inittemp /= 10;
            }
            // 全ルームの同スロットで入れ替え
            double inittemp2 = 0.1;
            series = new Series();
            chart.Series.Add(series);

            rooms.anneal(inittemp2, nepoch, niter, tconst, pool.maxid + 1, callbacklinear, true);
            string resultfilename = Program.GetFilename("output.csv", "Text CSV (*.csv)|*.csv|All files(*.*)|*.*");
            if (resultfilename == null)
            {
                MessageBox.Show("中止しました", "Aborted",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            OutputResult(resultfilename, "<?xml version = \"1.0\" encoding = \"UTF-8\" ?>" + rooms.ToString());
        }

        internal void DoAlignment(int max_slot, int nepoch, int niter, double tconst)
        {
            Chart chart = (Chart)form.GetControl("グラフ");
            Label countlabel = (Label)form.GetControl("重複数");
            Check_DB_is_not_open();
            // 部屋を準備
            rooms = new Rooms(max_slot);
            var all_room = new List<Room>();
            foreach (var room_name in excel2DB.DB.EachRoom())
            {
                all_room.Add(rooms.AddRoom(room_name));
            }
            // 不都合日程の部屋
            var p_room = rooms.AddRoom("不都合日程");
            p_room.unchangable();
            all_room.Add(p_room);
            // すべてのイベントを適当に部屋に割り当てる
            int room_no = 0;
            var pool = new AttendeePool();
            foreach (var event_str in excel2DB.DB.EachEventString())
            {
                var ev = new Event(event_str[1]); //学籍番号
                for (int i = 5; i < 10; i++)
                {
                    if (event_str[i] != "")
                    {
                        ev.Attendees.Add(pool.Get(event_str[i]));
                    }
                }
                try
                {
                    all_room[room_no].addEvent(ev);
                } catch
                {
                    room_no++;
                    if (!all_room[room_no].changable)
                    {
                        throw new Exception("イベントが多すぎます");
                    }
                    all_room[room_no].addEvent(ev);
                }
            }
            // 不都合日程割り当て
            for (int slot = 0; slot < max_slot; slot++)
            {
                var ev = new Event("不都合" + slot.ToString());
                foreach (var prof_id in excel2DB.DB.GetProhibitProfs(slot+1))
                {
                    var name = excel2DB.DB.GetProfessorName(prof_id);
                    ev.Attendees.Add(pool.Get(name));
                }
                p_room.addEvent(ev);
            }
            // テスト用
            rooms.shuffle(1000);

            // ここから本番
            void callbacklinear(int epoch, int iter, double lossval, int losscount)
            {
                int n = epoch * niter + iter;
                series.Points.AddXY(n, lossval);
                chart.Update();
                countlabel.Text = losscount.ToString();
                countlabel.Refresh();
                form.SetIterNum(n);
            }
            double inittemp = 50.0;
            for (int failiter = 0; failiter < 3; failiter++)
            {
                series = new Series();
                //chart.ChartAreas.First().AxisX.IsLogarithmic = true;
                chart.Series.Add(series);
                // 全ルームの全スロットで入れ替え
                rooms.anneal(inittemp, nepoch, niter, tconst, pool.maxid + 1, callbacklinear, false);
                chart.Series.Clear();
                // 重複解消できたか
                if (countlabel.Text == "")
                    break;
                inittemp /= 10;
            }
            // 全ルームの同スロットで入れ替え
            double inittemp2 = 0.1;
            series = new Series();
            chart.Series.Add(series);

            rooms.anneal(inittemp2, nepoch, niter, tconst, pool.maxid+1, callbacklinear,true);

            string resultfilename = Program.GetFilename("output.csv", "Text CSV (*.csv)|*.csv|All files(*.*)|*.*");
            if (resultfilename == null)
            {
                MessageBox.Show("中止しました", "Aborted",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            OutputResult(resultfilename, "<?xml version = \"1.0\" encoding = \"UTF-8\" ?>" + rooms.ToString());
            //OutputResult("output.csv", "<?xml version = \"1.0\" encoding = \"UTF-8\" ?>"+rooms.ToString());
            string listfilename = Program.GetFilename("alllist.csv", "Text CSV (*.csv)|*.csv|All files(*.*)|*.*");
            if (resultfilename == null)
            {
                MessageBox.Show("中止しました", "Aborted",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            excel2DB.DB.WriteProfDefenseList(listfilename);
        }

        void OutputResult(string outfile, string result)
        {
            using (StreamWriter wr = new StreamWriter("output.xml"))
            {
                wr.WriteLine(result);
            }
            var resultxml = XDocument.Parse(result);
            var roomname = new List<string>();
            var events = new List<List<string>>();
            var eventnames = new List<List<string>>();
            int eventmax = 0;
            int nroom = 0;
            foreach (XElement r in resultxml.Element("rooms").Elements("room"))
            {
                roomname.Add(r.Attribute("name").Value);
                nroom++;
                var ev = new List<string>();
                var evname = new List<string>();
                int n = 0;
                foreach (XElement e in r.Elements("event"))
                {
                    var student_id = e.Attribute("name").Value;
                    evname.Add(student_id);
                    excel2DB.DB.PutEventSlotRoom(student_id, n + 1, nroom);
                    var att = new StringBuilder();
                    foreach (var a in e.Elements("attendee")) {
                        att.Append(a.Attribute("name").Value);
                        att.Append("\n");
                    }
                    ev.Add(att.ToString());
                    n++;
                    if (n > eventmax)
                        eventmax = n;
                }
                events.Add(ev);
                eventnames.Add(evname);
            }
            using (StreamWriter wr = new StreamWriter(outfile))
            {
                wr.Write("\"\",");
                for (int i = 0; i < roomname.Count; i++)
                    wr.Write("\""+roomname[i]+"\",,,");
                wr.WriteLine("重複");
                var dup = new DuplicateChecker(excel2DB.DB);
                for (int i = 0; i < eventmax; i++)
                {
                    dup.clear();
                    wr.Write("\"" + excel2DB.DB.GetSlot(i) + "\",");
                    for (int j = 0; j < events.Count; j++)
                    {
                        if (events[j].Count < i)
                            continue;
                        var id = eventnames[j][i];
                        var studentname = excel2DB.DB.GetStudentInfo(id,"STUDENT_NAME");
                        wr.Write("\"" + studentname + "\n" + id + "\",");
                        var title = excel2DB.DB.GetStudentInfo(id, "PAPER_TITLE");
                        wr.Write("\"" + title + "\",");
                        if (events[j][i] == "")
                        {
                            wr.Write("\"\",");
                        } else
                        {
                            wr.Write("\"○" + events[j][i] + "\",");
                            dup.check(events[j][i]);
                        }
                    }
                    var duplicates = dup.duplicates();
                    if (duplicates.Count > 0)
                    {
                        foreach (var d in duplicates)
                        {
                            wr.Write(d + " ");
                        }
                    }
                    wr.WriteLine("");
                }
            }
        }

        public void DisplayExaminer(ListBox lbox)
        {
            Check_DB_is_not_open();
            lbox.Items.Clear();
            foreach (string name in excel2DB.DB.Examiners())
            {
                lbox.Items.Add(name);
            }
        }

        public void DisplayRooms(ListBox lbox)
        {
            Check_DB_is_not_open();
            lbox.Items.Clear();
            foreach (string name in excel2DB.DB.Rooms())
            {
                lbox.Items.Add(name);
            }
        }
        public void DisplaySlot(ListBox lbox)
        {
            Check_DB_is_not_open();
            lbox.Items.Clear();
            foreach (string name in excel2DB.DB.EachSlot())
            {
                lbox.Items.Add(name);
            }
        }
        public void DisplayStudent(ListBox lbox)
        {
            Check_DB_is_not_open();
            lbox.Items.Clear();
            foreach (string[] eventitem in excel2DB.DB.EachEventString())
            {
                lbox.Items.Add(eventitem[1]+" "+eventitem[3]);
            }
        }
        public void SetSlotNumber()
        {
            TextBox box = (TextBox)form.GetControl("スロット数");
            box.Text = excel2DB.DB.Count("slot").ToString();
        }
        public void ShowSelected(string name, ListBox lbox)
        {
            lbox.Items.Clear();
            foreach (string student in excel2DB.DB.StudentsToExamine(name))
            {
                lbox.Items.Add(student);
            }
        }
    }
}
