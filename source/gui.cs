using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Threading;
using System.Reflection;


namespace Crash_Team_Mod
{

    public partial class CTM : Form
    {
        private Process psx_cmd;
        private StreamWriter psx_input;

        //important strings
        string PATHS_FILE = @".\paths.txt";
        string COMPILE_LIST;
        string MOD_NAME;
        string MOD_DIR;
        string ISO_PATH;
        string NAME_ROM;
        string PREV_PSX_DIR = System.IO.Directory.GetCurrentDirectory();
        string PSX_DIR;
        string debug_path;
        string action;
        string game_ver;
        string task_text;


        public CTM()
        {
            InitializeComponent();
            buttons();
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.gui_closing);
            romfile.Filter = "CTR ROM (*.bin) | *.bin; | All files (*.*) | *.*";
            romfile.FilterIndex = 1;
            PSX_DIR = PREV_PSX_DIR.Replace("\\", "/");
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1500;
            timer.Tick += (sender, e) => check_proccess(); //always check if the buildlist are open
            timer.Start();

        }


        //bool to make sure game version is already selected
        bool version_bool = false;

        //to avoid cmd closing after a task finished
        bool active_buildlist = false;





        //check if the cmd is open
        public void check_proccess()
        {
            if (psx_cmd != null && !psx_cmd.HasExited)
            {
                return;
            }
            else
            {
                //reopen the cmd if it closes itself without reason
                if (active_buildlist)
                {
                    open_cmd();

                    if (!string.IsNullOrEmpty(game_ver))
                    {
                        action = game_ver;
                        version_bool = true;
                        commands(action);
                    }
                }
                else
                {
                    console_t.Text = string.Empty;
                }
            }
        }



        //close cmd when the app is closed
        void gui_closing(object sender, FormClosingEventArgs e)
        {
            close_cmd();
        }




        void restart_cmd()
        {
            psx_cmd.Kill();
            Process.Start("taskkill", $"/F /IM cmd.exe");
        }




        //closing the cmd
        void close_cmd()
        {
            if (psx_cmd != null && !psx_cmd.HasExited)
            {
                psx_cmd.Kill();
            }

            active_buildlist = false;

            Process.Start("taskkill", $"/F /IM cmd.exe");
        }





