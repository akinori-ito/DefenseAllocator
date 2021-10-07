using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DefenceAligner
{

    public class DBManip
    {
        public SQLiteConnection conn;
        bool TableExists(string table)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select name from sqlite_master where type='table';";
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return false;
                    while (reader.Read())
                    {
                        var r = reader["name"].ToString();
                        if (r == table)
                            return true;

                    }
                }
            }
            return false;
        }
        public DBManip(string dbfilename)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbfilename,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Memory
            };
            conn = new SQLiteConnection(builder.ToString());
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                if (!TableExists("events"))
                {
                    cmd.CommandText = "CREATE TABLE events(" +
                        "ID INTEGER PRIMARY KEY,"+
                        "DEGREE NCHAR(4) NOT NULL," +
                        "STUDENT_NO NCHAR(8) NOT NULL," +
                        "DEPARTMENT NCHAR(15) NOT NULL," +
                        "STUDENT_NAME NCHAR(20) NOT NULL," +
                        "PAPER_TITLE TEXT NOT NULL," +
                        "ID1 INTEGER," +
                        "ID2 INTEGER," +
                        "ID3 INTEGER," +
                        "ID4 INTEGER," +
                        "ID5 INTEGER," +
                        "SLOT INTEGER," +
                        "ROOM INTEGER)";
                    issue(cmd);
                }
                if (!TableExists("rooms"))
                {
                    cmd.CommandText = "CREATE TABLE rooms(ROOM_ID INTEGER PRIMARY KEY, ROOM_NAME TEXT)";
                    issue(cmd);
                }
                if (!TableExists("professor"))
                {
                    cmd.CommandText = "CREATE TABLE professor(" +
                        "ID INTEGER PRIMARY KEY," +
                        "TITLE NCHAR(5) NOT NULL," +
                        "NAME NCHAR(20) NOT NULL," +
                        "NOTE TEXT)";
                    issue(cmd);
                }
