using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using NPOI.SS.UserModel;
using System.Xml.Linq;

namespace DefenceAligner
{
    public class Examiner
    {
        public string Name { get; }
        public string Title { get; }
        public Examiner(string name, string title)
        {
            this.Name = name;
            this.Title = title;
        }
    }
    public class ExcelReader1
    {
        public List<Examiner> Examiners { get; private set; }
        public List<string> Dates { get; private set; }
        public List<string> Times { get; private set; }
        public List<string> Rooms { get; private set; }
        public List<string> Campuses { get; private set; }

        public ExcelReader1(IWorkbook book)
        {
            Read1st(book.GetSheetAt(0));
        }
        void Read1st(ISheet sheet)
        {
            var namecol = new Dictionary<string, int>();
            // どの列が何のリストなのかを取得
            IRow row = sheet.GetRow(0);
            for (int i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                var cell = row.GetCell(i);
                if (cell == null)
                    continue;
                var colname = cell.StringCellValue;
                if (colname != "")
                {
                    namecol[colname] = i;
                }
            }
            Examiners = ReadExaminer(sheet, namecol["審査員リスト"], namecol["職名"]);
            Dates = Readcol(sheet, namecol["審査日リスト"]);
            Times = Readcol(sheet, namecol["審査時間リスト"]);
            Rooms = Readcol(sheet, namecol["会場リスト"]);
            Campuses = Readcol(sheet, namecol["キャンパスリスト"]);

        }
        List<string> Readcol(ISheet sheet, int col)
        {
            var res = new List<string>();
            int row = 1;
            Console.WriteLine("Reading column " + col.ToString());
            while (true)
            {
                var r = sheet.GetRow(row);
                var c = r.GetCell(col);
                if (c == null)
                {
                    row++;
                    continue;
                }
                string strval;
                try
                {
                    strval = c.StringCellValue;
                } catch (Exception exc)
                {
                    strval = "";
                }
                row++;
                if (strval == "")
                    break;
                if (strval == "―" ||
                    (strval.Length >= 3 && strval.Substring(0, 3) == "(例)"))
                    continue;
                res.Add(strval);
            }
            return res;
        }
        List<Examiner> ReadExaminer(ISheet sheet, int col, int col2)
        {
            var res = new List<Examiner>();
            int row = 1;
            while (true)
            {
                var r = sheet.GetRow(row);
                if (r == null) break;
                var strval = r.GetCell(col).StringCellValue;
                if (strval == "")
                    break;
                if (strval == "―" ||
                    (strval.Length >= 3 && strval.Substring(0, 3) == "(例)"))
                {
                    row++;
                    continue;
                }
                res.Add(new Examiner(strval.Replace(" ","").Replace("　",""), 
                    sheet.GetRow(row).GetCell(col2).StringCellValue));
                row++;
            }
            return res;
        }
    }

    public class Excel2DB
    {
        public DBManip DB { get; }
        public Excel2DB(string dbfilename)
        {
            try
            {
                DB = new DBManip(dbfilename);
            } catch(DatabaseException ex)
            {
                throw new DatabaseException(ex.Message+" データベースが開けません：" + dbfilename);
            }
        }

        public void Close()
        {
            DB.Close();
        }
        ICell GetColumnCell(IRow row, int col)
        {
            ICell v;
            try
            {
                v = row.GetCell(col);
            }
            catch
            {
                return null;
            }
            return v;
        }

        string GetColumnString(IRow row, int col)
        {
            ICell v = GetColumnCell(row,col);
            if (v == null)
                return "";
            string str;
            try
            {
                str = v.StringCellValue;
            }
            catch
            {
                str = "";
            }
            return str;
        }
        int GetColumnInteger(IRow row, int col)
        {
            ICell v = GetColumnCell(row, col);
            if (v == null)
                return 0;
            int val;
            try
            {
                val = (int)v.NumericCellValue;
            } catch
            {
                val = 0;
            }
            return val;
        }

        IWorkbook GetBook(string filename)
        {
            var stream = new FileStream(filename, FileMode.Open);
            var book = WorkbookFactory.Create(stream);
            stream.Close();
            return book;
        }