        //open buildlist (paths are given by paths.txt)
        void open_cmd()
        {
            string python = Path.GetFullPath(".\\data\\tools\\Python\\Python310\\python.exe");
            string builder = Path.GetFullPath(".\\data\\tools\\mod-builder\\main.py");



           
            
                // buidlist proccess
                psx_cmd = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/K \"{python} \"\"{builder}\"\"\"",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,         // No shell
                        CreateNoWindow = true           // No window



                    }
                };


                psx_cmd.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Invoke(new Action(() =>
                        {
                            console_t.AppendText(e.Data + Environment.NewLine);
                        }));
                    }
                };


                psx_cmd.Start();
                psx_cmd.BeginOutputReadLine();
                psx_input = psx_cmd.StandardInput;


            }
        




        //write commands in the cmd using app buttons
        void commands(string action)
        {
            if (psx_cmd != null && !psx_cmd.HasExited)
            {
                psx_input.WriteLine(action);
                psx_input.Flush();
            }
            else
            {
                if (!version_bool)
                {
                    // only show warning if this is not called by version buttons
                    MessageBox.Show("The console is not open, first drag a buildlist in the square at the right side.");
                }
                else
                {
                    version_bool = false;
                }
            }
        }






        //unused in beta
        void buttons()
        {

            var allowedButtons = (enable_mod.Checked) ? new[] { mod1, mod2, mod3, mod4, search_rom }
            : new[] { dis_b, tex_b, log_b, comp_b, cleanc_b, buildc_b, exc_b, xd_b, cleanr_b, psx_r,
            romv1, romv2, romv3, search_rom};



            //block modding buttons if the checkbox is not enabled
            foreach (Control control in groupBox1.Controls)
            {
                if (control is System.Windows.Forms.Button button)
                {
                    if (!allowedButtons.Contains(button))
                    {
                        button.Enabled = false;
                    }
                    else
                    {
                        button.Enabled = true;
                    }

                }
            }

        }






        //update paths from the .txt file
        void change_compile_paths()
        {

            //replace these lines in paths.txt
            var replacements = new Dictionary<string, string>
                 {
                     { "COMPILE_LIST", COMPILE_LIST },
                     { "MOD_DIR", MOD_DIR },
                     { "MOD_NAME", MOD_NAME },
                     { "PSX_DIR", PSX_DIR },
                     { "NAME_ROM", NAME_ROM}
                 };


            string[] lines = File.ReadAllLines(PATHS_FILE);


            var updatedLines = new System.Collections.Generic.List<string>();


            foreach (var line in lines)
            {
                string updatedLine = line;


                foreach (var path in replacements)
                {

                    updatedLine = Regex.Replace(updatedLine, $@"^{path.Key}\s*=\s*\"".*?\""", $"{path.Key} = \"{path.Value}\"");
                }


                updatedLines.Add(updatedLine);
            }


            File.WriteAllLines(PATHS_FILE, updatedLines.ToArray());

        }







        //update iso path
        void update_iso_folder()
        {
            string old_folder = "ISO_PATH";
            string old_name = "NAME_ROM";
            string new_folder = ISO_PATH;
            string new_name = NAME_ROM;

            string[] lines = File.ReadAllLines(PATHS_FILE);

            var updated = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {

                string update_iso = Regex.Replace(line, $@"^{old_folder}\s*=\s*\"".*?\""", $"{old_folder} = \"{new_folder}\"");
                update_iso = Regex.Replace(update_iso, $@"^{old_name}\s*=\s*\"".*?\""", $"{old_name} = \"{new_name}\"");

                updated.Add(update_iso);
            }

            File.WriteAllLines(PATHS_FILE, updated.ToArray());

            action = "refresh_iso";
            commands(action);


        }





        //unused in beta
        private void enable_mod_CheckedChanged(object sender, EventArgs e)
        {
            buttons();
        }






        //set rom path
        private void search_rom_Click(object sender, EventArgs e)
        {
            if (romfile.ShowDialog() == DialogResult.OK)
            {
                rom.Text = romfile.FileName;
                NAME_ROM = System.IO.Path.GetFileName(romfile.FileName);
                ISO_PATH = System.IO.Directory.GetParent(romfile.FileName).FullName;

                version_bool = true;

                update_iso_folder();

            }
        }






        //set buildlist path
        private void drop_buildlist(object sender, DragEventArgs e)
        {
            //first close previous buildlists just in case
            close_cmd();



            // get the path of buildList.txt
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            COMPILE_LIST = files[0];

            string PREV_MODDIR = System.IO.Directory.GetParent(COMPILE_LIST).FullName;
            MOD_NAME = System.IO.Path.GetFileName(PREV_MODDIR);
            PREV_MODDIR += "\\";
            MOD_DIR = PREV_MODDIR.Replace("\\", "/");



            //show current buildlist path
            MessageBox.Show($"buildList selected: {COMPILE_LIST}");

            //update txt paths
            change_compile_paths();

            //open buildlist
            open_cmd();

            active_buildlist = true;

            if (!string.IsNullOrEmpty(game_ver))
            {
                action = game_ver;
                version_bool = true;
                commands(action);
                console_t.Text = string.Empty;
            }
        }






        //check if the file is really a buildList
        private void buildlist_checker(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && System.IO.Path.GetFileName(files[0]) == "buildList.txt")
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }





        //set usa rom as default
        private void select_usa(object sender, EventArgs e)
        {
            action = "sel_usa true";
            game_ver = action;
            version_bool = true;
            task_text = "NTSC-U ROM WAS SELECTED";
            console_t.AppendText(task_text + Environment.NewLine);
            commands(action);
        }



        //set japanese rom as default
        private void select_japan(object sender, EventArgs e)
        {
            action = "use_jap true";
            game_ver = action;
            version_bool = true;
            task_text = "NTSC-J ROM WAS SELECTED";
            console_t.AppendText(task_text + Environment.NewLine);
            commands(action);
        }




        //set pal rom as default
        private void select_pal(object sender, EventArgs e)
        {
            action = "use_pal true";
            game_ver = action;
            version_bool = true;
            task_text = "PAL ROM WAS SELECTED";
            console_t.AppendText(task_text + Environment.NewLine);
            commands(action);
        }




        //compilation
        private void compile(object sender, EventArgs e)
        {
            action = "start_compile";
            console_t.Text = string.Empty;
            task_text = "STARTING COMPILATION PLS WAIT...";
            console_t.AppendText(task_text + Environment.NewLine);
            commands(action);
        }




        //clean compiled files
        private void clean_compilation(object sender, EventArgs e)
        {
            action = "clean_comp";
            commands(action);
            console_t.Text = string.Empty;
            task_text = "CLEANING COMPILED FILES...";
            console_t.AppendText(task_text + Environment.NewLine);
        }




        //build rom
        private void build_rom(object sender, EventArgs e)
        {
            action = "mod_build";
            console_t.Text = string.Empty;
            commands(action);
        }




        //extract vanilla rom
        private void extract_rom(object sender, EventArgs e)
        {
            action = "mod_extract";
            console_t.Text = string.Empty;
            commands(action);
        }





        //create xdelta
        private void generate_xdelta(object sender, EventArgs e)
        {
            action = "mod_xdelta";
            commands(action);
        }




        //clean rom files
        private void clean_rom(object sender, EventArgs e)
        {
            action = "clean_iso";
            commands(action);
            console_t.Text = string.Empty;
            task_text = "CLEANING ROM FILES...";
            console_t.AppendText(task_text + Environment.NewLine);
        }




        //create dissasembly.elf
        private void generate_disasm(object sender, EventArgs e)
        {
            action = "make_disasm";
            task_text = "GENERATING DISSASEMBLY.ELF, LOOK IN DEBUG FOLDER.";
            console_t.AppendText(task_text + Environment.NewLine);
            commands(action);
        }




        //export textures as c files (idk what is this)
        private void ex_textures(object sender, EventArgs e)
        {
            action = "export_texturesc";
            commands(action);
        }





        //open debug folder
        private void open_logs(object sender, EventArgs e)
        {
            string prev_debug = MOD_DIR.Replace("/", "\\");
            debug_path = Path.Combine(prev_debug, "debug");

            if (Directory.Exists(debug_path))
            {
                Process.Start("explorer.exe", debug_path);
            }
            else
            {
                MessageBox.Show("cant open debug folder, make sure you tried to compile a mod before");
            }
        }

        private void psx_restart(object sender, EventArgs e)
        {
            if (active_buildlist)
            {
                restart_cmd();
                console_t.Text = string.Empty;
                open_cmd();
            }
            else
            {
                MessageBox.Show("No buildlist loaded");
            }
        }
    }
}
