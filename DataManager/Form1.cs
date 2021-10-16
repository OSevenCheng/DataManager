using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
namespace DataManager
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        float minValue = 1000000;
        float maxValue = -1000000;
        private delegate void SetPos(int ipos, string vinfo);//代理
        private void SetTextMesssage(int ipos, string vinfo)
        {
            if (this.InvokeRequired)
            {
                SetPos setpos = new SetPos(SetTextMesssage);
                this.Invoke(setpos, new object[] { ipos, vinfo });
            }
            else
            {
                this.label1.Text = ipos.ToString() + "/100";
                this.progressBar1.Value = Convert.ToInt32(ipos);
               // this.textBox1.AppendText(vinfo);
            }
        }
        
        //文件名
        private string[] SelectFileNames;
        //图像对象
        private Bitmap curBitmap;
        private void Init(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            label1.Text = "Loading " + filename;
            
            //@"E:\2021-10-14 工作存档\data_grib\2011.csv"
            FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None);
            StreamReader streamReader = new StreamReader(fs, Encoding.GetEncoding(936));

            string nextLine = streamReader.ReadLine();
            string[] its = nextLine.Split(',');
            foreach (var si in its)
            {
                comboBox1.Items.Add(si);
            }
            streamReader.Close();
        }
        int iv = 0;
        int latCount;
        int lonCount;
        private void ReadValue(
                object ofilepath
            )
        {
            string filepath = (string)ofilepath;
            FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None);
            StreamReader streamReader = new StreamReader(fs, Encoding.GetEncoding(936));
            //long totalLength = fs.Length;
            string nextLine = streamReader.ReadLine();
            Dictionary<string, TimeItem> Data = new Dictionary<string, TimeItem>();
            //long ic = 0;
            float latMin = float.Parse(textBoxLatMin.Text);
            float latMax = float.Parse(textBoxLatMax.Text);
            float lonMin = float.Parse(textBoxLonMin.Text);
            float lonMax = float.Parse(textBoxLonMax.Text);
            float latDelta = float.Parse(textBoxLatDelta.Text);
            float lonDelta = float.Parse(textBoxLonDelta.Text);
            latCount = (int)((latMax - latMin) / latDelta + 0.5)+1;
            lonCount = (int)((lonMax - lonMin) / lonDelta + 0.5)+1;
            while ((nextLine = streamReader.ReadLine()) != null)
            {
                //ic+=nextLine.Length;
                //SetTextMesssage((int)(100 * ((float)ic / (float)totalLength)), ic.ToString() + "\r\n");
                string[] items = nextLine.Split(',');
                string time = items[0];
                if (time == "UTC")
                    break;
                float lat = float.Parse(items[2]);
                float lon = float.Parse(items[3]);
                string v = items[iv];
                int ilat = (int)((lat - latMin) / latDelta);
                int ilon = (int)((lon - lonMin) / lonDelta);
                if (Data.ContainsKey(time))
                {
                    if (v != "")
                    {
                        float x = float.Parse(v);
                        //minValue = Math.Min(x, minValue);
                        //maxValue = Math.Max(x, maxValue);
                    }
                    Data[time].SetValue(ilat, ilon, v);
                }
                else
                {
                    TimeItem tmp = new TimeItem(latCount,lonCount);
                    tmp.SetValue(ilat, ilon, v);
                    Data.Add(time, tmp);
                }
            }
            streamReader.Close();
            //labelMax.Text = maxValue.ToString();
            //labelMin.Text = minValue.ToString();

            if(checkBoxBMP.Checked)
            {
                SaveAsBMP(Data, filepath);
            }
            if(checkBoxTXT.Checked)
            {
                SaveAsTXT(Data, filepath);
            }
        }
        private void SaveAsBMP(
                Dictionary<string, TimeItem> _Data,
                string _filepath
            )
        {
            minValue = float.Parse(textBoxMin.Text);
            maxValue = float.Parse(textBoxMax.Text);
            string filename = Path.GetFileName(_filepath);
            if (curBitmap == null)
            {
                curBitmap = new Bitmap(lonCount,latCount);
            }
            progressBar1.Value = 0;
            int count = 0;
            label1.Text = "Writing " + filename;
            foreach (var ti in _Data)
            {
                TimeItem a = ti.Value;
                for (int i = 0; i < latCount; i++)
                {
                    for (int j = 0; j < lonCount; j++)
                    {
                        string st = a.GetValue(i, j);
                        float x = 0f;
                        if (st != "")
                            x = float.Parse(st);

                        int c = (int)((x - minValue) / (maxValue - minValue) * 255);
                        Color color = Color.FromArgb(c, c, c);
                        curBitmap.SetPixel(j, i, color);
                    }
                }
                progressBar1.Value = (count++) / _Data.Count;
                string path = "D:\\data\\" + Path.GetFileNameWithoutExtension(_filepath);
                DirectoryInfo di = new DirectoryInfo(path);
                di.Create();
                string savePath = path + "\\" + ti.Key.Replace(":", "_") + ".bmp";

                curBitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            label1.Text = Path.GetFileName(_filepath) + "  Done";
        }
        private void SaveAsTXT(
            Dictionary<string, TimeItem> _Data,
             string _filepath
            )
        {
            //for (int i = 0; i < 33; i++)
            //{
            //    dataGridView1.Columns[i].Width = 100;
            //}
            //TimeItem a = _Data["2011-01-01T00:00:00Z"];
            //for (int i = 0; i < 33; i++)
            //{
            //    int index = dataGridView1.Rows.Add();
            //    for (int j = 0; j < 31; j++)
            //    {
            //        dataGridView1.Rows[index].Cells[j].Value = a.GetValue(i, j);
            //    }
            //}
            string name = Path.GetFileNameWithoutExtension(_filepath);
            string path = "D:\\data\\" + name;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Create();
            string savePath = path + "\\" + name + ".txt";
            StreamWriter sW = new StreamWriter(savePath);
            foreach (var d in _Data)
            {
                sW.WriteLine(d.Value.GetValues());
            }
            sW.Close();
        }
        private void buttonOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog opnDlg = new OpenFileDialog();
            opnDlg.Filter = "所有图像文件 | *.csv; ";
            opnDlg.Title = "打开csv文件";
            opnDlg.ShowHelp = true;
            opnDlg.Multiselect = false;
            if (opnDlg.ShowDialog() == DialogResult.OK)
            {
                SelectFileNames = opnDlg.FileNames;
            }
            foreach (var s in SelectFileNames)
            {
                Init(s);
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            OpenFileDialog opnDlg = new OpenFileDialog();
            opnDlg.Filter = "所有图像文件 | *.csv; ";
            opnDlg.Title = "打开csv文件";
            opnDlg.ShowHelp = true;
            opnDlg.Multiselect = true;
            if (opnDlg.ShowDialog() == DialogResult.OK)
            {
                SelectFileNames = opnDlg.FileNames;
            }
            label1.Text = "Converting... ";
            foreach (var s in SelectFileNames)
            {
                ReadValue(s);
                //Thread fThread = new Thread(new ParameterizedThreadStart(ReadValue));
                //fThread.Start(s);
            }
            label1.Text = "Done ! ";
        }

        private void checkBoxBMP_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv = comboBox1.SelectedIndex;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
    public class TimeItem
    {
        public TimeItem(int lat, int lon)
        {
            grid = new string[lat, lon];
        }
        string[,] grid;
        public void SetValue(int x, int y, string value)
        {
            grid[x, y] = value;
        }
        public string GetValues()
        {
            string r = "[";
            for (int i = 0; i < 33; i++)
            {
                r += "[";
                for (int j = 0; j < 31; j++)
                {
                    r += grid[i, j];
                    r += ",";
                }
                r = r.Substring(0, r.Length - 1);
                r += "]";
            }
            r += "]";
            return r;
        }
        public string GetValue(int x, int y)
        {
            return grid[x, y];
        }
    }
}
