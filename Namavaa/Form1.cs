using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Namavaa
{
    public partial class Form1 : Form
    {
        WebClient client = new WebClient();
        Namavaa namava = new Namavaa();
        public Form1()
        {
            InitializeComponent();
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void log(string text)
        {
            text += "\r\n";
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new MethodInvoker(() => { txtLog.AppendText(text); }));
            }
            else
            {
                txtLog.AppendText(text);
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            btnStart.Enabled = false;
            log("[+] Finding Available Qualities");
            Uri uri;
            if (Uri.TryCreate(txtLink.Text,UriKind.Absolute,out uri))
            {
                string path = GetParentUriString(uri);
                List<Quality> available = await namava.ParseStream(txtLink.Text);
                if(available == null)
                {
                    log("[-] Unable to parse Main Manifest File");
                    btnStart.Enabled = true;
                    return;
                }
                if (available.Count > 0)
                {

                    string text = "";
                    for (int i = 0; i < available.Count; i++)
                    {
                        text += string.Format("{0} - {1}\r\n", i, available[i].Resolution);
                    }
                    log(text);
                    log("[+] Downloading " + available[available.Count - 1].Resolution);
                    log("Using Base Path : " + path );
                    log("Parsing Video Manifest");
                    Video v = await namava.ParseVideo(path + available[available.Count - 1].Uri);
                    if( v != null)
                    {
                        log(String.Format("Video Size : {0}", BytesToString(v.Length)));
                        if(v.Key == "")
                        {
                            log("[-] Unable to parse Video Encryption KEY");
                            btnStart.Enabled = true;
                            return;
                        }
                        else
                        {
                            SaveFileDialog dialog = new SaveFileDialog() {CheckFileExists= false, Title = " Save File to ...",CheckPathExists = true,Filter = "*.ts|*.ts" }; 
                            if(dialog.ShowDialog() == DialogResult.OK)
                            {
                                await client.DownloadFileTaskAsync(new Uri(path + v.Url), dialog.FileName + ".tmp");
                                log("[+] Preparing Decryption System");
                                await namava.Decrypt(path + v.Key, dialog.FileName + ".tmp", dialog.FileName);
                                log("[+] Decryption Completed");
                                try
                                {
                                    System.Diagnostics.Process.Start(dialog.FileName);
                                }catch(Exception err)
                                {

                                }
                            }
                        }
                    }
                    else
                    {
                        log("[-] Unable to parse Video Manifest File");
                        btnStart.Enabled = true;
                        return;
                    }
                }

            }

            btnStart.Enabled = true;
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            log("[+] Downloading Completed");
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private string GetParentUriString(Uri uri)
        {
            return uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - (uri.Segments.Last().Length + uri.Query.Length));
        }
        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            textBox1.SelectAll();
        }
    }
    
}
