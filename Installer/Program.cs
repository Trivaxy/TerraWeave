using System;
using System.Windows.Forms;

namespace Installer
{
    public static class Program
    {
        private static readonly Version version = new Version(1, 0, 0, 0);

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form form = new InstallerForm
            {
                Text = $"TerraWeave Installer v{version}"
            };

            form.SetBounds(200, 200, 500, 180);

            form.FormBorderStyle = FormBorderStyle.FixedSingle;

            form.MaximizeBox = false;
            form.MinimizeBox = false;

            Application.Run(form);
        }
    }
}