//                if (!TableExists("date"))
//                {
//                    cmd.CommandText = "CREATE TABLE date(SLOT_ID INTEGER, DATE NCHAR(10), TIME NCHAR(30))";
//                    issue(cmd);
//                }
                if (!TableExists("prof_prohibit"))
                {
                    cmd.CommandText = "CREATE TABLE prof_prohibit(" +
                        "PROF_ID INTEGER,SLOT_ID INTEGER)";
                    issue(cmd);

                }
                if (!TableExists("slot"))
                {
                    cmd.CommandText = "CREATE TABLE slot(" +
                        "SLOT INTEGER PRIMARY KEY, DATE NCHAR(30),TIME NCHAR(60))";
                    issue(cmd);
                }
                if (!TableExists("room_prohibit"))
                {
                    cmd.CommandText = "CREATE TABLE room_prohibit(" +
                        "ROOM_ID INTEGER," +
                        "SLOT INTEGER)";
                    issue(cmd);
                }
            }
        }

        public void Close()
        {
            if (conn != null)
            {
                conn.Close();
            }
        }
        void issue(SQLiteCommand cmd)
        {
            try
            {
                cmd.ExecuteNonQuery();
            } catch
            {
                throw new DatabaseException("SQL error: command=" + cmd.CommandText);
            }
        }

        // 特定の項目（テーブル，カラム，値）の存在チェック
        public bool IsRegistered(string table, string column, string value)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT " + column + " FROM " + table + " WHERE " + column + "='" + value + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                        return true;
                }
            }
            return false;

        }
        // テーブルの項目数を調べる
        public int Count(string table)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM " + table;
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    int count = reader.GetInt32(0);
                    return count; 
                }
            }
        }
        // イベント数
        public int EventCount()
        {
            return Count("events");
        }
        // 審査員数
        public int ProfessorCount()
        {
            return Count("professor");
        }

        public int GetProfessorID(bool register, string name, string title = "", string note = "")
        {
            int id = -1;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ID FROM professor where NAME='" + 
                    name.Replace(" ","").Replace("　","") + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        return reader.GetInt32(0);
                    }
                }
                if (!register)
                    return -1;
                cmd.CommandText = "INSERT INTO professor (TITLE,NAME,NOTE) VALUES(" +
                    "'" + title + "'," +
                    "'" + name + "'," +
                    "'" + note + "')";
                issue(cmd);
                cmd.CommandText = "SELECT ID FROM professor where NAME='" + name + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    id = reader.GetInt32(0);
                }
            }
            return id;

        }
        // イベントの登録
        public void PutEvent(string degree, string student_id, string department,
            string student_name, string title, int[] prof_id)
        {
            using (var cmd = conn.CreateCommand())
            {
                var sql = new StringBuilder();
                var escapedtitle = title.Replace("'", "''");
                sql.Append("INSERT INTO events (DEGREE, STUDENT_NO, DEPARTMENT, STUDENT_NAME, PAPER_TITLE, ID1, ID2, ID3, ID4, ID5) VALUES(" +
                     "'" + degree + "'," +
                    "'" + student_id + "'," +
                    "'" + department + "'," +
                    "'" + student_name + "'," +
                    "'" + escapedtitle + "',");
                for (int j = 0; j < prof_id.Length; j++)
                {
                    sql.Append(prof_id[j].ToString());
                    if (j == prof_id.Length - 1)
                    {
                        sql.Append(")");
                    }
                    else
                    {
                        sql.Append(",");
                    }
                }
                cmd.CommandText = sql.ToString();
                issue(cmd);
            }
        }
        // イベントにスロットと部屋を登録
        public void PutEventSlotRoom(string student_id, int slot, int room)
        {
            using (var cmd = conn.CreateCommand())
            {
                var sql = "UPDATE events SET SLOT=" + slot.ToString() + ", ROOM=" + room.ToString() + " WHERE STUDENT_NO='" + student_id + "'";
                cmd.CommandText = sql;
                issue(cmd);
            }
        }
        // 部屋の登録
            public void PutRoom(string roomname)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ROOM_NAME from rooms where ROOM_NAME='" + roomname + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                        return;
                }
                cmd.CommandText = "INSERT INTO rooms (ROOM_NAME) VALUES('" + roomname + "')";
                issue(cmd);
            }
        }
        // 部屋名からIDを調べる
        public int GetRoomID(string roomname)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ROOM_ID from rooms where ROOM_NAME='" + roomname + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        throw new DatabaseException("部屋が登録されていません：" + roomname);
                    }
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }

        }
        // 教員IDから名前を調べる
        public string GetProfessorName(int id)
        {
            if (id < 0)
                return "";
            string name = "";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT NAME FROM professor WHERE ID=" + id.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader.GetString(0);
                    }
                }
            }
            return name;

        }
        // 学籍番号から情報を調べる
        public string GetStudentInfo(string student_no, string item)
        {
            string result = "";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT "+item+" FROM events WHERE STUDENT_NO='" + student_no+"'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.GetString(0);
                    }
                }
            }
            return result;

        }
        // 主査の一覧
        public List<string> MainExaminers()
        {
            var ex = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT NAME FROM professor WHERE ID IN " +
                    "(SELECT ID1 FROM events)";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ex.Add(reader.GetString(0));
                    }
                }
            }
            return ex;
        }
        // 審査員の一覧
        public List<string> Examiners()
        {
            var ex = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT NAME FROM professor";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ex.Add(reader.GetString(0));
                    }
                }
            }
            return ex;
        }
        // 審査員が担当する学生の一覧
        public List<string> StudentsToExamine(string examiner)
        {
            var st = new List<string>();
            var prof_id = GetProfessorID(false, examiner);
            using (var cmd = conn.CreateCommand())
            {
                var str = new StringBuilder();
                str.Append("SELECT STUDENT_NO,STUDENT_NAME from events WHERE ");
                for (int i = 1; i < 7; i++)
                {
                    str.Append("ID" + i.ToString() + "=" + prof_id.ToString() + " OR ");
                }
                str.Append("ID7=" + prof_id.ToString());
                cmd.CommandText = str.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        st.Add(reader.GetString(0) + " " + reader.GetString(1));
                    }
                }
            }
            return st;
        }
        // 全ての部屋について繰り返す
        public IEnumerable<string> EachRoom()
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ROOM_NAME FROM rooms";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }
        // 部屋の一覧を返す
        public List<string> Rooms()
        {
            var rm = new List<string>();
            foreach (var r in EachRoom())
            {
                rm.Add(r);
            }
            return rm;
        }
        // 全ての審査エントリについて繰り返す
        public IEnumerable<string[]> EachEventString()
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM events";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string[] val = new string[12];
                        // 1番目の要素はIDなので除く
                        for (int i = 0; i < 5; i++)
                            val[i] = reader.GetString(i+1);
                        for (int i = 5; i < 10; i++)
                        {
                            val[i] = GetProfessorName(reader.GetInt32(i+1));
                        }
                        yield return val;
                    }
                }
            }
        }
        // 全ての審査エントリについて繰り返す
        public IEnumerable<DefenseEvent> EachEvent()
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM events";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ev = new DefenseEvent(
                            reader.GetInt32(0),   // ID
                            reader.GetString(1),  // DEGREE
                            reader.GetString(2),  // STUDENT_NO
                            reader.GetString(3),  // DEPARTMENT
                            reader.GetString(4),  // STUDENT_NAME
                            reader.GetString(5)   // PAPER_TITLE
                        );

                        for (int i = 0; i < 5; i++)
                        {
                            ev.Referee_id[i] = reader.GetInt32(i+6);
                        }
                        yield return ev;
                    }
                }
            }
        }
        // IDから審査イベントを返す
        public DefenseEvent GetEvent(int id)
        {
            DefenseEvent ev = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM events WHERE ID="+id.ToString()+";";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ev = new DefenseEvent(
                            reader.GetInt32(0),   // ID
                            reader.GetString(1),  // DEGREE
                            reader.GetString(2),  // STUDENT_NO
                            reader.GetString(3),  // DEPARTMENT
                            reader.GetString(4),  // STUDENT_NAME
                            reader.GetString(5)   // PAPER_TITLE
                        );

                        for (int i = 0; i < 5; i++)
                        {
                            ev.Referee_id[i] = reader.GetInt32(i+6);
                        }

                    }
                }
            }
            return ev;
        }
        // 日時の登録
        public void PutSlot(string date, string time)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO slot (DATE,TIME) VALUES('" + date + "','" + time + "')";
                issue(cmd);
            }
        }

        // 日時からスロットを検索
        public int GetSlot(string date, string time)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SLOT from slot WHERE DATE='" + date + "' and TIME='" + time + "'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                        throw new DatabaseException(date + " " + time + "のスロットが見つかりません");
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }
        }

        // 番号からスロットを検索
        public string GetSlot(int n)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DATE,TIME from slot WHERE SLOT="+(n+1).ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                        throw new DatabaseException(n.ToString() + "番目のスロットが見つかりません");
                    reader.Read();
                    return reader.GetString(0)+" "+reader.GetString(1);
                }
            }
        }
        // スロットについて繰り返し
        public IEnumerable<string> EachSlot()
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM slot";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(1) + " " + reader.GetString(2);
                    }
                }
            }
        }

        // 不都合日程登録
        public void PutProhibitDate(string professor_name, string date, string time)
        {
            if (Count("date") == 0)
            {
                throw new DatabaseException("最初に日程を登録してください");
            }
            int prof_id = GetProfessorID(false, professor_name);
            int slot_id = GetSlot(date, time);
            if (prof_id == -1)
            {
                throw new DatabaseException("審査員が未登録です：" + professor_name);
            }
            this.PutProhibitDate(prof_id, slot_id);
        }
        public void PutProhibitDate(int prof_id, int slot_id)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO prof_prohibit VALUES(" +
                    prof_id.ToString() + "," +
                    slot_id.ToString() + ")";
                issue(cmd);
            }
        }
        // 不都合日程照会
        public List<int> GetProhibitProfs(int slot)
        {
            using (var cmd = conn.CreateCommand())
            {
                var res = new List<int>();
                cmd.CommandText = "SELECT PROF_ID FROM prof_prohibit WHERE SLOT_ID=" + slot.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(reader.GetInt32(0));
                    }
                }
                return res;
            }
        }
        // 部屋の利用不可日時の登録
        public void PutProhibitRoom(string room_name, string date, string time)
        {
            if (Count("date") == 0)
            {
                throw new DatabaseException("最初に日程を登録してください");
            }
            int room_id = GetRoomID(room_name);
            int slot_id = GetSlot(date, time);
            PutProhibitRoom(room_id, slot_id);

        }
        public void PutProhibitRoom(int room_id, int slot_id)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO room_prohibit VALUES(" +
                    room_id.ToString() + "," +
                    slot_id.ToString() + ")";
                issue(cmd);
            }
        }
        // 使えない部屋とスロット一覧で繰り返す
        public IEnumerable<Tuple<int,int>> EachProhibitRoom()
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM room_prohibit";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new Tuple<int,int>(reader.GetInt32(0),reader.GetInt32(1));
                    }
                }
            }

        }

        // 教員の担当学生一覧
        public void WriteProfDefenseList(string filename)
        {
            var quote = "\"";
            var quotec = "\",\"";
            using (var wr = new StreamWriter(filename))
            {
                wr.WriteLine("\"教員氏名\",\"学籍番号\",\"学生氏名\",\"発表日\",\"時間\",\"審査室\"");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT professor.NAME, events.STUDENT_NO, events.STUDENT_NAME, slot.DATE, slot.TIME, rooms.ROOM_NAME " +
                        "from ((events INNER JOIN professor ON events.ID1 = professor.ID OR events.ID2 = professor.ID OR events.ID3 = professor.ID OR events.ID4 = professor.ID OR " +
                        "events.ID5 = professor.ID) INNER JOIN slot ON slot.SLOT = events.SLOT) INNER JOIN rooms ON rooms.ROOM_ID = events.ROOM ORDER BY professor.NAME, slot.DATE, slot.TIME";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            wr.Write(quote + reader.GetString(0) + quotec);
                            wr.Write(reader.GetString(1) + quotec);
                            wr.Write(reader.GetString(2) + quotec);
                            wr.Write(reader.GetString(3) + quotec);
                            wr.Write(reader.GetString(4) + quotec);
                            wr.WriteLine(reader.GetString(5) + quote);
                        }
                    }
                }

            }
        }
    }
}
