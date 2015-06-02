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
        private string filename, filenameMp3;

        public Form1()
        {
            InitializeComponent();
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
                AutoSave();
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
                if (e.Shift)
                    TriggerSaveAs();
                else
                    TriggerSave();
                e.SuppressKeyPress = true;
            }
            if (e.Control && e.KeyCode == Keys.O)
            {
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    filename = dialog.FileName;
                    OpenTextFile();
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

        private void TriggerSave()
        {
            if (filename != null) {
                AutoSave();
                return;
            }
            TriggerSaveAs();
        }

        private void AutoSave()
        {
            if (filename == null)
                return;
            if (String.IsNullOrWhiteSpace(richTextBox1.Text))
                return;
            SaveTextFile();
            txtSaved.Text = "last save @ " + DateTime.Now.ToString();
        }

        private void OpenTextFile()
        {
            if (!File.Exists(filename))
                return;
            var lines = File.ReadAllLines(filename);
            var lines2 = lines.AsEnumerable();
            if (lines.Length >= 2)
            {
                var line1 = lines2.FirstOrDefault();
                if (!String.IsNullOrEmpty(line1) && line1.StartsWith("MP3:"))
                {
                    var mp3Filename = line1.Replace("MP3:", "");
                    if (!String.IsNullOrWhiteSpace(mp3Filename))
                        loadMp3(mp3Filename);
                    lines2 = lines2.Skip(1);
                    var line2 = lines2.FirstOrDefault();
                    if (!String.IsNullOrEmpty(line2) && line2.StartsWith("LastTime:"))
                    {
                        var prevTs = TimeSpan.Zero;
                        if (Playa != null && TimeSpan.TryParse(line2.Replace("LastTime:", ""), out prevTs))
                            Playa.Position = prevTs;
                        lines2 = lines2.Skip(1);
                        // skip the extra newline after header (see SaveTextFile below)
                        if (String.IsNullOrEmpty(lines2.FirstOrDefault()))
                            lines2 = lines2.Skip(1);
                    }
                }
            }
            richTextBox1.Text = String.Join("\n", lines2);
        }
        private void SaveTextFile()
        {
            var playerPos = (Playa != null)
                ? Playa.Position.ToString()
                : TimeSpan.Zero.ToString();
            string text = String.Format("MP3:{0}\nLastTime:{1}\n\n{2}", filenameMp3, playerPos, richTextBox1.Text);
            File.WriteAllText(filename, text);
        }

        private void TriggerSaveAs()
        {
            if (String.IsNullOrWhiteSpace(richTextBox1.Text))
                return;
            var dialog = new SaveFileDialog();
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;
            filename = dialog.FileName;
            AutoSave();
        }


        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var file1 = files.FirstOrDefault();
            if (file1 == null)
                return;
            if (Path.GetExtension(file1).ToLower() != ".mp3")
                return;
            // we are good, an mp3 file is dragged in!
            loadMp3(file1);
        }

        private void loadMp3(string file1)
        {
            // unload old mp3
            if (Playa != null) {
                Playa.Stop();
                Playa.Dispose();
            }
            filenameMp3 = file1;
            Playa = new AudioPlaya(file1);
            Trace.WriteLine(file1 + " dragged in!!!!!!");
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

        

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isDisposing = true;
        }


    }
}
