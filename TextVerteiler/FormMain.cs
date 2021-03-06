﻿/*
 * 
 * This Program is free for ANY use.
 * 
 * 
 * Program written by Janis Dähne
 * 
 * 
 * To change network stuff
 * change port: Port
 * 
 * UDP Packet broadcast is every 3 seconds
 * 
 * 
 * 
 * Ports in use:
 * udpServer: 8011
 * tcpListener: 8008
 * 
*/







using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TextVerteiler.Networking;
using System.Net;
using System.Net.Sockets;

namespace TextVerteiler
{
    public partial class FormMain : Form
    {

        public const int UDPBROADCAST_Delay = 3000;

        public static int PaketSize = 42000; //muss auch bei clienten geändert werden !! bei socket.receivebuffersize

        public ServerContext Server { get; set; }

        private int Port = 8008;


        public bool IfLostFocus { get; set; }

        public bool IfButtonClicked { get; set; }

        public bool IfTimespanElapsed { get; set; }

        public int Zeitspanne { get; set; }

        public int TimeCount { get; set; }

        private int Pointer = 0;

        List<string> TextHistory = new List<string>();


        public static double LeaveOpacity = 0.3D;

        public int CheckStatusCounter { get; set; } //0 to 10


        public int LazyCounter { get; set; }//nach XX sec automatisch verstecken

        public int MaxLazy { get; set; }

        public static int HistoryStackSize { get; set; }

        public long NarichtenCounter { get; set; }

        public static SendModeMultiplePackages SendModeForMultiplePackages = SendModeMultiplePackages.AllClientsSameTime;

        ToolTip tip1 = new ToolTip();
        ToolTip tip2 = new ToolTip();


        public enum SendModeMultiplePackages
        {
            /// <summary>
            /// Alle Teile der mehrteiligen Naricht werden erst zu dem 1. Client, dann zu dem 2. Clien... 
            /// (Narichten kommen wahrscheinlich zu unterschiedlichen Zeiten an, bei großen Dateien)
            /// </summary>
            ClientAfterClient,

            /// <summary>
            /// 1. Teil der Naricht wird an alle Clients gesendet, 2. Teil der Naricht wird an alle Clients gesendet.
            /// (Bei allen Clienten sollten die Narichten gleichzeitig ankommen.)
            /// </summary>
            AllClientsSameTime
            
        }

        public FormMain()
        {
            InitializeComponent();

            //Program.fmEinstellungen.Show();
            //Program.fmEinstellungen.Visible = false;

            Program.fmEinstellungen.InitFormEinstellungen();

            this.IfButtonClicked = true;
            this.Zeitspanne = 1000;


        }




        private void FormMain_Load(object sender, EventArgs e)
        {
            ResetPosition();
            btnSend.Enabled = true;

        }

        public void IniFormMain()
        {

            tip1.UseFading = false;
            tip1.UseAnimation = false;
            tip1.ShowAlways = false;
            tip1.InitialDelay = 500;

            tip2.UseFading = false;
            tip2.UseAnimation = false;
            tip2.ShowAlways = false;
            tip2.InitialDelay = 500;

            tip1.SetToolTip(btnResetFormat, "Löscht das aktuelle Format,\n sodass wieder normaler Text geschrieben werden kann.");
            tip2.SetToolTip(btnKopieren, "Makiert den gesamten Text und kopiert ihn in die Zwischenablage.");

            this.TopMost = true;
            ResetPosition();

            FormMain.HistoryStackSize = 20;

            Program.fmEinstellungen.cbSendModes.Items.Add("Alle Clienten gleichzeitig");
            Program.fmEinstellungen.cbSendModes.Items.Add("Client für Client");
            Program.fmEinstellungen.cbSendModes.SelectedIndex = 0;

            try
            {
                tbMessage.LostFocus += new EventHandler(tbMessage_LostFocus);

                //IPHostEntry r = Dns.GetHostEntry(IPAddress.Loopback); //klappt nur bei win 7/8
                IPHostEntry rr = Dns.GetHostEntry(Dns.GetHostName()); //klappt auch bei Win xp

                var result = rr.AddressList.Where((ipadd) => ipadd.AddressFamily == AddressFamily.InterNetwork && ipadd.ToString() != "127.0.0.1").ToList();

                if (result.Count >= 1 && rr.HostName != "")
                {

                    //tbMessage.Text = "Eigene IP: " + result[0].ToString();// +" : " + Port.ToString();
                    tbMessage.Text = "Eigener Dns Name: " + rr.HostName;// +" : " + Port.ToString();
                    Server = new ServerContext(Port, 50, ref TextHistory, result[0].ToString());
                }
                else
                {
                    MessageBox.Show("Server konnte nicht gestartet werden: Dns Name nicht gefunden", "Dns Name nicht gefunden");
                    Program.Exit();
                }


                Server.DoListen();

            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message);
            }
            SetLeaveOpacity();

        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIconFmMain.ShowBalloonTip(10);
            this.Visible = false;
            e.Cancel = true;
        }

