using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace TextVerteiler
{
    static class Program
    {

        public static FormMain fmMain;
        public static FormEinstellungen fmEinstellungen;

        public static Mutex mutex;

        public static bool MehrAlsEinPaketZulassen = true;

        //makiert, dass das gesendete paket teil einer größeren naricht ist
        public static byte[] MultipleTextPackages = System.Text.Encoding.Unicode.GetBytes(((char)2).ToString());

        public static byte[] LastTextPackages = System.Text.Encoding.Unicode.GetBytes(((char)3).ToString());


        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool isNew;
            mutex = new Mutex(false, Application.ProductName + "_abcxyz16091994", out isNew);

            if (isNew)
            {
                //ok just this instance running
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                fmEinstellungen = new FormEinstellungen();
                fmMain = new FormMain();

                fmMain.IniFormMain();

                Application.Run();
            }
            else
            {
                MessageBox.Show("Eine Instanz des Programmes läuft bereits, bitte vorher beenden.", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                //wait, we just want 1 instance running... run to end
            }
        }


        public static void Exit()
        {
            Application.ExitThread();
        }
    }
}
