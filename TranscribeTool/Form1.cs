using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Timer = System.Timers.Timer;

namespace TranscribeTool
{
    public partial class Form1 : Form
    {
        public AudioPlaya Playa { get; set; }
        private Timer ttt, tttAutoSave;
        private bool isDisposing = false;
        private string filenameMp3;
        private TranscriberLogic logic;

        public Form1()
        {
            InitializeComponent();
            logic = new TranscriberLogic();
            labelTime.Text = "Drag audio!";
            ttt = new Timer();
            ttt.Tick += ttt_Tick;
            ttt.Interval = 100;
            ttt.Start();
            tttAutoSave = new Timer();
            tttAutoSave.Tick += tttAutoSave_Tick;
            tttAutoSave.Interval = 10000;
            tttAutoSave.Start();
        }

        private string FilenameTxt {
            get {
                return logic.FnameMp3ToText(filenameMp3);
            }
        }

        #region ========================= UI events =======================================

        private void Form1_Load(object sender, EventArgs e) { }
        private void tttAutoSave_Tick(object sender, EventArgs e)
        {
            if (isDisposing)
            {
                tttAutoSave.Stop();
                tttAutoSave.Dispose();
                return;
            }
            Invoke(new MethodInvoker(delegate
            {
                SaveTextFile();
            }));
        }

        private void ttt_Tick(object sender, EventArgs e)
        {
            if (isDisposing) {
                ttt.Stop();
                ttt.Dispose();
                return;
            }

            Invoke(new MethodInvoker(delegate {
                if (Playa != null)
                    labelTime.Text = Playa.Position.ToString(@"hh\:mm\:ss");
            }));
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveTextFile();
                e.SuppressKeyPress = true;
            }
            if (e.Control && e.KeyCode == Keys.O)
            {
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    OpenAnyFile(dialog.FileName);
                }
                e.SuppressKeyPress = true;
            }
            if (Playa != null)
            {
                if ((e.Alt && e.KeyCode == Keys.Left) ||
                   (e.Control && e.KeyCode == Keys.Left))
                {
                    Playa.Position -= TimeSpan.FromSeconds(5);
                    e.SuppressKeyPress = true;
                }
                if ((e.Alt && e.KeyCode == Keys.Right) ||
                   (e.Control && e.KeyCode == Keys.Right))
                {
                    Playa.Position += TimeSpan.FromSeconds(5);
                    e.SuppressKeyPress = true;
                }
                if ((e.Alt && e.KeyCode == Keys.Space) ||
                    (e.Control && e.KeyCode == Keys.Space))
                {
                    if (!Playa.Playing)
                        Playa.Play();
                    else
                        Playa.Pause();
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //foreach (string file in files)
            //    Trace.WriteLine(file);

#if ChangeTextOnDragEnter
            // change text... I don't like this
            var file1 = files.FirstOrDefault();
            if (file1 != null) {
                richTextBox1.Text = String.Format(
@"Audio: {0}
Recorded on: {2}
",
                    Path.GetFileName(file1), "", File.GetCreationTime(file1).ToString("f"));
            }
#endif
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var file1 = files.FirstOrDefault();
            if (file1 == null)
                return;
            Trace.WriteLine(file1 + " dragged in!!!!!!");
            OpenAnyFile(file1);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isDisposing = true;
        }

        #endregion

        #region ========================= privates =======================================

        private void OpenAnyFile(string fname)
        {
            if (Path.GetExtension(fname).ToLower() == ".mp3")
                filenameMp3 = fname;
            if (Path.GetExtension(fname).ToLower() == ".md")
            {
                filenameMp3 = logic.FnameTextToMp3(fname);
                if (!File.Exists(filenameMp3))
                {
                    MessageBox.Show("No Mp3 file found: " + Path.GetFileName(filenameMp3), "No Mp3 file!");
                }
            }
            loadMp3();
            loadTextFile();
        }

        private void loadMp3()
        {
            // unload old mp3
            if (Playa != null)
            {
                Playa.Stop();
                Playa.Dispose();
            }
            Playa = new AudioPlaya(filenameMp3);
        }

        private void loadTextFile()
        {
            txtSaved.Text = "last save @ ---";
            if (!File.Exists(FilenameTxt))
            {
                richTextBox1.Text = "";
                return;
            }
            var text = File.ReadAllText(FilenameTxt);
            var text2 = logic.ParseApplyAndRemoveInvisibleMetadata(text, Playa);
            richTextBox1.Text = text2;
        }

        private void SaveTextFile()
        {
            if (FilenameTxt == null)
                return;
            if (String.IsNullOrWhiteSpace(richTextBox1.Text))
                return;
            var text = logic.AddInvisibleMetadata(richTextBox1.Text, Playa, filenameMp3);
            File.WriteAllText(FilenameTxt, text);
            txtSaved.Text = "last save @ " + DateTime.Now.ToString();
        }

        #endregion

    }
}
