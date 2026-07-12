using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TateYokoMemocho
{
    public sealed class EditorForm : Form
    {
        private const int DefaultWidth = 560;
        private const int DefaultHeight = 540;
        private readonly WebView2 webView;
        private readonly string settingsDirectory;
        private readonly string windowStatePath;

        public EditorForm()
        {
            Text = "たてよこメモ帖";
            BackColor = Color.White;
            MinimumSize = new Size(400, 360);
            StartPosition = FormStartPosition.Manual;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TateYokoMemocho");
            windowStatePath = Path.Combine(settingsDirectory, "window-state.txt");

            RestoreWindowState();

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.White
            };
            Controls.Add(webView);

            Load += async (_, __) => await InitializeEditorAsync();
            FormClosing += (_, __) => SaveWindowState();
        }

        private async Task InitializeEditorAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(settingsDirectory, "WebView2");
                Directory.CreateDirectory(userDataFolder);

                CoreWebView2Environment environment =
                    await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(environment);

                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.local",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app"),
                    CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.Navigate("https://app.local/index.html");
                webView.Focus();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "エディターを起動できませんでした。\n\n" + exception.Message,
                    "たてよこメモ帖",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        private void RestoreWindowState()
        {
            Rectangle defaultBounds = new Rectangle(40, 40, DefaultWidth, DefaultHeight);
            Bounds = defaultBounds;

            try
            {
                if (!File.Exists(windowStatePath)) return;

                string[] values = File.ReadAllLines(windowStatePath);
                if (values.Length < 5) return;

                int x = int.Parse(values[0], CultureInfo.InvariantCulture);
                int y = int.Parse(values[1], CultureInfo.InvariantCulture);
                int width = Math.Max(MinimumSize.Width,
                    int.Parse(values[2], CultureInfo.InvariantCulture));
                int height = Math.Max(MinimumSize.Height,
                    int.Parse(values[3], CultureInfo.InvariantCulture));
                Rectangle savedBounds = new Rectangle(x, y, width, height);

                bool appearsOnScreen = Screen.AllScreens.Any(screen =>
                    screen.WorkingArea.IntersectsWith(savedBounds));
                if (appearsOnScreen) Bounds = savedBounds;

                if (string.Equals(values[4], "Maximized", StringComparison.OrdinalIgnoreCase))
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            catch
            {
                Bounds = defaultBounds;
            }
        }

        private void SaveWindowState()
        {
            try
            {
                Directory.CreateDirectory(settingsDirectory);
                Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                string state = WindowState == FormWindowState.Maximized ? "Maximized" : "Normal";

                File.WriteAllLines(windowStatePath, new[]
                {
                    bounds.X.ToString(CultureInfo.InvariantCulture),
                    bounds.Y.ToString(CultureInfo.InvariantCulture),
                    bounds.Width.ToString(CultureInfo.InvariantCulture),
                    bounds.Height.ToString(CultureInfo.InvariantCulture),
                    state
                });
            }
            catch
            {
                // サイズ記録に失敗しても、エディターの終了は妨げない。
            }
        }
    }
}
