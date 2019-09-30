using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DefenceAligner
{
    public partial class Form1 : Form
    {
        MyApp app;
        Dictionary<string, Control> formList;
        public Form1(MyApp app)
        {
            InitializeComponent();
            this.app = app;
            app.SetForm(this);
            formList = new Dictionary<string, Control>
            {
                { "スロット数", textBox1 },
                { "エポック数", textBox2 },
                { "繰り返し回数", textBox3 },
                { "減衰指数", textBox4 },
                { "重複数", label10 },
                { "日時", listBox4 },
                { "不都合日程", listBox1 },
                { "審査対象", listBox2 },
                { "審査室", listBox3 },
                { "表示エリア", listBox4 },
                { "チェックマーク：日時", label12 },
                { "チェックマーク：不都合日程", label14 },
                { "チェックマーク：審査リスト", label16 },
                { "チェックマーク：審査室", label18 },
                { "グラフ", chart2 }
            };

        }

        public Control GetControl(string name)
        {
            return formList[name];
        }

        public void CheckMark(string name, bool check)
        {
            Label label = (Label)GetControl(name);
            if (check)
                label.Image = global::DefenceAligner.Properties.Resources.image_check;
            else
                label.Image = global::DefenceAligner.Properties.Resources.image_delete;
        }

        private void MenuExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ListBoxDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    app.ReadExcel(files[i]);

                } catch (DatabaseException ex)
                {
                    MessageBox.Show(ex.ToString(), "エラー",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Error);
                    return;

                }
            }
            app.DisplayExaminer(listBox1);
            app.DisplayRooms(listBox3);
            app.DisplaySlot(listBox4);
            app.DisplayStudent(listBox2);
            app.SetSlotNumber();
        }

        private void ListBoxDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ChooseDB(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                FileName = "論文審査データベース.db3",
                Filter = "Sqlite3 DB(*.db3;*.sqlite3)|*.db3;*.sqlite3|すべてのファイル(*.*)|*.*",
                FilterIndex = 1,
                Title = "論文審査データベースを指定してください（なければ新たに作成します）",
                CheckFileExists = false
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき、選択されたファイル名を表示する
                try
                {
                    app.OpenDB(dialog.FileName);
                }
                catch (DatabaseException ex)
                {
                    MessageBox.Show(ex.ToString(),
                                    "エラー",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Error);
                    return;
                }
                label2.Text = dialog.FileName;
                app.DisplayExaminer(listBox1);
                app.DisplayRooms(listBox3);
                app.DisplaySlot(listBox4);
                app.DisplayStudent(listBox2);
                app.SetSlotNumber();
            }
        }


        private void ShowSelected(object sender, EventArgs e)
        {

        }

        private void DoAlignment(object sender, EventArgs e)
        {
            app.DoAlignment(Int32.Parse(textBox1.Text), 
                Int32.Parse(textBox2.Text),
                Int32.Parse(textBox3.Text),
                Double.Parse(textBox4.Text));
            MessageBox.Show("計算終了", "Finished",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
        }
    }
}