        // Excelのワークシートを読み、適切な処理を行う
        public void ReadExcel(string filename, Form1 form)
        {
            IWorkbook book = GetBook(filename);
            var sheet0 = new ExcelReader1(book);
            // 1ページ目の情報をデータベースに登録
            // 審査員情報
            foreach (var e in sheet0.Examiners)
            {
                DB.GetProfessorID(true, e.Name, e.Title, "");
            }
            // 部屋情報
            foreach (var r in sheet0.Rooms)
            {
                DB.PutRoom(r);
                form.CheckMark("チェックマーク：審査室", true);
            }
            DB.ConfirmOnlineRoom();
            // 日時情報
            foreach (var d in sheet0.Dates)
            {
                foreach (var t in sheet0.Times)
                {
                    DB.PutSlot(d, t.Replace(" ",""));
                }
                form.CheckMark("チェックマーク：日時", true);
            }
            ReadDaimoku(book.GetSheetAt(1));
            form.CheckMark("チェックマーク：審査リスト", true);
            ReadExProhibit(book.GetSheetAt(2));
            form.CheckMark("チェックマーク：不都合日程", true);
            ReadRoomProhibit(book.GetSheetAt(3));
        }
        // 学生情報読み込み
        public void ReadDaimoku(ISheet sheet)
        { 
            for (int i = 1; i < sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                var student_id = GetColumnString(row,1);
                if (student_id == "")
                    return;
                if (DB.IsRegistered("events", "STUDENT_NO", student_id))
                    continue;
                var department = GetColumnString(row,2);
                var student_name = GetColumnString(row,3);
                var title = GetColumnString(row,4);
                int[] prof_id = new int[7];
                int col = 5;
                for (int j = 0; j < 7; j++)
                {
                    prof_id[j] = -1; // not set
                }
                String[] titles =
                {
                    "教授","准教授","講師","客員教授","特任教授",
                    "特任准教授","助教","非常勤講師","名誉教授"
                };
                for (int j = 0; j < 7; j++)
                {
                    var name = GetColumnString(row,col+1).Replace("　","");
                    var t = GetColumnString(row,col).Replace("　", "");
                    if (titles.Contains(name))
                    {
                        var tmp = name;
                        name = t;
                        t = tmp;
                    }
                    //var note = GetColumnString(row,col + 2);
                    if (t != "")
                    {
                        prof_id[j] = DB.GetProfessorID(false, name, t, "");
                        if (prof_id[j] < 0)
                            throw new DatabaseException("審査員が登録されていません：" + name + "("+t+")");
                    }
                    col += 2;
                }
                var onlinestr = GetColumnString(row,col);
                int online = 0;
                if (onlinestr == "オンライン")
                    online = 1;
                else if (onlinestr != "対面")
                {
                    throw new DatabaseException(
                        (i+1).ToString()+"行"+
                        (col+1).ToString()+"列: "+
                        "オンラインか対面を指定してください");
                }
                //Console.WriteLine(student_id + " " + onlinestr+" "+online.ToString());
                DB.PutEvent("修士",student_id, department, student_name, title, prof_id,online);
            }
        }
        // 審査員の不都合日程読み込み
        public void ReadExProhibit(ISheet sheet)
        {
            // 実際のデータは6列目から始まる
            // 1行目は日付、２行目は時間
            // それぞれスロットに対応するかをまずチェックする
            IRow row0 = sheet.GetRow(0);
            IRow row1 = sheet.GetRow(1);
            string curdate = "";
            int lastCellNum = row1.LastCellNum;
            int[] colslot = new int[lastCellNum];
            for (int i = 0; i < lastCellNum; i++)
                colslot[i] = -1;
            for (int i = 5; i < lastCellNum; i++)
            {
                string date = GetColumnString(row0, i);
                if (date == "")
                    date = curdate;
                else
                {
                    date = date.Replace("\r", "").Replace("\n", "");
                    curdate = date;
                }
                string time = GetColumnString(row1, i);
                if (time == "")
                    break;
                time = time.Replace(" ", "").Replace("：",":");
                int slot = DB.GetSlot(date, time);
                colslot[i] = slot;
            }
            // 不都合日程の登録
            for (int rownum = 2; rownum < sheet.LastRowNum; rownum++)
            {
                var row = sheet.GetRow(rownum);
                string examiner = GetColumnString(row, 1);
                if (examiner == "")
                    continue;
                int prof_id = DB.GetProfessorID(false, examiner);
                if (prof_id < 0)
                {
                    throw new DatabaseException("審査員が登録されていません：" + examiner);
                }
                for (int i = 5; i < lastCellNum; i++)
                {
                    if (colslot[i] < 0)
                        break;
                    string s = GetColumnString(row, i);
                    if (s != "")
                    {
                        DB.PutProhibitDate(prof_id, colslot[i]);
                    }
                }
            }
        }
        // 部屋の利用日程読み込み
        public void ReadRoomProhibit(ISheet sheet)
        {
            // 実際のデータは4列目から始まる
            // 1行目は日付、２行目は時間
            // それぞれスロットに対応するかをまずチェックする
            IRow row0 = sheet.GetRow(0);
            IRow row1 = sheet.GetRow(1);
            string curdate = "";
            int lastCellNum = row1.LastCellNum;
            int[] colslot = new int[lastCellNum];
            for (int i = 0; i < lastCellNum; i++)
                colslot[i] = -1;
            for (int i = 3; i < lastCellNum; i++)
            {
                string date = GetColumnString(row0, i);
                if (date == "")
                    date = curdate;
                else
                {
                    date = date.Replace("\r", "").Replace("\n", "");
                    curdate = date;
                }
                string time = GetColumnString(row1, i);
                if (time == "")
                    break;
                time = time.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("：",":");
                int slot = DB.GetSlot(date, time);
                colslot[i] = slot;
            }
            // 不都合日程の登録
            for (int rownum = 2; rownum < sheet.LastRowNum; rownum++)
            {
                var row = sheet.GetRow(rownum);
                string room = GetColumnString(row, 1);
                if (room == "")
                    continue;
                int room_id = DB.GetRoomID(room.Replace(" ",""));
                if (room_id < 0)
                {
                    throw new DatabaseException("会場が登録されていません：" + room);
                }
                for (int i = 3; i < lastCellNum; i++)
                {
                    if (colslot[i] < 0)
                        break;
                    string s = GetColumnString(row, i);
                    if (s != "")
                    {
                        DB.PutProhibitRoom(room_id, colslot[i]);
                    }
                }
            }
        }
    }
}