        public void ResetPosition()
        {
            Point p = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width,
                   Screen.PrimaryScreen.WorkingArea.Bottom - this.Height);
            //((Screen.PrimaryScreen.Bounds.Bottom - Screen.PrimaryScreen.WorkingArea.Bottom) = taskbar height);
            this.Location = p;
        }


        [System.Diagnostics.DebuggerHidden()]
        void tbMessage_LostFocus(object sender, EventArgs e)
        {
            if (this.IfLostFocus)
            {
                if (tbMessage.Text != "")
                {
                    Server.SendText(tbMessage.Text);
                    NarichtenCounter++;
                    lbStatus.Text = "Gesendet: " + DateTime.Now.ToShortTimeString() + " - Naricht " + NarichtenCounter;
                    if (TextHistory.Count == 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                    }
                    else if (TextHistory.Count > 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                        btnNext.Enabled = false;
                    }
                }

                //Server.SendText(tbMessage.Text);
                //this.Text = "Text Input - " + Server.GetRealClientsCount() + " received";
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

            if (this.IfButtonClicked)
            {
                if (tbMessage.Text != "")
                {
                    Server.SendText(tbMessage.Rtf); //text is unformatiert , rtf is formatiert
                    NarichtenCounter++;
                    lbStatus.Text = "Gesendet: " + DateTime.Now.ToShortTimeString() + " - Naricht " + NarichtenCounter;
                    if (TextHistory.Count == 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                    }
                    else if (TextHistory.Count > 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                        btnNext.Enabled = false;
                    }
                }

            }

        }

        private void tbMessage_TextChanged(object sender, EventArgs e)
        {

            if (tbMessage.Rtf == "" || tbMessage.Text == "")
            {
                btnSend.Enabled = false;
            }
            else
            {
                btnSend.Enabled = true;
            }
            if (this.IfTimespanElapsed)
            {
                this.TimeCount = 0;
                timer1.Start();
            }

        }

        public void SetFormText(string Text)
        {
            if (this.InvokeRequired)
            {
                Action<string> set = new Action<string>(SetFormText);
                this.Invoke(set, new object[] { Text });
            }
            else
            {
                this.Text = Text;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (TimeCount >= this.Zeitspanne / timer1.Interval)
            {

                if (tbMessage.Text != "")
                {

                    Server.SendText(tbMessage.Text);
                    NarichtenCounter++;
                    lbStatus.Text = "Gesendet: " + DateTime.Now.ToShortTimeString() + " - Naricht " + NarichtenCounter;
                    if (TextHistory.Count == 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                    }
                    else if (TextHistory.Count > 2)
                    {
                        Pointer = TextHistory.Count - 1;
                        btnPrevious.Enabled = true;
                        btnNext.Enabled = false;
                    }
                }

                this.TimeCount = 0;
                timer1.Stop();
            }
            TimeCount++;
        }



        public void Shutdown()
        {
            Server.DisconnectAllClients();
            notifyIconFmMain.Dispose();
            Program.Exit();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            Pointer++;
            SetTextFromHistory();

            if (Pointer == TextHistory.Count - 1)
            {
                btnNext.Enabled = false;
                btnPrevious.Enabled = true;
            }
            else
            {
                btnPrevious.Enabled = true;
                btnNext.Enabled = true;
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            Pointer--;
            SetTextFromHistory();

            if (Pointer == 0)
            {
                btnPrevious.Enabled = false;
                btnNext.Enabled = true;
            }
            else
            {
                btnPrevious.Enabled = true;
                btnNext.Enabled = true;
            }
        }

        private void SetTextFromHistory()
        {
            if (Pointer >= 0 && Pointer < this.TextHistory.Count)
            {
                this.tbMessage.Clear();
                tbMessage.Rtf = this.TextHistory[Pointer];
                tbMessage.SelectAll();
            }

        }

        private void tbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                tbMessage.SelectAll();
            }
        }


        private void einstellungenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.fmEinstellungen.Visible = true;
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void notifyIconFmMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visible == true)
                {
                    this.Visible = false;
                }
                else
                {
                    this.Visible = true;
                }

            }
        }

        #region Opacity

        [System.Diagnostics.DebuggerHidden()]
        private void SetEnterOpacity()
        {
            this.Opacity = 1.0;
        }

        [System.Diagnostics.DebuggerHidden()]
        private void SetLeaveOpacity()
        {
            this.Opacity = LeaveOpacity;
        }

        #endregion

        [System.Diagnostics.DebuggerHidden()]
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {

            if (this.DesktopBounds.IntersectsWith(new Rectangle(Cursor.Position.X, Cursor.Position.Y, 1, 1)))
            {
                SetEnterOpacity();
                LazyCounter = 0;
            }
            else
            {
                SetLeaveOpacity();

                //nur wenn fenster auch wirklich sichtbar
                if (this.Visible == true)
                {
                    if (LazyCounter >= MaxLazy && MaxLazy > 0)
                    {
                        this.Visible = false;
                        LazyCounter = 0;
                    }

                    if (MaxLazy > 0)
                    {
                        LazyCounter++;
                    }
                    else
                    {
                        LazyCounter = 0;
                    }
                }


            }

            if (this.CheckStatusCounter >= 10) // 1 sekunde (intervall 100 * 10)
            {
                if (Server != null)
                {
                    //* 2 da unicode 2 bytes pro char

                    //this.Text = Server.GetRealClientsCount() + 
                    //    " connected" + " - Unicode: " + 
                    //    tbMessage.Rtf.Length * 2 + "/42000 Bytes" ;

                    this.Text = Server.GetRealClientsCount() +
                        " connected" + " - Unicode: " +
                        tbMessage.Rtf.Length * 2 + " Bytes = " + (int)Math.Ceiling((double)tbMessage.Rtf.Length * 2 / PaketSize) +
                        " Pakete (je " + PaketSize + ")";

                }

                this.CheckStatusCounter = 0;
            }

            this.CheckStatusCounter++;



        }

        private void btnResetFormat_Click(object sender, EventArgs e)
        {
            tbMessage.Font = this.Font;
            tbMessage.SelectionFont = this.Font;
            tbMessage.SelectionAlignment = HorizontalAlignment.Left;
            tbMessage.Focus();
        }

        private void btnKopieren_Click(object sender, EventArgs e)
        {
            tbMessage.SelectAll();
            tbMessage.Copy();
        }

    }
}
