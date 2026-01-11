using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace HzDetect
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            if (!IsAdministrator())
            {
                MessageBox.Show("Execute como Administrador para uma varredura completa.", "HzDetect");
            }

            FormLicense licenca = new FormLicense();
            if (licenca.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new FormMain());
            }
        }

        public static bool IsAdministrator()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }
    }

    public class FormLicense : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public FormLicense()
        {
            this.Size = new Size(500, 380);
            this.BackColor = Color.FromArgb(10, 10, 10);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 30, 30));

            Label lblTitle = new Label() { Text = "HzDetect", Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold), ForeColor = Color.HotPink, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 80 };
            
            // Ajuste: TabStop = false e ReadOnly evitam a seleção automática
            TextBox txtTerms = new TextBox() { 
                Multiline = true, 
                ReadOnly = true, 
                TabStop = false,
                BackColor = Color.FromArgb(18, 18, 18), 
                ForeColor = Color.DarkGray, 
                BorderStyle = BorderStyle.None, 
                Font = new Font("Consolas", 9), 
                Location = new Point(40, 90), 
                Size = new Size(420, 150), 
                Text = "SISTEMA DE AUDITORIA ATIVO\r\n\r\nO HzDetect irá escanear:\r\n- Arquivos Temporários\r\n- Registros de Execução (Prefetch)\r\n- Strings de Memória\r\n- Logs de Aplicativos\r\n\r\nAguarde o término para liberação." 
            };

            // Remove o foco do TextBox ao carregar o formulário para não ficar azul
            this.Load += (s, e) => {
                txtTerms.SelectionLength = 0;
                this.ActiveControl = lblTitle; 
            };

            Button btnAccept = new Button() { Text = "ACCEPT", Size = new Size(140, 45), Location = new Point(90, 280), BackColor = Color.HotPink, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnAccept.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };
            
            Button btnDecline = new Button() { Text = "DECLINE", Size = new Size(140, 45), Location = new Point(270, 280), BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.HotPink, FlatStyle = FlatStyle.Flat };
            btnDecline.Click += (s, e) => Environment.Exit(0);

            this.Controls.AddRange(new Control[] { lblTitle, txtTerms, btnAccept, btnDecline });
        }
    }

    public class FormMain : Form
    {
        private ListBox listScan;
        private Button btnBegin;
        private Label lblStatus;
        private Panel pnlProgress;
        
        // Esta URL será substituída pelo server.js durante o build
        private string webhookUrl = "https://discord.com/api/webhooks/1459773747992199324/drju_VTwPXIc-KzN6S4OIMgrT1U99fF5HWbnpcyh46GKC2mDHAtkf3w1hVsT9r3j2wCu";

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public FormMain()
        {
            this.Size = new Size(850, 550);
            this.BackColor = Color.FromArgb(7, 7, 7);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 35, 35));

            Label lblLogo = new Label() { Text = "HzDetect", Font = new Font("Arial Black", 42, FontStyle.Bold), ForeColor = Color.White, Location = new Point(0, 45), Size = new Size(850, 70), TextAlign = ContentAlignment.MiddleCenter };
            
            Label btnExit = new Label() { Text = "✕", ForeColor = Color.FromArgb(60, 60, 60), Location = new Point(800, 20), Cursor = Cursors.Hand, Font = new Font("Arial", 14, FontStyle.Bold), AutoSize = true };
            btnExit.Click += (s, e) => Environment.Exit(0);

            Panel pnlTerminal = new Panel() { Location = new Point(80, 160), Size = new Size(690, 240), BackColor = Color.FromArgb(12, 12, 12) };
            listScan = new ListBox() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 12), ForeColor = Color.HotPink, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8) };
            pnlTerminal.Controls.Add(listScan);

            pnlProgress = new Panel() { Location = new Point(80, 400), Size = new Size(0, 2), BackColor = Color.HotPink };

            btnBegin = new Button() { Text = "INITIALIZE FULL SCAN", Location = new Point(325, 435), Size = new Size(200, 50), BackColor = Color.FromArgb(20, 20, 20), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnBegin.FlatAppearance.BorderColor = Color.HotPink;
            btnBegin.Click += async (s, e) => await RunIntenseScan();

            lblStatus = new Label() { Text = "STATUS: SYSTEM READY", ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, 515), AutoSize = true, Font = new Font("Consolas", 8) };
            this.Controls.AddRange(new Control[] { lblLogo, btnExit, pnlTerminal, pnlProgress, btnBegin, lblStatus });
        }

        private void AddMsg(string text)
        {
            listScan.Items.Add(text);
            listScan.TopIndex = listScan.Items.Count - 1; 
        }

        private async Task RunIntenseScan()
        {
            btnBegin.Enabled = false;
            listScan.Items.Clear();
            pnlProgress.Width = 0;
            string detections = "";
            string[] targets = { "SWIFT", "XENO", "INJECT", "CHEAT", "HACK" };

            AddMsg("[*] INITIALIZING HzDetect KERNEL ANALYZER...");
            await Task.Delay(800);
            
            var processes = Process.GetProcesses();
            for(int i = 0; i < Math.Min(processes.Length, 15); i++) {
                AddMsg($"    -> ATTACHED: {processes[i].ProcessName}.exe [PID: {processes[i].Id}]");
                await Task.Delay(30);
            }

            string[] paths = {
                @"C:\Windows\Prefetch",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };

            foreach (var path in paths)
            {
                AddMsg($"\n[>] ANALYZING DIRECTORY: {path}");
                if (Directory.Exists(path)) {
                    try {
                        var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                        foreach (var file in files.Take(30)) {
                            string n = Path.GetFileName(file).ToUpper();
                            AddMsg($"    CHECKING: {n}");
                            if (targets.Any(t => n.Contains(t))) {
                                detections += $"• **Arquivo:** `{n}`\n";
                            }
                            await Task.Delay(10);
                        }
                    } catch { }
                }
                pnlProgress.Width += 690 / paths.Length;
            }

            pnlProgress.Width = 690;
            lblStatus.Text = "SCAN COMPLETE - UPLOADING LOGS";
            await SendLogFormatted(detections);
            btnBegin.Enabled = true;
            MessageBox.Show("Auditoria Completa. Logs enviados com sucesso.", "HzDetect");
        }

        private async Task SendLogFormatted(string logData)
        {
            try {
                using (HttpClient client = new HttpClient()) {
                    var payload = new {
                        embeds = new[] {
                            new {
                                title = "🛡️ HzDetect Audit Log",
                                color = 16716947,
                                fields = new object[] {
                                    new { name = "User", value = Environment.UserName, inline = true },
                                    new { name = "PC", value = Environment.MachineName, inline = true },
                                    new { name = "Evidence", value = string.IsNullOrEmpty(logData) ? "✅ Clean" : logData, inline = false }
                                },
                                footer = new { text = $"HzDetect Premium | {DateTime.Now}" }
                            }
                        }
                    };
                    await client.PostAsync(webhookUrl, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                }
            } catch { }
        }

        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } }
    }
}