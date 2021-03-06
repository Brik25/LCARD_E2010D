﻿/*

GUI для получение параметров c ПЭД
Программист: Гайнуллин Рамиль Русланович
Начало разработки: 03.11.18
Окончание разработки:13.06.19
Стадия разработки: завершено
Дата последней модификации: 24.06.2019

*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using MetroFramework.Forms;
using System.IO;
using System.Data.SQLite;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using read;
using System.IO.Ports;
using System.Drawing;
using System.Text;
using System.Management;
using System.Reflection;

namespace DesktopAPP
{
    public partial class Form1 : MetroForm
    {
        int computation_interrupt = 0;
        int max_value;

        SQLiteConnection conn = new SQLiteConnection("Data Source=info_exp.db;Version=3;");
        SQLiteDataReader re;
        FolderBrowserDialog dialog;

        private Mutex db_mtx = new Mutex();

        string datadir = "Evaluates";
        string fprefix = "Test ";
        string dataOUT;
        string dataIN;
        string converdata;
        int num;
        int check_value; //convert проверка на действие


        struct Params
        {
            public int dac;
            public int dac_step;
            public int start_mode;
            public int pulse_type;
            public int sync_type;
            public int conn_type;
            public int[] input_voltage;
            public string frequency;
            public int frequency2;
            public List<int> channels;
            public string user_name;
        }

        protected void init_db()
        {
            SQLiteConnection.CreateFile("info_exp.db");
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(conn);
            

            string sql_create =
             "CREATE TABLE Start (idStart integer primary key,start_name varchar(45) NOT NULL);" +
             "CREATE TABLE Impul (idImpul integer primary key, name_impu varchar(45) NOT NULL);" +
             "CREATE TABLE Podkl (idPodkl integer primary key, name_podkl varchar(45) NOT NULL);" +
             "CREATE TABLE Syncho (idSyncho integer primary key, synch_name varchar(45) NOT NULL);" +
             "CREATE TABLE chast (idchast integer primary key, znach_chast integer NOT NULL);" +
             "CREATE TABLE input_voltage (code integer primary key, value integer NOT NULL);" +

             "CREATE TABLE info_table (" +
             "id integer primary key AUTOINCREMENT," +
             "Impul_idImpul integer NOT NULL REFERENCES Impul(idImpul)," +
             "chast_idchast integer NOT NULL REFERENCES chast(idchast)," +
             "Start_idStart integer NOT NULL REFERENCES Start(idStart)," +
             "Syncho_idSyncho integer NOT NULL REFERENCES Syncho(idSyncho)," +
             "Podkl_idPodkl integer NOT NULL REFERENCES Podkl(idPodkl)," +
             "chan_num integer NOT NULL," +
             "Cap integer NOT NULL," +
             "name varchar(45) NOT NULL," +
             "user_name varchar(45) NOT NULL" +
             ");" +

             "CREATE TABLE channel (" +
             "code integer primary key AUTOINCREMENT," +
             "data BLOB NOT NULL," +
             "num integer NOT NULL," +
             "file integer REFERENCES info_table(id)," +
             "input_voltage integer REFERENCES input_voltage(code)" +
             ");" +
             "CREATE TABLE Com_ports (id integer primary key, commands varchar(45) NOT NULL);";


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
            "insert into input_voltage(code,value) values (0,3000);" +
            "insert into input_voltage(code,value) values (1,1000);" +
            "insert into input_voltage(code,value) values (2,300);" +
            //параметры частоты работы
            "insert into chast(idchast,znach_chast) values (0,1000);" +
            "insert into chast(idchast,znach_chast) values (1,2000);" +
            "insert into chast(idchast,znach_chast) values (2,3000);" +
            "insert into chast(idchast,znach_chast) values (3,4000);" +
            "insert into chast(idchast,znach_chast) values (4,5000);"+
            //COM-порт
            "insert into Com_ports(id,commands) values (0,\"02 06 00 01 00 00 D8 39\");" +
            "insert into Com_ports(id,commands) values (1,\"02 06 00 01 00 01 19 F9\");";


            cmd.CommandText = insert;
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public Form1()
        {
            InitializeComponent();


            if (!File.Exists("info_exp.db"))
            {
                init_db();
            }
            conn.Open();


            //SQLiteCommand command_napr = new SQLiteCommand("select value_voltage from voltage", conn);
            SQLiteCommand command_napr = new SQLiteCommand("select value from input_voltage", conn);
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

            SQLiteCommand com_command = new SQLiteCommand("select commands from Com_ports", conn);
            re = com_command.ExecuteReader();
            while (re.Read())
            {
                metroComboBox10.Items.Add(re.GetValue(0).ToString());
                metroComboBox16.Items.Add(re.GetValue(0).ToString());
                metroComboBox17.Items.Add(re.GetValue(0).ToString());
                metroComboBox18.Items.Add(re.GetValue(0).ToString());
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
            metroComboBox2.Enabled = false;
            metroComboBox3.Enabled = false;
            metroComboBox4.Enabled = false;

            numericUpDown1.Maximum = 2000;
            numericUpDown1.Minimum = 100;
            
            numericUpDown2.Minimum = 3;
            numericUpDown2.Maximum = 100;

            numericUpDown3.Maximum = 8000;
            numericUpDown3.Minimum = -8000;
            
            //COM ports
            string[] ports = SerialPort.GetPortNames();
            metroComboBox11.Items.AddRange(ports);
            metroComboBox12.SelectedIndex = 2;
            metroComboBox13.SelectedIndex = 2;
            metroComboBox14.SelectedIndex = 0;
            metroComboBox15.SelectedIndex = 0;
            Ruch.Checked = true;
            groupBox4.Enabled = false;
           
            //статус девайса
            check_device();

        }



        private void metroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {

            if (metroCheckBox1.Checked == true)
            {
                metroComboBox1.Enabled = true;
            }
            else
            {
                metroComboBox1.Enabled = false;
            }
        }

        private void metroCheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (metroCheckBox2.Checked == true)
            {
                metroComboBox2.Enabled = true;
            }
            else
            {
                metroComboBox2.Enabled = false;
            }
        }

        private void metroCheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (metroCheckBox3.Checked == true)
            {
                metroComboBox3.Enabled = true;
            }
            else
            {
                metroComboBox3.Enabled = false;
            }
        }

        private void metroCheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (metroCheckBox4.Checked == true)
            {
                metroComboBox4.Enabled = true;
            }
            else
            {
                metroComboBox4.Enabled = false;
            }
        }

        private void metroCheckBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (metroCheckBox5.Checked == true)
            {
                metroCheckBox5.Text = "Серия экспериментов";
                numericUpDown1.Minimum = 100; 
                numericUpDown1.Value = 100;
                numericUpDown1.Enabled = true;
                numericUpDown3.Enabled = true;
                metroLabel16.Visible = true;
                metroLabel9.Visible = false;

            }
            else
            {
                metroCheckBox5.Text = "Одинарный эксперимент";
                numericUpDown1.Minimum = 0;
                numericUpDown1.Value = 0;
                numericUpDown1.Enabled = false;               
                numericUpDown3.Enabled = false;
                metroLabel16.Visible = false;
                metroLabel9.Visible = true;

            }
        }

        private Params get_params()
        {
            Params prms = new Params();
            prms.input_voltage = new int[4];
            this.Invoke(new Action(() =>
            {
                prms.input_voltage[0] = metroComboBox1.SelectedIndex;
                prms.input_voltage[1] = metroComboBox2.SelectedIndex;
                prms.input_voltage[2] = metroComboBox3.SelectedIndex;
                prms.input_voltage[3] = metroComboBox4.SelectedIndex;
                prms.frequency = metroComboBox5.Text; //частота
                prms.frequency2 = metroComboBox5.SelectedIndex;
                prms.start_mode = metroComboBox6.SelectedIndex;
                prms.sync_type = metroComboBox7.SelectedIndex;
                prms.pulse_type = metroComboBox8.SelectedIndex;
                prms.conn_type = metroComboBox9.SelectedIndex;
                prms.dac = Convert.ToInt32(numericUpDown4.Value); //Цап
                prms.dac_step = Convert.ToInt32(numericUpDown1.Value); //шаг

                prms.channels = new List<int>();

                if (metroCheckBox1.Checked == true)
                    prms.channels.Add(1);
                if (metroCheckBox2.Checked == true)
                    prms.channels.Add(2);
                if (metroCheckBox3.Checked == true)
                    prms.channels.Add(3);
                if (metroCheckBox4.Checked == true)
                    prms.channels.Add(4);
            }));

            return prms;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            check_device();
            if (metroLabel24.Text == "E20-10")
            {

                List<int> start_button = new List<int>();
                if (metroCheckBox1.Checked == true)
                    start_button.Add(1);
                if (metroCheckBox2.Checked == true)
                    start_button.Add(2);
                if (metroCheckBox3.Checked == true)
                    start_button.Add(3);
                if (metroCheckBox4.Checked == true)
                    start_button.Add(4);

                if (start_button.Count == 0)
                {
                    MessageBox.Show("Не выбран ни один канал", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Params prms = get_params();
                    max_value = Convert.ToInt32(numericUpDown3.Value);
                    try
                    {               
                        if(max_value < prms.dac)
                        {
                            MessageBox.Show("Параметр максимального значения не может быть меньше минимального", "Ошибка", MessageBoxButtons.OK,MessageBoxIcon.Error);
                        }

                        
                            num = 0;
                            new Thread(() => run_calculate()).Start();
                            metroButton2.Visible = true;                       
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Значения для ЦАП или его шаг,не были указаны", "Ошибка параметров", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else 
            {
                MessageBox.Show("Ошибка с модулем L-Card,проверьте подключение!", "Ошибка", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {

            metroButton2.Visible = false;
            Interlocked.Exchange(ref computation_interrupt, 1);

        }


        void delete()
        {
            int ind = metroGrid1.SelectedCells[0].RowIndex;
            string id = metroGrid1.CurrentRow.Cells[0].Value.ToString();
            metroGrid1.Rows.RemoveAt(ind);
            conn.Open();
            string delete = "DELETE FROM info_table WHERE id='" + id + "';DELETE FROM channel WHERE file = '" + id + "'";

            SQLiteCommand cmd = new SQLiteCommand(delete, conn);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
          
            DialogResult dialog = MessageBox.Show("Вы хотите удалить данный эксперимент?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog == DialogResult.Yes)
            {
                delete();
            }
        }

        private void графикToolStripMenuItem_Click(object sender, EventArgs e)
        {
            metroCheckBox6.Enabled = true;
            new Thread((worker) => grapth()).Start();
            void grapth()
            {
                this.Invoke(new Action(() =>
                {
                    this.unlockGui(false);
                }));
                try
                {
                    string name = "Grpth";
                    check_value = 1;
                    convert(name, check_value);
                    ReadClass rc = new ReadClass();
                    MWCharArray mlfname = new MWCharArray(name + ".txt");
                    rc.read(0, mlfname);
                    File.Delete("Grpth.txt");
                }
                catch
                {
                    DialogResult dialog = MessageBox.Show("Данные для построения графика отсутствуют,причина:входной сигнал не пришел на модуль L-Card.Удалить эксперимент?", "Ошибка", MessageBoxButtons.YesNo,MessageBoxIcon.Error);
                    if (dialog == DialogResult.Yes)
                    {
                        this.Invoke(new Action(() =>
                        {
                           delete();
                           File.Delete("Grpth.txt");
                        }));

                    }
                    File.Delete("Grpth.txt");
                }
                this.Invoke(new Action(() =>
                {
                    metroButton2.Visible = false;
                    this.unlockGui(true);
                }));
            }
        }

        private void переснятьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Вы хотите переснять данный эксперимент?", "Переснять", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog == DialogResult.Yes)
            {
                metroCheckBox6.Checked = true;
                metroCheckBox5.Enabled = false;
                new Thread(() => run_calculate()).Start();
            }            
        }

        private void экспортToolStripMenuItem_Click(object sender, EventArgs e)
        {
            metroCheckBox6.Enabled = false;

            this.Invoke(new Action(() =>
            {
                this.unlockGui(false);
            }));

            string name = metroGrid1.CurrentRow.Cells[7].Value.ToString();
            dialog = new FolderBrowserDialog();
            dialog.Description = "Сохранить в:";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                check_value = 0;
                converdata = dialog.SelectedPath;
                string converdata2 = converdata + "/" + name + ".txt";
                
                if (!File.Exists(converdata2))
                {
                    convert(name, check_value);
                    MessageBox.Show("Данные сохранены успешно! ");
                }
                else
                {
                    DialogResult dialog = MessageBox.Show("Такой файл существует,вы хотите перезаписать его?", "Перезаписать", MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                    if (dialog == DialogResult.Yes)
                    {
                        convert(name, check_value);
                        MessageBox.Show("Данные сохранены успешно!");
                    }
                }
            }

            this.Invoke(new Action(() =>
            {
                metroButton2.Visible = false;
                this.unlockGui(true);
            }));
        }

        private void unlockGui(bool state)
        {
            metroContextMenu1.Enabled = state;
            metroButton1.Enabled = state;
            numericUpDown2.Enabled = state;
            metroTabControl1.Enabled = state;
            metroComboBox5.Enabled = state;
            metroComboBox6.Enabled = state;
            metroComboBox7.Enabled = state;
            metroComboBox8.Enabled = state;
            metroComboBox9.Enabled = state;
            metroTextBox2.Enabled = state;

          
        }

        private void run_calculate()
        {
            this.Invoke(new Action(() =>
            {
                this.unlockGui(false);
                    
            }));
            Params prms = get_params();


            if (metroCheckBox5.Checked == false || metroCheckBox6.Checked == true)
            {

                int i = prms.dac;
                expirence(i, prms);

            }
            else if (metroCheckBox5.Checked == true)
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
                "-j " + string.Join(",", prms.channels.ConvertAll(el => el.ToString()).ToArray())
                + " -a " + prms.input_voltage[0] + "," + prms.input_voltage[1] + "," + prms.input_voltage[2] + "," + prms.input_voltage[3]
                + " -b " + prms.frequency
                + " -e " + prms.start_mode
                + " -f " + prms.pulse_type
                + " -g " + prms.sync_type
                + " -z " + prms.conn_type
                + " -p " + fprefix
                + " -q " + datadir
                + " -time " + numericUpDown2.Value;
            param += " -ca " + i;

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.Arguments = "/C "+ @"\ReadData\ReadData.exe " + param;
            process.Start();
            process.WaitForExit();

            int tmp = i;
            
                new Task(() => insert_entry(i, prms)).Start();
        }

        private void insert_entry(int i, Params prms)
        {
            try
            {
                num++;
                db_mtx.WaitOne();
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn);

                var data = File.ReadAllBytes(datadir + "/" + fprefix + i + ".dat");

                List<byte>[] channels_data = new List<byte>[prms.channels.Count];
                for (int n = 0; n < prms.channels.Count; ++n)
                {
                    channels_data[n] = new List<byte>();
                }
                for (int n = 0, m = 0, k = 0; n < data.Length; ++n, ++m)
                {
                    if (m == 2)
                    {
                        ++k;
                        m = 0;
                    }
                    if (k == prms.channels.Count)
                        k = 0;
                    channels_data[k].Add(data[n]);
                }

                if (metroTextBox2.Text == "")
                {
                    if (metroCheckBox5.Checked == true)
                    {
                        prms.user_name = "Серия_ " + num;
                    }
                    else
                    {
                        prms.user_name = "Эксперимент";
                    }

                }
                else
                {

                    if (metroCheckBox5.Checked == true)
                    {
                        prms.user_name = "Серия_" + metroTextBox2.Text + "_" + num;
                    }
                    else
                    {
                        prms.user_name = "Эксперимент_" + metroTextBox2.Text;
                    }
                }

                if (metroCheckBox5.Enabled == true)
                {
                    try
                    {
                        string insert_file_sql =
                        "   insert into info_table (" +
                        "   Impul_idImpul," +
                        "   chast_idchast," +
                        "   Start_idStart," +
                        "   Syncho_idSyncho," +
                        "   Podkl_idPodkl," +
                        "   Cap," +
                        "   name," +
                        "   user_name," +
                        "   chan_num) " +
                        "   values (" +

                        prms.pulse_type + "," +
                        prms.frequency2 + "," +
                        prms.start_mode + "," +
                        prms.sync_type + "," +
                        prms.conn_type + "," +
                        i + ",'" +
                        fprefix + i + "','" +
                        prms.user_name + "'," +
                        prms.channels.Count +
                        ");";

                        cmd.CommandText = insert_file_sql;
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "select last_insert_rowid()";
                        long file_id = (long)cmd.ExecuteScalar();

                        for (int n = 0; n < prms.channels.Count; ++n)
                        {
                            string upload_sql =
                                "insert into channel (data, num, file, input_voltage) values (@data, " + prms.channels[n] + ", " + file_id + ", " + prms.input_voltage[prms.channels[n] - 1] + ");";
                            cmd.CommandText = upload_sql;
                            byte[] channel_data = channels_data[n].ToArray();
                            cmd.Parameters.Add("@data", DbType.Binary, channel_data.Length).Value = channel_data;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {

                    }

                }
                else if (metroCheckBox6.Checked == true && metroCheckBox5.Enabled == false)
                {
                    string id = metroGrid1.CurrentRow.Cells[0].Value.ToString();
                    string delete = "Delete FROM channel where file = " + id;
                    cmd.CommandText = delete;
                    cmd.ExecuteNonQuery();

                    string update_file_sql =
                                            "UPDATE info_table SET " +
                                            "impul_idImpul='" + prms.pulse_type + "'," +
                                            "chast_idchast='" + prms.frequency2 + "'," +
                                            "Start_idStart='" + prms.start_mode + "'," +
                                            "Syncho_idSyncho='" + prms.sync_type + "'," +
                                            "Podkl_idPodkl='" + prms.conn_type + "'," +
                                            "Cap='" + i + "'," +
                                            "name='" + fprefix + i + "'," +
                                            "chan_num='" + prms.channels.Count + "' " +
                                            "WHERE id ='" + id + "'";
                    cmd.CommandText = update_file_sql;
                    cmd.ExecuteNonQuery();

                    for (int n = 0; n < prms.channels.Count; ++n)
                    {
                        string upload_sql =
                            "insert into channel (data, num, file, input_voltage) values (@data, " + prms.channels[n] + ", " + id + ", " + prms.input_voltage[prms.channels[n] - 1] + ");";
                        cmd.CommandText = upload_sql;
                        byte[] channel_data = channels_data[n].ToArray();
                        cmd.Parameters.Add("@data", DbType.Binary, channel_data.Length).Value = channel_data;
                        cmd.ExecuteNonQuery();
                    }

                    this.Invoke(new Action(() =>
                    {
                        metroCheckBox6.Checked = false;
                        metroCheckBox5.Enabled = true;
                    }));
                }

                UpdataMena();
                conn.Close();
                db_mtx.ReleaseMutex();
                File.Delete(datadir + "/" + fprefix + i + ".dat");
            }
            catch(Exception)
            {
                conn.Close();
            }
        }

        private void UpdataMena()
        {
            this.Invoke(new Action(() =>
            {
                metroGrid1.Rows.Clear();
            }));

            string inf =
                "Select id," +
                "znach_chast," +
                "Cap," +
                "start_name," +
                "Synch_name," +
                "name_impu," +
                "name_Podkl," +
                "user_name " +
                "FROM info_table,Impul,chast,Start,Syncho,Podkl " +

                "where info_table.Impul_idImpul = impul.idImpul " +
                "AND info_table.chast_idchast = chast.idchast " +
                "AND info_table.Start_idStart = Start.idStart " +
                "AND info_table.Syncho_idSyncho = Syncho.idSyncho " +
                "AND info_table.Podkl_idPodkl = Podkl.idPodkl ";

            SQLiteCommand cmd = new SQLiteCommand(inf, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<string[]> data = new List<string[]>();

            while (reader.Read())
            {
                data.Add(new string[8]);

                data[data.Count - 1][0] = reader[0].ToString();
                data[data.Count - 1][1] = reader[1].ToString();
                data[data.Count - 1][2] = reader[2].ToString();
                data[data.Count - 1][3] = reader[3].ToString();
                data[data.Count - 1][4] = reader[4].ToString();
                data[data.Count - 1][5] = reader[5].ToString();
                data[data.Count - 1][6] = reader[6].ToString();
                data[data.Count - 1][7] = reader[7].ToString();
            }
            foreach (string[] s in data)
                this.Invoke(new Action(() =>
                {
                    metroGrid1.Rows.Add(s);
                }));
            
        }

        private void convert(string name, int check_value)
        {

            string id = metroGrid1.CurrentRow.Cells[0].Value.ToString();

            SQLiteCommand cmd = new SQLiteCommand(
                "SELECT data, input_voltage.value, num FROM channel " +
                "JOIN info_table ON channel.file = info_table.id " +
                "JOIN input_voltage ON channel.input_voltage = input_voltage.code " +
                "WHERE info_table.id = " + id, conn);
            conn.Open();

            using (var reader = cmd.ExecuteReader())
            {

                List<List<double>> result = new List<List<double>>();
                List<string> header = new List<string>();

                while (reader.Read())
                {
                    long input_voltage = (long)reader.GetValue(1);
                    List<double> decnums = new List<double>();

                    header.Add(reader.GetValue(2).ToString());

                    byte[] bindata = GetBytes(reader);

                    const int numbytes = 2;
                    byte[] buf = new byte[numbytes];

                    for (int i = 0, bytenum = 0; i < bindata.Length; ++i, ++bytenum)
                    {
                        if (bytenum == numbytes)
                        {
                            bytenum = 0;
                            double val = BitConverter.ToInt16(buf, 0);

                            if (metroCheckBox6.Enabled == false)
                            {
                               val = val * input_voltage / 8000;
                            }
                            
                            decnums.Add(val);
                        }
                        buf[bytenum] = bindata[i];
                    }

                    result.Add(decnums);
                }

                StreamWriter file;

                if (check_value == 0)
                {
                    file = new StreamWriter(converdata + "/" + name + ".txt");
                }
                else
                {
                    file = new StreamWriter(name + ".txt");
                }

                string delim = "\t";
                long minlength = long.MaxValue;
                for (int i = 0; i < result.Count; ++i)
                {
                    if (result[i].Count < minlength)
                    {
                        minlength = result[i].Count;
                    }
                }
                for (int i = 0; i < header.Count; ++i)
                {
                    file.Write(header[i] + delim);
                }
                file.WriteLine();
                for (int i = 0; i < minlength; ++i)
                {
                    for (int j = 0; j < result.Count; ++j)
                    {
                        string ldelim = delim;
                        if (j == result.Count - 1)
                        {
                            ldelim = "";
                        }

                        result[j][i].ToString("G");
                        file.Write(Convert.ToString(result[j][i]) + ldelim);
                    }
                    file.WriteLine();
                }
                file.Flush();
                file.Close();

            }
            conn.Close();


        }

        static byte[] GetBytes(SQLiteDataReader reader)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            Form2 com_form = new Form2();
            com_form.Show();
        }

        private void metroComboBox17_MouseClick(object sender, MouseEventArgs e)
        {
            metroComboBox17.Items.Clear();
            conn.Open();
            SQLiteCommand com_command = new SQLiteCommand("select commands from Com_ports", conn);
            re = com_command.ExecuteReader();
            while (re.Read())
            {
                metroComboBox17.Items.Add(re.GetValue(0).ToString());
            }
            conn.Close();
        }

        private void metroComboBox16_MouseClick(object sender, MouseEventArgs e)
        {
            metroComboBox16.Items.Clear();
            conn.Open();
            SQLiteCommand com_command = new SQLiteCommand("select commands from Com_ports", conn);
            re = com_command.ExecuteReader();
            while (re.Read())
            {
                metroComboBox16.Items.Add(re.GetValue(0).ToString());

            }
            conn.Close();
        }

        private void metroComboBox18_MouseClick(object sender, MouseEventArgs e)
        {
            metroComboBox18.Items.Clear();
            conn.Open();
            SQLiteCommand com_command = new SQLiteCommand("select commands from Com_ports", conn);
            re = com_command.ExecuteReader();
            while (re.Read())
            {
                metroComboBox18.Items.Add(re.GetValue(0).ToString());

            }
            conn.Close();
        }

        private void metroComboBox10_MouseClick(object sender, MouseEventArgs e)
        {
            metroComboBox10.Items.Clear();
            conn.Open();
            SQLiteCommand com_command = new SQLiteCommand("select commands from Com_ports", conn);
            re = com_command.ExecuteReader();
            while (re.Read())
            {
                metroComboBox10.Items.Add(re.GetValue(0).ToString());

            }
            conn.Close();
        }

        private void metroGrid1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (metroGrid1.Rows.Count == 0)
                {
                    metroContextMenu1.Enabled = false;
                }        
            }
        }

        //Коннект
        private void metroButton5_Click(object sender, EventArgs e)
        {
            try
            {
                //com ports param
                serialPort1.PortName = metroComboBox11.Text;
                serialPort1.BaudRate = Convert.ToInt32(metroComboBox12.Text);
                serialPort1.DataBits = Convert.ToInt32(metroComboBox13.Text);
                serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), metroComboBox14.Text);
                serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), metroComboBox15.Text);
                serialPort1.Open();
                groupBox3.Enabled = false;
                metroButton5.Visible = false;
                metroButton6.Visible = true;
                groupBox4.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            groupBox3.Enabled = true;
            metroButton5.Visible = true;
            metroButton6.Visible = false;
            groupBox4.Enabled = false;
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            bool error = false;
            if (serialPort1.IsOpen)
            {
                if (Hex.Checked == false)
                {

                    if (Ruch.Checked == true)
                    {
                        dataOUT = richTextBox1.Text;
                        serialPort1.Write(dataOUT);
                        textBox1.ForeColor = Color.Green;
                        textBox1.AppendText("Команда:"+ richTextBox1.Text + "\n");

                    }                   
                }
                else            
                {
                    try
                    {
                        if (Ruch.Checked == true)
                        {
                            byte[] data = HexStringToByteArray(richTextBox1.Text);
                            serialPort1.Write(data, 0, data.Length);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + richTextBox1.Text.ToUpper()+"\n"); 
                            
                        }
                    }
                    catch (FormatException) { error = true; }
                    catch (ArgumentException) { error = true; }

                    if (error) MessageBox.Show(this, "Не правильно задана 16-ричная строка: " + textBox1.Text + "\n", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        private void metroButton10_Click(object sender, EventArgs e)
        {
            bool error = false;
            if (serialPort1.IsOpen)
            {
                if (Hex.Checked == true)
                {
                    try
                    {
                        if (Ruch.Checked == false)
                        {
                            byte[] data = HexStringToByteArray(metroComboBox17.Text);
                            serialPort1.Write(data, 0, data.Length);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox17.Text.ToUpper() + "\n");
                        }                    
                    }
                    catch (FormatException) { error = true; }
                    catch (ArgumentException) { error = true; }

                    if (error) MessageBox.Show(this, "Не правильно задана 16-ричная строка: " + textBox1.Text + "\n", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                else
                {
                        dataOUT = metroComboBox17.Text;
                        serialPort1.Write(dataOUT);
                        textBox1.ForeColor = Color.Green;
                        textBox1.AppendText("Команда:" + metroComboBox17.Text + "\n");
                    
                }
            }
        }

        private void metroButton9_Click(object sender, EventArgs e)
        {
            bool error = false;
            if (serialPort1.IsOpen)
            {
                if (Hex.Checked == true)
                {
                    try
                    {
                        if (Ruch.Checked == false)
                        {
                            byte[] data = HexStringToByteArray(metroComboBox16.Text);
                            serialPort1.Write(data, 0, data.Length);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox16.Text.ToUpper() + "\n");
                        }
                        else
                        {
                            dataOUT = metroComboBox16.Text;
                            serialPort1.Write(dataOUT);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox16.Text + "\n");
                        }
                    }
                    catch (FormatException) { error = true; }
                    catch (ArgumentException) { error = true; }

                    if (error) MessageBox.Show(this, "Не правильно задана 16-ричная строка: " + textBox1.Text + "\n", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                else
                {
                    dataOUT = metroComboBox17.Text;
                    serialPort1.Write(dataOUT);
                    textBox1.ForeColor = Color.Green;
                    textBox1.AppendText("Команда:" + metroComboBox17.Text + "\n");

                }
            }
        }

        private void metroButton11_Click(object sender, EventArgs e)
        {
            bool error = false;
            if (serialPort1.IsOpen)
            {
                if (Hex.Checked == true)
                {
                    try
                    {
                        if (Ruch.Checked == false)
                        {
                            byte[] data = HexStringToByteArray(metroComboBox18.Text);
                            serialPort1.Write(data, 0, data.Length);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox18.Text.ToUpper() + "\n");
                        }
                        else
                        {
                            dataOUT = metroComboBox18.Text;
                            serialPort1.Write(dataOUT);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox18.Text + "\n");
                        }
                    }
                    catch (FormatException) { error = true; }
                    catch (ArgumentException) { error = true; }

                    if (error) MessageBox.Show(this, "Не правильно задана 16-ричная строка: " + textBox1.Text + "\n", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                else
                {
                    dataOUT = metroComboBox17.Text;
                    serialPort1.Write(dataOUT);
                    textBox1.ForeColor = Color.Green;
                    textBox1.AppendText("Команда:" + metroComboBox17.Text + "\n");

                }
            }
        }

        private void metroButton12_Click(object sender, EventArgs e)
        {
            bool error = false;
            if (serialPort1.IsOpen)
            {
                if (Hex.Checked == true)
                {
                    try
                    {
                        if (Ruch.Checked == false)
                        {
                            byte[] data = HexStringToByteArray(metroComboBox10.Text);
                            serialPort1.Write(data, 0, data.Length);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox10.Text.ToUpper() + "\n");
                        }
                        else
                        {
                            dataOUT = metroComboBox10.Text;
                            serialPort1.Write(dataOUT);
                            textBox1.ForeColor = Color.Green;
                            textBox1.AppendText("Команда:" + metroComboBox10.Text + "\n");
                        }
                    }
                    catch (FormatException) { error = true; }
                    catch (ArgumentException) { error = true; }

                    if (error) MessageBox.Show(this, "Не правильно задана 16-ричная строка: " + textBox1.Text + "\n", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                else
                {
                    dataOUT = metroComboBox17.Text;
                    serialPort1.Write(dataOUT);
                    textBox1.ForeColor = Color.Green;
                    textBox1.AppendText("Команда:" + metroComboBox17.Text + "\n");
                }
            }
        }


        private byte[] HexStringToByteArray(string s)
        {

            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;

        }

        private void metroButton7_Click(object sender, EventArgs e)
        {

            if (textBox1.Text != "")
            {
                textBox1.Text = "";
            }
            if (textBox2.Text != "")
            {
                textBox2.Text = "";
            }
        }

        private delegate void LineReceivedEvent(string POT);

        private void LineReceived(string s)
        {
            textBox2.ForeColor = Color.Red;
            textBox2.AppendText("Ответ:"+s+"\n");

        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(500);
            int intBytes = serialPort1.BytesToRead;
            byte[] bytes = new byte[intBytes];
            
            serialPort1.Read(bytes, 0, intBytes);
            string s = BitConverter.ToString(bytes);
            this.BeginInvoke(new LineReceivedEvent(LineReceived), s);
        }

        private void metroCheckBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (Ruch.Checked == true)
            {
                metroComboBox10.Enabled = false;
                metroComboBox16.Enabled = false;
                metroComboBox17.Enabled = false;
                metroComboBox18.Enabled = false;
                metroButton12.Enabled = false;
                metroButton10.Enabled = false;
                metroButton9.Enabled = false;
                metroButton11.Enabled = false;
                metroButton4.Enabled = true;
                richTextBox1.Enabled = true;
            }
            else
            {
                metroComboBox10.Enabled = true;
                metroComboBox16.Enabled = true;
                metroComboBox17.Enabled = true;
                metroComboBox18.Enabled = true;
                metroButton12.Enabled = true;
                metroButton10.Enabled = true;
                metroButton9.Enabled = true;
                metroButton11.Enabled = true;
                metroButton4.Enabled = false;
                richTextBox1.Enabled = false;
            }
        }

        private void metroTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            //запрет ввода спец символов ,иначе смерть
            if (e.KeyChar == ':' || e.KeyChar == '/' || e.KeyChar == '*' || e.KeyChar == '?' || e.KeyChar == '"' || e.KeyChar == '<' || e.KeyChar == '>' || e.KeyChar == '|')
            {
                e.Handled = true;
            }
        }

        void check_device()
        {
            metroLabel24.Text = "Disable";
            metroLabel25.Text = "No";

            ManagementObjectSearcher deviceList = new ManagementObjectSearcher("Select Name, Status from Win32_PnPEntity where Name = 'E20-10'");

            if (deviceList != null)
                foreach (ManagementObject device in deviceList.Get())
                {
                    string name = device.GetPropertyValue("Name").ToString();
                    string status = device.GetPropertyValue("Status").ToString();

                    metroLabel24.Text = name;
                    metroLabel25.Text = status;
                }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Вы хотите закрыть программу?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog == DialogResult.No)
            {
                e.Cancel=true;
            }
        }

        private void metroButton8_Click(object sender, EventArgs e)
        {
            
            Form3 faq = new Form3();
            faq.Show();
        }
    }
}

                   
   

