using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Installer
{
    public partial class InstallerForm : Form
    {
        private readonly Button install;
        private readonly Button exploreFilesButton;
        private readonly TextBox fileChosen;
        private readonly OpenFileDialog openFile;
        private readonly Label label;

        public InstallerForm()
        {
            InitializeComponent();

            install = new Button() 
            {
                Location = new Point(20, 80),
                Size = new Size(450, 40),
                Text = "Install",
            };
            install.Click += OnInstallButtonClicked;

            exploreFilesButton = new Button()
            {
                Location = new Point(20, 40),
                Size = new Size(60, 24),
                Text = "Browse",
            };
            exploreFilesButton.Click += OnExploreFilesButtonClicked;

            fileChosen = new TextBox() 
            { 
                Location = new Point(90, 40),
                Size = new Size(380, 24),
                ReadOnly = true,
            };

            openFile = new OpenFileDialog();

            label = new Label()
            {
                Location = new Point(90, 20),
                Size = new Size(240, 20),
                Text = "Select a Terraria executable to patch to."
            };

            Controls.Add(install);
            Controls.Add(exploreFilesButton);
            Controls.Add(fileChosen);
            Controls.Add(label);
        }

        private void OnExploreFilesButtonClicked(object sender, EventArgs e)
        {
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                string[] nameSplit = openFile.FileName.Split(Path.DirectorySeparatorChar);

                if (nameSplit[^1] != "Terraria.exe") // This weird operator [^1] gets the last index in the array.
                {
                    MessageBox.Show("File was not a Terraria executable file.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                fileChosen.Text = openFile.FileName;
            }
        }

        private void OnInstallButtonClicked(object sender, EventArgs e)
        {
            if (fileChosen.Text != null && fileChosen.Text != "")
            {
                string[] split = fileChosen.Text.Split(Path.DirectorySeparatorChar);

                Array.Resize(ref split, split.Length - 1);

                string dir = "";

                for (int i = 0; i < split.Length; i++)
                {
                    if (i != 0)
                    {
                        dir += Path.DirectorySeparatorChar;
                    }

                    dir += split[i];
                }

                dir += $"{Path.DirectorySeparatorChar}Unpatched";

                Directory.CreateDirectory(dir);

                File.Copy(fileChosen.Text, $"{dir}{Path.DirectorySeparatorChar}Terraria.exe");

                string[] withoutExe = fileChosen.Text.Split('.');

                Array.Resize(ref withoutExe, withoutExe.Length - 1);

                string terrariaWithNoExe = "";

                foreach(string s in withoutExe)
                {
                    terrariaWithNoExe += s;
                }

                File.Copy($"{terrariaWithNoExe}Server.exe", $"{dir}{Path.DirectorySeparatorChar}TerrariaServer.exe");

                Console.WriteLine("e");
            }
        }
    }
}
