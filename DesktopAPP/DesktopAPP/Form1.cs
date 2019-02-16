﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using MetroFramework.Components;
using MetroFramework.Forms;
using System.IO;
using System.Net;
using System.Data.SQLite;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace DesktopAPP
{
    public partial class Form1 : MetroForm
    {
        int computation_interrupt = 0;
        int max_value;

        SQLiteConnection conn = new SQLiteConnection("Data Source=test.db;Version=3;");
        SQLiteDataReader re;

        private Mutex db_mtx = new Mutex();

        string datadir = "Evaluates";
        string fprefix = "Test ";

        struct Params
        {

            public int dac;
            public int dac_step;
            public int start_mode;
            public int pulse_type;
            public int sync_type;
            public int conn_type;
            public int input_voltage_1;
            public int input_voltage_2;
            public int input_voltage_3;
            public int input_voltage_4;
            public string frequency;
            public int frequency2;


        }
        protected void init_db()
        {
            SQLiteConnection.CreateFile("test.db");
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(conn);

            string sql_create =

                         "CREATE TABLE Start (" +
                            "idStart integer primary key, " +
                            "start_name varchar(45) NOT NULL" +
                         ");" +
                         "CREATE TABLE Napr (idNapr integer primary key, znach_nap integer NOT NULL);" +
                         "CREATE TABLE Impul (idImpul integer primary key, name_impu varchar(45) NOT NULL);" +
                         "CREATE TABLE Podkl (idPodkl integer primary key, name_podkl varchar(45) NOT NULL);" +
                         "CREATE TABLE Syncho (idSyncho integer primary key, synch_name varchar(45) NOT NULL);" +
                         "CREATE TABLE chast (idchast integer primary key, znach_chast integer NOT NULL);" +

                         "CREATE TABLE info_table (" +
                         "id integer primary key AUTOINCREMENT," +
                         "Napr_idNapr integer NOT NULL REFERENCES Napr(idNapr)," +
                         "Impul_idImpul integer NOT NULL REFERENCES Impul(idImpul)," +
                         "chast_idchast integer NOT NULL REFERENCES chast(idchast)," +
                         "Start_idStart integer NOT NULL REFERENCES Start(idStart)," +
                         "Syncho_idSyncho integer NOT NULL REFERENCES Syncho(idSyncho)," +
                         "Podkl_idPodkl integer NOT NULL  REFERENCES Podkl(idPodkl)," +
                         "Cap integer NOT NULL," +
                         "name varchar(45) NOT NULL," +
                         "data BLOB" +
                         ");";

            cmd.CommandText = sql_create;
            cmd.ExecuteNonQuery();

            string insert =

                //параметры Режима старта
                "insert into Start(idStart,start_name) values (0,\"Внутренний старт\");" +
                "insert into Start(idStart,start_name) values (1,\"Внутренний старт с трансляцией\");" +
                "insert into Start(idStart,start_name) values (2,\"Внешний старт по фронту\");" +
                "insert into Start(idStart,start_name) values (3,\"Внешний старт по спаду\");" +
                //параметры аналоговой синхронизации
                "insert into Syncho(idSyncho,synch_name) values (0,\"Отсутствие\");" +
                "insert into Syncho(idSyncho,synch_name) values (1,\"по переходу вверх\");" +
                "insert into Syncho(idSyncho,synch_name) values (2,\"по переходу вниз\");" +
                "insert into Syncho(idSyncho,synch_name) values (3,\"по уровню выше\");" +
                "insert into Syncho(idSyncho,synch_name) values (4,\"по уровню ниже\");" +
                //параметры тактовых импульсов
                "insert into Impul(idImpul,name_impu) values (0,\"Внутренние\");" +
                "insert into Impul(idImpul,name_impu) values (1,\"Внутренние с трансляцией\");" +
                "insert into Impul(idImpul,name_impu) values (2,\"Внешние по фронту\");" +
                "insert into Impul(idImpul,name_impu) values (3,\"Внешние по спаду\");" +
                //параметры типа подключения
                "insert into Podkl(idPodkl,name_podkl) values (0,\"Заземленный канал АЦП модуля\");" +
                "insert into Podkl(idPodkl,name_podkl) values (1,\"Подача выходного сигнала на вход АЦП модуля\");" +
                //параметры входного напряжения
                "insert into Napr(idNapr,znach_nap) values (0,3000);" +
                "insert into Napr(idNapr,znach_nap) values (1,1000);" +
                "insert into Napr(idNapr,znach_nap) values (2,300);" +
                //параметры частоты работы
                "insert into chast(idchast,znach_chast) values (0,1000);" +
                "insert into chast(idchast,znach_chast) values (1,2000);" +
                "insert into chast(idchast,znach_chast) values (2,3000);" +
                "insert into chast(idchast,znach_chast) values (3,4000);" +
                "insert into chast(idchast,znach_chast) values (4,5000);";

            cmd.CommandText = insert;
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public Form1()
        {
            InitializeComponent();

            if (!File.Exists("test.db"))
            {
                init_db();
            }
            conn.Open();

            SQLiteCommand command_napr = new SQLiteCommand("select znach_nap from Napr", conn);
            re = command_napr.ExecuteReader();
            while (re.Read())
            {
                metroComboBox1.Items.Add(re.GetValue(0).ToString());
                metroComboBox2.Items.Add(re.GetValue(0).ToString());
                metroComboBox3.Items.Add(re.GetValue(0).ToString());
                metroComboBox4.Items.Add(re.GetValue(0).ToString());
            }

            SQLiteCommand command_chast = new SQLiteCommand("select znach_chast from chast", conn);
            re = command_chast.ExecuteReader();
            while (re.Read())
            {
                metroComboBox5.Items.Add(re.GetValue(0).ToString());
            }

            SQLiteCommand command_start = new SQLiteCommand("select start_name from Start", conn);
            re = command_start.ExecuteReader();
            while (re.Read())
            {
                metroComboBox6.Items.Add(re.GetValue(0).ToString());
            }

            SQLiteCommand command_analog = new SQLiteCommand("select synch_name from Syncho", conn);
            re = command_analog.ExecuteReader();
            while (re.Read())
            {
                metroComboBox7.Items.Add(re.GetValue(0).ToString());
            }

            SQLiteCommand command_impul = new SQLiteCommand("select name_impu from Impul", conn);
            re = command_impul.ExecuteReader();
            while (re.Read())
            {
                metroComboBox8.Items.Add(re.GetValue(0).ToString());
            }

            SQLiteCommand command_podkl = new SQLiteCommand("select name_podkl from Podkl", conn);
            re = command_podkl.ExecuteReader();
            while (re.Read())
            {
                metroComboBox9.Items.Add(re.GetValue(0).ToString());
            }
            UpdataMena();
            conn.Close();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            metroComboBox1.SelectedIndex = 0;
            metroComboBox2.SelectedIndex = 0;
            metroComboBox3.SelectedIndex = 0;
            metroComboBox4.SelectedIndex = 0;
            metroComboBox5.SelectedIndex = 4;
            metroComboBox6.SelectedIndex = 0;
            metroComboBox7.SelectedIndex = 0;
            metroComboBox8.SelectedIndex = 0;
            metroComboBox9.SelectedIndex = 1;
        }

        private Params get_params()
        {
            Params prms = new Params();
            this.Invoke(new Action(() =>
            {
                prms.input_voltage_1 = metroComboBox1.SelectedIndex;
                prms.input_voltage_2 = metroComboBox2.SelectedIndex;
                prms.input_voltage_3 = metroComboBox3.SelectedIndex;
                prms.input_voltage_4 = metroComboBox4.SelectedIndex;
                prms.frequency = metroComboBox5.Text;
                prms.frequency2 = metroComboBox5.SelectedIndex;

                //prms.chan_num = int.Parse(metroComboBox4.Text);

                prms.start_mode = metroComboBox6.SelectedIndex;
                prms.sync_type = metroComboBox7.SelectedIndex;
                prms.pulse_type = metroComboBox8.SelectedIndex;
                prms.conn_type = metroComboBox9.SelectedIndex;

                prms.dac = Convert.ToInt32(metroTextBox1.Text); //Цап
                prms.dac_step = Convert.ToInt32(numericUpDown1.Value); //шаг
            }));

            return prms;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            Params prms = get_params();


            if (metroComboBox1.SelectedIndex == 0)
            {
                max_value = 3000;
            }
            else if (metroComboBox1.SelectedIndex == 1)
            {
                max_value = 1000;
            }
            else if (metroComboBox1.SelectedIndex == 2)
            {
                max_value = 300;
            }
            try
            {
                if (max_value < prms.dac)
                {
                    MessageBox.Show("Значение ЦАП не может превышать размер входящего напряжения", "Ошибка параметров", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (prms.dac_step > prms.dac)
                {
                    MessageBox.Show("Значение шага для ЦАП не может превышать парметра ЦАП", "Ошибка параметров", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    new Thread(() => run_calculate()).Start();
                    metroButton2.Visible = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Значения для ЦАП или его шаг,не были указаны", "Ошибка параметров", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void unlockGui(bool state)
        {

            metroButton1.Enabled = state;
        }

        private void run_calculate()
        {
            this.Invoke(new Action(() =>
            {
                this.unlockGui(false);
            }));
            Params prms = get_params();


            if (metroRadioButton1.Checked)
            {

                int i = prms.dac;
                expirence(i, prms);

            }
            else if (metroRadioButton2.Checked)
            {
                for (int i = prms.dac; i <= max_value; i += prms.dac_step)
                {
                    if (1 == Interlocked.Exchange(ref computation_interrupt, 0))
                        break;
                    expirence(i, prms);
                }
            }
            this.Invoke(new Action(() =>
            {
                metroButton2.Visible = false;
                this.unlockGui(true);
            }));
        }

        private void expirence(int i, Params prms)
        {

            string param =
                
                "-j " + "1,2,3" +
                " -a " + prms.input_voltage_1+","+prms.input_voltage_2+","+prms.input_voltage_3+","+prms.input_voltage_4   
                + " -b " + prms.frequency
                + " -e " + prms.start_mode
                + " -f " + prms.pulse_type
                + " -g " + prms.sync_type
                + " -z " + prms.conn_type
                + " -p " + fprefix
                
                + " -q " + datadir;
            param += " -ca " + i;

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.Arguments = "/C " + "C:\\Users\\Brik\\Desktop\\проги\\ReadData\\Debug\\ReadData.exe " + param;
            process.Start();
            process.WaitForExit();

            int tmp = i;
            new Task(() => insert_entry(i, prms)).Start();

        }
       

        private void insert_entry(int i, Params prms)
        {

            var str = File.ReadAllBytes(datadir + "/" + fprefix + i + ".dat");

            db_mtx.WaitOne();
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(conn);

           

            string insert_inf =
             "insert into info_table (Napr_idNapr,Impul_idImpul,chast_idchast,Start_idStart,Syncho_idSyncho,Podkl_idPodkl,Cap,name,data) " +
            "values (" + prms.input_voltage_1 + "," + prms.pulse_type + "," + prms.frequency2 + "," + prms.start_mode + "," + prms.sync_type + "," + prms.conn_type + "," + i + ",'" + fprefix + i + "', @vanya);";
            cmd.CommandText = insert_inf;
            cmd.Parameters.Add("@vanya", DbType.Binary, str.Length).Value = str;
            cmd.ExecuteNonQuery();
            UpdataMena();
            conn.Close();
            db_mtx.ReleaseMutex();
            //File.Delete(datadir + "/" + fprefix + i + ".dat");



        }
        private void UpdataMena()
        {
            this.Invoke(new Action(() =>
            {
                metroGrid1.Rows.Clear();
            }));

            string inf =
                "Select id," +
                "znach_nap," +
                "znach_chast," +
                "Cap," +
                "start_name," +
                "name_Podkl," +
                "name_impu," +
                "Synch_name," +
                "name " +    
                "FROM info_table,Napr,Impul,chast,Start,Syncho,Podkl " +
                "WHERE info_table.Napr_IdNapr = Napr.idNapr " +
                "AND info_table.Impul_idImpul = impul.idImpul " +
                "AND info_table.chast_idchast = chast.idchast " +
                "AND info_table.Start_idStart = Start.idStart " +
                "AND info_table.Syncho_idSyncho = Syncho.idSyncho " +
                "AND info_table.Podkl_idPodkl = Podkl.idPodkl ";

            //conn.Open();

            SQLiteCommand cmd = new SQLiteCommand(inf, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<string[]> data = new List<string[]>();

            while (reader.Read())
            {
                data.Add(new string[9]);

                data[data.Count - 1][0] = reader[0].ToString();
                data[data.Count - 1][1] = reader[1].ToString();
                data[data.Count - 1][2] = reader[2].ToString();
                data[data.Count - 1][3] = reader[3].ToString();
                data[data.Count - 1][4] = reader[4].ToString();
                data[data.Count - 1][5] = reader[5].ToString();
                data[data.Count - 1][6] = reader[6].ToString();
                data[data.Count - 1][7] = reader[7].ToString();
                data[data.Count - 1][8] = reader[8].ToString();    
            }
            // conn.Close();
            foreach (string[] s in data)
                this.Invoke(new Action(() =>
                {
                    metroGrid1.Rows.Add(s);
                }));
            metroGrid1.ClearSelection();
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            metroButton2.Visible = false;
            Interlocked.Exchange(ref computation_interrupt, 1);
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int ind = metroGrid1.SelectedCells[0].RowIndex;
            string id = metroGrid1.CurrentRow.Cells[0].Value.ToString();
            metroGrid1.Rows.RemoveAt(ind);
            conn.Open();
            string delete = "DELETE FROM info_table WHERE id='" + id + "'";

            SQLiteCommand cmd = new SQLiteCommand(delete, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private void переснятьToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
