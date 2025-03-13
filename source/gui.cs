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
using Microsoft.VisualBasic.FileIO;
using Crash_Team_Mod_Header;
using static Crash_Team_Mod_Header.modding;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using System.Timers;


namespace Crash_Team_Mod
{
    public partial class CTM : Form
    {
        modding mod = new modding();

        const byte _NULL = 0;


        public CTM()
        {


            InitializeComponent();
            buttons();
            print_welcome(true);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.gui_closing);
            romfile.Filter = "CTR ROM (*.bin) | *.bin; | All files (*.*) | *.*";
            duckexe.Filter = "DUCKSTATION (*.exe) | *.exe;";
            modelfile.Filter = "CTR MODEL FILE (*.ply, *.ctr) | *.ply;*.ctr";
            lngfile.Filter = "CTR LANGUAGE FILE (*.lng, *.txt) | *.lng;*.txt";
            xafile.Filter = "CTR XA AUDIO FILE (*.XA) | *.XA;";

            romfile.FilterIndex = 1;
            duckexe.FilterIndex = 1;
            modelfile.FilterIndex = 1;
            lngfile.FilterIndex = 1;
            xafile.FilterIndex = 1;

            //fill the strings
            mod.PATHS_FILE = @".\paths.txt";
            mod.BACKUP_FOLDER = @"C:\ctr-mod-files";
            mod.PREV_PSX_DIR = (Directory.Exists(Path.Combine(mod.BACKUP_FOLDER, "data"))) ?
                mod.BACKUP_FOLDER : System.IO.Directory.GetCurrentDirectory();
            mod.PSX_DIR = mod.PREV_PSX_DIR.Replace("\\", "/");
            mod.TEMP_SDK = Path.Combine(mod.BACKUP_FOLDER, "CTR-MOD-SDK");
            mod.ROM_REGION = "1"; // ntsc-u as default value just in case
            this.Load += move_folder;

            //execute this in real time
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 3000;
            timer.Tick += (sender, e) => check_process(); //always check if the buildlist are open
            timer.Start();

        }



        void create_log()
        {
            string logfile = Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt");

            using (var fs = new FileStream(logfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fs))
            {
                //clean the file/create a new one
            }
            update_logs();

        }

        //TO DO: MAKE A SEPARATE FUNCTION TO PRINT MESSAGEBOXES


        //to avoid cmd closing after a task finished
        bool active_buildlist = false;

        bool temp_sdk = false;


        void print_welcome(bool intro) //print welcome message
        {
            string intro_text = "text";

            if (intro)
            {
                intro_text = @"

        Welcome to Crash Team Modding:
            Modding for all people!
            
        Waiting for user selection...
        
        These are the available options

        BUILDLIST:

        Ctr-mod-sdk:
        - Compile
        - Clean Compilation files
        - Build Modded ISO
        - Extract Vanilla ISO
        - Create xdelta patch
        - Clean ISO files

        Debug:
        - Generate Disassemble Elf
        - Export textures as C file
        - Open ROM with duckstation
        
        CTR-TOOLS:

        Rom modding:
        - Extract
        - Rebuild
        - Generate xdelta
        
        Advanced:
        - Convert Character Models
           ->.ply -> .ctr -> .obj
           
        - Convert lang files
            .lng <-> .txt
            
        - Open XA files";
            }

            else if (!intro)
            {
                intro_text = @"

        CTR-TOOLS WAS SELECTED

       These are the available options:

        Debug:
        - Open ROM with duckstation
        
        Rom modding:
        - Extract
        - Rebuild
        - Generate xdelta
        
        Advanced:
        - Convert Character Models
           ->.ply -> .ctr -> .obj
           
        - Convert lang files
            .lng <-> .txt
            
        - Open XA files
";
            }

            console_t.SuspendLayout();
            console_t.Text = intro_text;
            console_t.ResumeLayout();
        }






        //move toolchain folder if the current folder have spaces
        void move_folder(object sender, EventArgs e)
        {
            if (mod.PSX_DIR.Contains(" ")) //if the folder contains spaces then use a temp folder
            {
                MessageBox.Show("Moving work folder pls wait...");

                string backup = Path.Combine(mod.PREV_PSX_DIR, "data");


                mod.PREV_PSX_DIR = mod.BACKUP_FOLDER;

                mod.PSX_DIR = mod.PREV_PSX_DIR.Replace("\\", "/");

                FileSystem.MoveDirectory(backup, Path.Combine(mod.PREV_PSX_DIR, "data"), true);

                change_paths((byte)paths.WRITE_PATHS); //update .txt with the new path
            }
            mod.PSX_TOOLS_PATH = Path.Combine(Path.Combine(mod.PREV_PSX_DIR, "data"), "tools");
            mod.CTR_TOOLS_PATH = Path.Combine(mod.PSX_TOOLS_PATH, "ctr-tools");
            mod.BIGTOOL = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "ctrtools"), "bigtool.exe"));
        }




        //check if the cmd is open
        public void check_process()
        {
            if (enable_mod.Checked) psx_execute((byte)psx.CLOSE);

            if (mod.psx_cmd != null && !mod.psx_cmd.HasExited)
            {
                return;
            }
            else
            {
                psx_execute((byte)psx.RELOAD);
            }
        }


        bool closing = false;

        //close cmd when the app is closed
        void gui_closing(object sender, FormClosingEventArgs e)
        {
            closing = true;

            if ((GET_MODDIR != null) && (temp_sdk))

            {
                mod.COMPILE_LIST = Path.Combine(mod.BACKUP_MODDIR, "buildList.txt");
                mod.MOD_DIR = mod.BACKUP_MODDIR;
                change_paths((byte)paths.WRITE_PATHS);
            }
            psx_execute((byte)psx.CLOSE);
        }


        System.Windows.Forms.Timer delay_logs;

        //fix this later
        void update_logs()
        {

            delay_logs = new System.Windows.Forms.Timer { Interval = 500 };
            delay_logs.Start();
            delay_logs.Tick += (s, e) =>
            {

                if (enable_mod.Checked || mod.psx_cmd == null && mod.psx_cmd.HasExited)
                {
                    delay_logs.Stop();
                    return;
                }

                if (!console_t.IsDisposed)
                {
                    console_t.Invoke((Action)(() =>
                    {


                        using (var fs = File.Open(Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {

                            console_t.SuspendLayout();

                            console_t.Text = reader.ReadToEnd();

                            console_t.ResumeLayout();

                        }

                    }));

                }
            };


        }





        //open the buildlist
        void open_python()
        {


            // in framework 3.5 you can only combine 2 paths with path.combine
            string python = Path.GetFullPath(Path.Combine(Path.Combine(Path.Combine(mod.PSX_TOOLS_PATH, "Python"), "Python310"), "python.exe"));
            string builder = Path.GetFullPath(Path.Combine(Path.Combine(mod.PSX_TOOLS_PATH, "mod-builder"), "main.py"));

            string logfile = Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt");



            mod.psx_cmd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = python,
                    Arguments = $"\"{builder}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            mod.psx_cmd.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    using (var fs = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(e.Data + Environment.NewLine);
                    }
                }
            };

            mod.psx_cmd.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    using (var fs = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.WriteLine($"[cmd] {e.Data}" + Environment.NewLine);
                    }
                }
                console_t.Invoke((Action)(() =>
                {
                    console_t.SelectionStart = console_t.Text.Length;
                    console_t.ScrollToCaret();
                }));
            };

            mod.psx_cmd.Start();
            mod.psx_cmd.BeginOutputReadLine();
            mod.psx_cmd.BeginErrorReadLine();
            mod.psx_input = mod.psx_cmd.StandardInput;
        }


















        string GET_MODDIR;  //original mod(no temp) folder

        //  cmd process
        void psx_execute(byte todo)
        {
            switch (todo)
            {


                case (byte)psx.CLOSE:
                    {
                        if (mod.psx_cmd != null && !mod.psx_cmd.HasExited)
                        {

                            psx_execute((byte)psx.RESTART);

                        }

                        active_buildlist = false;

                        break;
                    }



                case (byte)psx.RESTART:
                    {
                        //somehow this crashes the app
                        if (mod.psx_cmd != null && !mod.psx_cmd.HasExited) //if cmd process exists
                        {
                            mod.psx_input.Close();
                            mod.psx_cmd.Kill();
                        }
                        else
                        {

                            if (Process.GetProcessesByName("python").Length > 0)
                            {
                                MessageBox.Show("previous python procces was found, press ok to close it"); //avoid annoying antivirus alert

                                foreach (Process python_process in Process.GetProcessesByName("python")) //if cmd process not exists then search if python is open in the background
                                {
                                    python_process.Kill();
                                    python_process.WaitForExit();

                                }
                            }

                        }

                        if (mod.psx_cmd != null && !mod.psx_cmd.HasExited)
                        {
                            using (var fs = new FileStream(Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                            using (var writer = new StreamWriter(fs))
                            {
                                writer.WriteLine("CMD WAS RESTARTED" + Environment.NewLine);
                            }
                        }

                        break;
                    }

                case ((byte)psx.TEMP_PATH):
                    {
                        GET_MODDIR = System.IO.Directory.GetParent(mod.COMPILE_LIST).FullName;

                        psx_execute((byte)psx.SEARCH_SDK);

                        if (mod.PREV_SDK.Contains(" "))
                        {
                            MessageBox.Show("your folder contains spaces, creating a temp folder... pls wait" + mod.TEMP_SDK); //trying to avoid the annoying antivirus alert

                            if (Directory.Exists(mod.BACKUP_MODDIR))
                            {
                                MessageBox.Show("cleaning temp files..."); //trying to avoid the annoying antivirus alert
                                Directory.Delete(mod.BACKUP_MODDIR, true);
                            }

                            if (!Directory.Exists(mod.BACKUP_FOLDER))
                                System.IO.Directory.CreateDirectory(mod.BACKUP_FOLDER);

                            FileSystem.CopyDirectory(mod.PREV_SDK, mod.TEMP_SDK, true);
                            temp_sdk = true;

                        }
                        else
                        {
                            temp_sdk = false;
                        }

                        break;
                    }

                case ((byte)psx.DUCK):
                    {
                        if (mod.DUCK_PATH == null)
                        {
                            switch_gui((byte)gui.SET_DUCK_PATH);
                            return;
                        }

                        string romname = mod.NAME_ROM.Replace(".bin", ""); //TO DO: replace this with getfilenamewithoutextension
                        string executable = mod.DUCK_PATH;
                        mod.DESIRED_ROM = (!rebuild_rom) ?
                        Path.Combine(mod.ISO_PATH, romname + "_" + mod.MOD_NAME + ".bin") : mod.MODDED_ROM;

                        if (!File.Exists(mod.DESIRED_ROM))
                        {
                            MessageBox.Show("cant find the rom:" + mod.DESIRED_ROM);
                            return;
                        }

                        string args = $"-batch \"{mod.DESIRED_ROM}\"";

                        execute_process(6, executable, args, mod.PREV_PSX_DIR);

                        break;
                    }

                case (byte)psx.OPEN:
                    {
                        if (!Directory.Exists(mod.PREV_PSX_DIR))
                        {
                            MessageBox.Show("error: cant find data folder, data folder is lost, pls download the app again");
                            return;
                        }

                        psx_execute((byte)psx.RESTART);
                        open_python();

                        break;
                    }


                case (byte)psx.SEARCH_SDK:
                    {
                        string search_file = "config.json";  //search this file in back folders
                        string search_folder = Path.GetFullPath(GET_MODDIR); //buildlist folder

                        while (!String.IsNullOrEmpty(search_folder))
                        {
                            string[] search_all = Directory.GetFiles(search_folder, search_file);

                            if (search_all.Length > 0)
                            {
                                mod.PREV_SDK = search_folder;
                                return;
                            }

                            search_folder = Directory.GetParent(search_folder)?.FullName;
                        }

                        if (mod.PREV_SDK == null)
                        {
                            MessageBox.Show("Cant find SDK folder, make sure you downloaded it correctly");

                        }
                        break;

                    }


                case (byte)psx.RELOAD:
                    {
                        if (active_buildlist)
                        {
                            psx_execute((byte)psx.OPEN);
                        }

                        break;
                    }

            }

            if (todo >= (byte)psx.COMPILE && todo < (byte)region.CTR_USA)
            {
                using (var fs = new FileStream(Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fs))
                {

                }
            }

            if (todo >= (byte)psx.COMPILE)
            {
                commands(todo);
            
             
            }
        }




        //write commands in the cmd using app buttons
        void commands(byte command) //TO DO: MERGE ROM COMMANDS WITH PSX COMMANDS
        {
            if ((temp_sdk) && (command < (byte)region.CTR_USA)) //refresh the temp sdk with latest changes in the folder
            {
                MessageBox.Show("updating temp folder..."); //trying to avoid the annoying antivirus alert

                foreach (var moddir in Directory.GetFileSystemEntries(mod.BACKUP_MODDIR)) //copy temp mod folder
                {

                    if (Path.GetFileName(moddir) == "buildList.txt") //skip buildList because it breaks the program
                    {
                        continue;
                    }


                    string files = Path.Combine(mod.MOD_DIR, Path.GetFileName(moddir));


                    if (File.Exists(moddir))
                    {
                        File.Copy(moddir, files, true);
                    }

                    else if (Directory.Exists(moddir))
                    {
                        Directory.CreateDirectory(files);
                        foreach (var file in Directory.GetFiles(moddir))
                        {
                            File.Copy(file, Path.Combine(files, Path.GetFileName(file)), true);
                        }
                    }
                }






            }


            string[] ACTION = { "start_compile", "clean_comp", "mod_build", "clean_iso", "mod_xdelta", "mod_extract",
                "make_disasm", "export_texturesc", "sel_usa true", "sel_pal true", "sel_jap true", "sel_proto true",
                "sel_japtrial true"};

            if (command >= (byte)region.CTR_USA)
            {
                byte region = (byte)((command - (byte)psx.TEXTURES) & 0xFF);
                mod.ROM_REGION = region.ToString(); //this will be saved in paths.txt
            }

            command = (byte)((command - (byte)psx.COMPILE) & 0xFF);// match numbers with the array

            string[] TASK_TEXT = { "STARTING COMPILATION...", "CLEANING COMPILATION FILES...",
            "BUILDING ROM...", "CLEANING OLD ROM FILES...", "MAKING XDELTA...", "EXTRACTING ROM FILES...",
            "GENERATING DISASSEMBLY.ELF, LOOK IN DEBUG FOLDER...", "EXPORTING TEXTURES...", "NTSC-U SELECTED",
            "PAL SELECTED", "NTSC-J SELECTED", "PROTO SEP 3 SELECTED", "JAPAN TRIAL SELECTED"
            };

            //probably dead code, need to refactorize later
            if ((mod.psx_cmd == null || mod.psx_cmd.HasExited)) //if cmd is closed
            {
                byte operation = (byte)(((byte)region.CTR_USA - (byte)psx.COMPILE) & 0xFF);

                // only show warning if this is not called by version buttons
                if (command < operation) MessageBox.Show("The console is not open, first drag a buildlist in the square at the right side.");

                return;

            }

            using (var fs = new FileStream(Path.Combine(Path.Combine(mod.PSX_DIR, "data"), "logs.txt"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine(TASK_TEXT[command] + Environment.NewLine);
            }





            mod.psx_input.WriteLine(ACTION[command]);
        }







        //block non ctr tools functions
        void buttons()
        {
            bool swap_buttons = enable_mod.Checked;
            var cmdbuttons = new[] {
            comp_b, cleanc_b, cleanr_b, buildc_b, exc_b, xd_b,dis_b, log_b, tex_b, psx_r, usa_b, pal_b, jap_b,
            sep3_b, jtrial_b};
            var toolbuttons = new[] { rebuild_toolsb, ext_toolsb, xdelta_toolsb, adv_toolsb, };



            drag_buildlist.Enabled = (swap_buttons) ? false : true;
            prev_settingsb.Enabled = (swap_buttons) ? false : true;


            //block modding buttons if the checkbox is not enabled
            foreach (Control control in groupBox1.Controls)
            {
                if (control is System.Windows.Forms.Button button)
                {

                    if (cmdbuttons.Contains(button))
                    {
                        button.Enabled = (swap_buttons) ? false : true;
                    }
                    else if (toolbuttons.Contains(button))
                    {
                        button.Enabled = (swap_buttons) ? true : false;
                    }
                }
            }
        }





        //update paths from the .txt file
        void change_paths(byte task)
        {

            //replace these lines in paths.txt
            var replacements = new Dictionary<string, string>
                 {
                     { "COMPILE_LIST", mod.COMPILE_LIST },
                     { "MOD_DIR", mod.MOD_DIR },
                     { "MOD_NAME", mod.MOD_NAME },
                     { "PSX_DIR", mod.PSX_DIR },
                     { "NAME_ROM", mod.NAME_ROM},
                     {"ISO_PATH", mod.ISO_PATH},
                     {"ROM_REGION", mod.ROM_REGION},
                     {"DUCK_PATH", mod.DUCK_PATH }

            };


            string[] lines = File.ReadAllLines(mod.PATHS_FILE);



            var updatedLines = new System.Collections.Generic.List<string>();


            foreach (var line in lines)
            {
                string updatedLine = line;

                if (task == (byte)paths.READ_PATHS) //if loading previous config
                {
                    foreach (var path in replacements.Keys.ToList())
                    {
                        string format = $@"^{path}\s*=\s*""([^""]+)""";
                        Match match = Regex.Match(line, format);
                        if (match.Success)
                        {

                            if (path == "COMPILE_LIST") mod.COMPILE_LIST = match.Groups[1].Value;
                            if (path == "MOD_DIR") mod.MOD_DIR = match.Groups[1].Value;
                            if (path == "MOD_NAME") mod.MOD_NAME = match.Groups[1].Value;
                            if (path == "PSX_DIR") mod.PSX_DIR = match.Groups[1].Value;
                            if (path == "NAME_ROM") mod.NAME_ROM = match.Groups[1].Value;
                            if (path == "ISO_PATH") mod.ISO_PATH = match.Groups[1].Value;
                            if (path == "ROM_REGION") mod.ROM_REGION = match.Groups[1].Value;
                            if (path == "DUCK_PATH") mod.DUCK_PATH = match.Groups[1].Value;

                        }
                    }

                }
                else
                {

                    foreach (var path in replacements)
                    {
                        if (task == (byte)paths.SAVE_DUCK)
                        {
                            if (path.Key == "DUCK_PATH")
                                updatedLine = Regex.Replace(updatedLine, $@"^DUCK_PATH\s*=\s*\"".*?\""", $"DUCK_PATH = \"{mod.DUCK_PATH}\"");
                        }
                        else
                        {
                            if (rom.Text == String.Empty) return;

                            if (task == (byte)paths.REFRESH_ISO) //if update iso path
                            {

                                if (path.Key == "ISO_PATH")
                                    updatedLine = Regex.Replace(updatedLine, $@"^ISO_PATH\s*=\s*\"".*?\""", $"ISO_PATH = \"{mod.ISO_PATH}\"");


                                if (path.Key == "NAME_ROM")
                                    updatedLine = Regex.Replace(updatedLine, $@"^NAME_ROM\s*=\s*\"".*?\""", $"NAME_ROM = \"{mod.NAME_ROM}\"");

                                if (path.Key == "ROM_REGION")
                                    updatedLine = Regex.Replace(updatedLine, $@"^ROM_REGION\s*=\s*\"".*?\""", $"ROM_REGION = \"{mod.ROM_REGION}\"");

                            }
                            else //if new buildList
                            {

                                updatedLine = Regex.Replace(updatedLine, $@"^{path.Key}\s*=\s*\"".*?\""", $"{path.Key} = \"{path.Value}\"");
                            }
                        }
                    }

                    updatedLines.Add(updatedLine);



                    File.WriteAllLines(mod.PATHS_FILE, updatedLines.ToArray());
                }
            }

            if (task == (byte)paths.SAVE_DUCK) return;

            if (mod.ISO_PATH == null) return;

            //set the path for ctr tools functions
            mod.MODDED_ROM = Path.Combine(mod.ISO_PATH, "ctr_rebuild.bin");
            mod.XDELTA_PATCH = Path.Combine(mod.ISO_PATH, "ctr_rebuild.xdelta");
        }





        //hide or show gui buttons
        void switch_gui(byte swap)
        {

            List<Control> hide = new List<Control> { dis_b, tex_b, log_b, comp_b, cleanc_b, buildc_b, exc_b, xd_b, cleanr_b, psx_r,
            usa_b, jap_b, pal_b, sep3_b, jtrial_b, ext_toolsb, adv_toolsb, rebuild_toolsb, xdelta_toolsb, console_t, enable_mod,
            drag_buildlist, d_label, db_label, m_label, drag_buildlist, openr_b, prev_settingsb};



            rom.Visible = (swap != (byte)gui.SET_DUCK_PATH) ? true : false;
            search_rom.Visible = (swap != (byte)gui.SET_DUCK_PATH) ? true : false;
            r_label.Visible = (swap != (byte)gui.SET_DUCK_PATH) ? true : false;

            foreach (Control control in groupBox1.Controls)
            {

                if (hide.Contains(control))
                {
                    if (swap > (byte)gui.MAIN)
                    {
                        control.Hide();
                    }
                    else
                    {
                        control.Show();
                    }
                }
            }
            if (swap == (byte)gui.SET_DUCK_PATH)
            {
                adv_goback.Location = new Point(420, 230);
                duck.Location = new Point(370, 206);
                search_duckb.Location = new Point(579, 206);
                duck_label.Location = new Point(370, 182);
            }

            if (swap == (byte)gui.ADVANCED)
            {

                adv_lngconv.Location = new Point(24, 71);
                adv_lngfolder.Location = new Point(24, 95);
                adv_lngpath.Location = new Point(24, 47);
                adv_lngsearch.Location = new Point(143, 47);
                adv_modelconv.Location = new Point(240, 71);
                adv_modelfolder.Location = new Point(240, 95);
                adv_modelpath.Location = new Point(240, 47);
                adv_modelsearch.Location = new Point(360, 47);
                adv_openxa.Location = new Point(24, 203);
                adv_xapath.Location = new Point(24, 179);
                adv_searchxa.Location = new Point(143, 179);
                adv_xaudiofolder.Location = new Point(24, 247);
                adv_trackfolder.Location = new Point(240, 203);
                adv_tscreenfolder.Location = new Point(240, 227);
                adv_howlfolder.Location = new Point(423, 141);
                adv_tools_gui.Location = new Point(423, 175);
                adb_rebfolder.Location = new Point(415, 220);
                adv_goback.Location = new Point(16, 400);
                ot_label.Location = new Point(433, 121);
                lng_label.Location = new Point(24, 20);
                md_label.Location = new Point(240, 20);
                xa_label.Location = new Point(24, 159);
                r_label.Text = "ORIGINAL BASE ROM";

            }
            else if (swap == (byte)gui.MAIN)
            {
                adv_lngconv.Location = new Point(1064, 243);
                adv_lngfolder.Location = new Point(1064, 243);
                adv_lngpath.Location = new Point(1064, 243);
                adv_lngsearch.Location = new Point(1064, 243);
                adv_modelconv.Location = new Point(1064, 243);
                adv_modelfolder.Location = new Point(1064, 243);
                adv_modelpath.Location = new Point(1064, 243);
                adv_modelsearch.Location = new Point(1064, 243);
                adv_openxa.Location = new Point(1064, 243);
                adv_xapath.Location = new Point(1064, 243);
                adv_searchxa.Location = new Point(1064, 243);
                adv_xaudiofolder.Location = new Point(1064, 243);
                adv_trackfolder.Location = new Point(1064, 243);
                adv_tscreenfolder.Location = new Point(1064, 243);
                adv_goback.Location = new Point(1064, 243);
                adv_howlfolder.Location = new Point(1064, 243);
                adv_tools_gui.Location = new Point(1064, 243);
                adb_rebfolder.Location = new Point(1064, 243);
                duck.Location = new Point(1064, 243);
                search_duckb.Location = new Point(1064, 243);
                ot_label.Location = new Point(1064, 243);
                lng_label.Location = new Point(1064, 243);
                md_label.Location = new Point(1064, 243);
                xa_label.Location = new Point(1064, 243);
                duck_label.Location = new Point(1064, 243);
                r_label.Text = "SELECT YOUR ROM";
            }

        }

        bool rebuild_rom = false; //select the current rom for duckstation


        private void enable_mod_CheckedChanged(object sender, EventArgs e)
        {
            psx_execute((byte)psx.CLOSE);
            print_welcome(!enable_mod.Checked);
            buttons();
            rebuild_rom ^= true;

            prev_settingsb.Checked = false;
            prev_settingsb.Enabled = true;

        }






        //set rom path
        private void search_rom_Click(object sender, EventArgs e) //TO DO: MERGE NAME_ROM WITH ISO_PATH AND ONLY SEPARATE IT IN CMD
        {
            if (romfile.ShowDialog() == DialogResult.OK)
            {
                rom.Text = romfile.FileName;

                //if rom name hace spaces
                if (romfile.FileName.Contains(" "))
                {
                    MessageBox.Show("your rom path contains spaces, a temp folder will be created");

                    if (!Directory.Exists(mod.BACKUP_FOLDER))
                    {
                        System.IO.Directory.CreateDirectory(mod.BACKUP_FOLDER);
                    }

                    mod.TEMP_ROM = Path.Combine(Path.Combine(mod.BACKUP_FOLDER, "rom"), "ctr.bin");
                    FileSystem.CopyFile(romfile.FileName, mod.TEMP_ROM, true);
                    Process.Start("explorer.exe", System.IO.Directory.GetParent(mod.TEMP_ROM).FullName);

                    mod.ISO_PATH = System.IO.Directory.GetParent(mod.TEMP_ROM).FullName;
                    mod.NAME_ROM = System.IO.Path.GetFileName(mod.TEMP_ROM);
                }
                else
                {
                    mod.NAME_ROM = System.IO.Path.GetFileName(romfile.FileName);
                    mod.ISO_PATH = System.IO.Directory.GetParent(romfile.FileName).FullName;
                }


                change_paths((byte)paths.REFRESH_ISO);
            }
        }




        //set buildlist path
        private void drop_buildlist(object sender, DragEventArgs e)
        {

            if (rom.Text == String.Empty)
            {
                MessageBox.Show("pls select a rom first");
                return;
            }

            create_log();

            // get the path of buildList.txt
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            mod.COMPILE_LIST = files[0];

            psx_execute((byte)psx.TEMP_PATH); // check if paths contains spaces

            mod.BACKUP_MODDIR = GET_MODDIR;
            string PREV_MODDIR = (temp_sdk) ? Path.Combine(mod.TEMP_SDK, GET_MODDIR.Substring(mod.PREV_SDK.Length).TrimStart(Path.DirectorySeparatorChar))
                : mod.BACKUP_MODDIR;
            string PREV_COMPILE_LIST = Path.Combine(PREV_MODDIR, "buildList.txt");
            mod.MOD_NAME = System.IO.Path.GetFileName(PREV_MODDIR);
            PREV_MODDIR += "\\";
            mod.MOD_DIR = PREV_MODDIR.Replace("\\", "/");

            MessageBox.Show(mod.BACKUP_MODDIR);

            //show current buildlist path
            MessageBox.Show($"buildList selected: {mod.COMPILE_LIST}");
            mod.COMPILE_LIST = PREV_COMPILE_LIST; //dont show this in the messagebox bc it can be a temp folder


            //update txt paths
            change_paths((byte)paths.WRITE_PATHS);

            //open buildlist
            psx_execute((byte)psx.OPEN);

            active_buildlist = true;
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


        //decides the rom region
        void set_rom_region(byte reg)
        {
            commands(reg);
            change_paths((byte)paths.REFRESH_ISO);
        }

        //set usa rom as default
        private void select_usa(object sender, EventArgs e)
        {
            set_rom_region((byte)region.CTR_USA);
        }



        //set japanese rom as default
        private void select_japan(object sender, EventArgs e)
        {
            set_rom_region((byte)region.CTR_JAPAN);
        }




        //set pal rom as default
        private void select_pal(object sender, EventArgs e)
        {
            set_rom_region((byte)region.CTR_PAL);
        }

        //set prototype september 3 rom as default
        private void select_protosep3(object sender, EventArgs e)
        {
            set_rom_region((byte)region.CTR_PROTO);

        }

        //set japanese trial rom as default
        private void select_jtrial(object sender, EventArgs e)
        {
            set_rom_region((byte)region.CTR_J_TRIAL);
        }


        //compilation
        private void compile(object sender, EventArgs e)
        {
            psx_execute((byte)psx.COMPILE);
        }




        //clean compiled files
        private void clean_compilation(object sender, EventArgs e)
        {
            psx_execute((byte)psx.CLEAN_C);
        }




        //build rom
        private void build_rom(object sender, EventArgs e)
        {
            psx_execute((byte)psx.BUILD_ROM);
        }




        //extract vanilla rom
        private void extract_rom(object sender, EventArgs e)
        {
            psx_execute((byte)psx.EXTRACT);
        }





        //create xdelta
        private void generate_xdelta(object sender, EventArgs e)
        {
            psx_execute((byte)psx.XDELTA);
        }




        //clean rom files
        private void clean_rom(object sender, EventArgs e)
        {
            if (mod.NAME_ROM != null)
            {
                string romname = Path.GetFileNameWithoutExtension(mod.NAME_ROM);

                if (Directory.Exists(Path.Combine(mod.ISO_PATH, romname)))
                {
                    DialogResult result = MessageBox.Show($"should delete rom temp folder: \"{Path.Combine(mod.ISO_PATH, romname)}\" ?",
                    "Confirm", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes) //avoid antivirus alert
                    {
                        Directory.Delete(Path.Combine(mod.ISO_PATH, romname), true);
                        File.Delete(Path.Combine(mod.ISO_PATH, romname + ".xml"));
                    }

                }
            }



            psx_execute((byte)psx.CLEAN_R);
        }




        //create dissasembly.elf
        private void generate_disasm(object sender, EventArgs e)
        {
            psx_execute((byte)psx.DISASSEMBLY);
        }




        //export textures as c files (idk what is this)
        private void ex_textures(object sender, EventArgs e)
        {
            psx_execute((byte)psx.TEXTURES);
        }





        //open debug folder
        private void open_logs(object sender, EventArgs e)
        {
            string error = "cant open debug folder, make sure you compiled a mod before";

            if (mod.MOD_DIR == null)
            {
                MessageBox.Show(error);
                return;
            }

            mod.DEBUG_PATH = Path.Combine(mod.MOD_DIR.Replace("/", "\\"), "debug");

            if (Directory.Exists(mod.DEBUG_PATH))
            {
                Process.Start("explorer.exe", mod.DEBUG_PATH);
            }
            else
            {
                MessageBox.Show(error);
            }
        }





        //restart cmd
        private void psx_restart(object sender, EventArgs e)
        {
            if (!active_buildlist) MessageBox.Show("No buildlist loaded");

            psx_execute((byte)psx.RELOAD);
        }



        //if loading previous presets
        private void load_presets(object sender, EventArgs e)
        {
            if (enable_mod.Checked) return;

            byte i;

            //load paths from the txt
            change_paths((byte)paths.READ_PATHS);

            if (mod.COMPILE_LIST == null)
            {
                MessageBox.Show("invalid buildlist path, pls load a new buildlist");
                return;
            }

            if (mod.ISO_PATH == null)
            {
                MessageBox.Show("invalid iso path, pls select a new one");
                return;
            }


            string[] presets = { mod.COMPILE_LIST, Path.Combine(mod.ISO_PATH, mod.NAME_ROM) };


            for (i = 0; i < 2; i++)
            {

                if (!File.Exists(presets[i]))
                {
                    if (i == 0) MessageBox.Show("cant find:" + presets[i]);

                    if (i == 1) MessageBox.Show("the rom file:" + presets[i] + "doesnt exist");

                    return;
                }
            }

            create_log();

            rom.Text = Path.Combine(mod.ISO_PATH, mod.NAME_ROM);

            //check if the folder have spaces
            psx_execute((byte)psx.TEMP_PATH);

            //open the buildList
            psx_execute((byte)psx.OPEN);
            active_buildlist = true;

            prev_settingsb.Enabled = false;
        }



        //open duckstation
        private void open_duck(object sender, EventArgs e)
        {
            psx_execute((byte)psx.DUCK);
        }



        private void duck_Search(object sender, EventArgs e)
        {
            if (duckexe.ShowDialog() == DialogResult.OK)
            {
                //update textbox and duckpath string
                duck.Text = duckexe.FileName;
                mod.DUCK_PATH = duckexe.FileName;
                change_paths((byte)paths.SAVE_DUCK);

                psx_execute((byte)psx.DUCK); //open duck
            }
        }

        //to open a program
        void execute_process(byte mode, string file, string args, string workdir) //TO DO: ADD ARG FOR CREATENOWINDOW
        {

            //0 = all false no wait exit
            // 1 = redirect false shell true & wait exit
            // 2 = redirect true shell false no wait exit
            // 3 = redirect true shell true & wait exit
            // 4 = redirect true shell true no wait exit
            // 5 = redirect true shell false wait exit
            // 6 = redirect false shell false wait exit

            ProcessStartInfo exec_process = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = workdir,
                RedirectStandardOutput = (mode < 2 || mode == 6) ? false : true,
                RedirectStandardError = (mode < 2 || mode == 6) ? false : true,
                UseShellExecute = (mode == 1 || mode > 2 && mode < 5) ? true : false,
                CreateNoWindow = true
            };
            using (Process process = new Process { StartInfo = exec_process })
            {
                process.Start();

                if ((mode == 1 || (mode >= 3 && mode != 4))) process.WaitForExit();

                if (mode >= 2 && mode <= 5)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    if (String.IsNullOrEmpty(error)) MessageBox.Show("ERROR:" + error);

                }

            }
        }




        //ctr tools functions
        void ctr_tools_funcs(byte func)
        {
            string filename;
            string args;

            switch (func)
            {


                case ((byte)modding_tools.EXTRACT):
                    {

                        if (!File.Exists(mod.ROM_TO_EXTRACT) || mod.ROM_TO_EXTRACT == null) //just in case
                        {
                            MessageBox.Show("Error! Cant find the rom.");
                            return;
                        }

                        MessageBox.Show("Extracting the rom... pls wait");

                        mod.REBUILD_FOLDER = Path.Combine(mod.ISO_PATH, "ctr_rebuild");

                        string ext_rom = Path.GetFullPath(mod.ROM_TO_EXTRACT); //rom name

                        //dumpiso args
                        filename = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "mkpsxiso"), "dumpsxiso.exe"));
                        args = $"\"{ext_rom}\" -x \"{mod.REBUILD_FOLDER}\" -s \"{mod.REBUILD_FOLDER}.xml\"";
                        execute_process(6, filename, args, mod.PREV_PSX_DIR); //dumpiso

                        //bigtool args
                        filename = mod.BIGTOOL;
                        args = $"\"{Path.Combine(mod.REBUILD_FOLDER, "bigfile.big")}\"";
                        execute_process(6, filename, args, mod.PREV_PSX_DIR); //bigtool

                        MessageBox.Show("Rom succesfully extracted in:" + mod.REBUILD_FOLDER);
                        break;
                    }



                case ((byte)modding_tools.REBUILD):
                    {
                        string mkpsxiso = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "mkpsxiso"), "mkpsxiso.exe"));


                        if (!Directory.Exists(mod.REBUILD_FOLDER) || mod.REBUILD_FOLDER == null)
                        {
                            MessageBox.Show("Error! ctr_rebuild folder not exist.");
                            return;
                        }

                        MessageBox.Show("Rebuilding the rom... pls wait");

                        //big tools args
                        filename = mod.BIGTOOL;
                        args = $"\"{Path.Combine(mod.REBUILD_FOLDER, "bigfile.txt")}\"";
                        execute_process(6, filename, args, mod.REBUILD_FOLDER); //build bigtools


                        //mkpsxiso args
                        filename = mkpsxiso;
                        args = $"\"{Path.Combine(mod.ISO_PATH, "ctr_rebuild.xml")}\" -y -q -o \"{mod.MODDED_ROM}\"";
                        execute_process(6, filename, args, mod.PREV_PSX_DIR); //mkpsxiso

                        MessageBox.Show("Rom rebuilded!");
                        break;
                    }



                case ((byte)modding_tools.XDELTA):
                    {
                        string xdelta3 = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "ctrtools"), "xdelta3.exe"));

                        if (!File.Exists(mod.MODDED_ROM) || mod.MODDED_ROM == null)
                        {
                            MessageBox.Show("cant find ctr_rebuild.bin");
                            return;
                        }
                        else if (rom.Text == String.Empty || !File.Exists(rom.Text))
                        {
                            MessageBox.Show("first select a vanilla rom to make the xdelta");
                        }

                        //xdelta args
                        filename = xdelta3;
                        args = $"-e -s \"{rom.Text}\" \"{mod.MODDED_ROM}\" \"{mod.XDELTA_PATCH}\"";
                        execute_process(6, filename, args, mod.PREV_PSX_DIR); //make xdelta

                        MessageBox.Show("xdelta created!");
                        break;
                    }




                case ((byte)modding_tools.LNG_CONVERT):
                    {
                        if (mod.LNG_PATH == null || !File.Exists(mod.LNG_PATH))
                        {
                            MessageBox.Show("no LNG selected");
                            return;
                        }

                        string lng2text = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "ctrtools"), "lng2txt.exe"));

                        //lng2txt args
                        filename = lng2text;
                        args = $" \"{mod.LNG_PATH}\"";
                        execute_process(6, filename, args, Directory.GetParent(mod.LNG_PATH).FullName); //convert .lng to .txt & .txt to .lng

                        MessageBox.Show("LNG succesfully converted");

                        break;
                    }


                case ((byte)modding_tools.MODEL_CONVERT):
                    {
                        if (mod.MODEL_PATH == null || !File.Exists(mod.MODEL_PATH))
                        {
                            MessageBox.Show("no model selected");
                            return;
                        }

                        string model_reader = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "ctrtools"), "model_reader.exe"));
                        MessageBox.Show(model_reader);

                        //model_reader args
                        filename = model_reader;
                        args = $"\"{mod.MODEL_PATH}\"";
                        execute_process(6, filename, args, Directory.GetParent(mod.MODEL_PATH).FullName); //convert .ply to .ctr & .ctr to .obj

                        MessageBox.Show("model succesfully converted");

                        break;
                    }


                case ((byte)modding_tools.XA_OPEN):
                    {
                        if (mod.XA_PATH == null || !File.Exists(mod.XA_PATH))
                        {
                            MessageBox.Show("cant find" + mod.XA_PATH);
                            return;
                        }
                        string xa = Path.GetFullPath(Path.Combine(Path.Combine(mod.PSX_TOOLS_PATH, "xa_converter"), "XAConv.exe"));

                        //open xa converter
                        filename = xa;
                        args = $"\"{mod.XA_PATH}\"";
                        execute_process(6, filename, args, mod.PREV_PSX_DIR);

                        break;
                    }



                case ((byte)modding_tools.OPEN_FOLDER):
                    {
                        Process.Start("explorer.exe", mod.TOOLS_OPEN_THIS_FOLDER); //open desired ctr folder
                        break;
                    }
            }

        }

        //ctr tools things
        private void tools_extract(object sender, EventArgs e)
        {
            if (romfile.ShowDialog() == DialogResult.OK)
            {
                mod.ROM_TO_EXTRACT = romfile.FileName;
                ctr_tools_funcs((byte)modding_tools.EXTRACT);
            }
        }


        private void tools_build(object sender, EventArgs e)
        {
            ctr_tools_funcs((byte)modding_tools.REBUILD);
        }

        private void tools_xdelta(object sender, EventArgs e) //TO DO: IMPLEMENT XDELTA CREATOR WITH ANY ROM SELECTED
        {
            ctr_tools_funcs((byte)modding_tools.XDELTA);
        }

        private void tools_advanced(object sender, EventArgs e)
        {
            switch_gui((byte)gui.ADVANCED);
        }
        private void tools_goback(object sender, EventArgs e)
        {
            switch_gui((byte)gui.MAIN);
        }

        private void tools_lngSearch(object sender, EventArgs e)
        {
            if (lngfile.ShowDialog() == DialogResult.OK)
            {
                adv_lngpath.Text = lngfile.FileName;
                mod.LNG_PATH = lngfile.FileName;
            }
        }

        private void tools_modelSearch(object sender, EventArgs e)
        {
            if (modelfile.ShowDialog() == DialogResult.OK)
            {
                adv_modelpath.Text = modelfile.FileName;
                mod.MODEL_PATH = modelfile.FileName;
            }
        }

        private void tools_xaSearch(object sender, EventArgs e)
        {
            if (xafile.ShowDialog() == DialogResult.OK)
            {
                adv_xapath.Text = xafile.FileName;
                mod.XA_PATH = xafile.FileName;
            }
        }

        //convert ctr files
        private void tools_convertlng(object sender, EventArgs e)
        {
            ctr_tools_funcs((byte)modding_tools.LNG_CONVERT);
        }

        private void tools_convertmodel(object sender, EventArgs e)
        {
            ctr_tools_funcs((byte)modding_tools.MODEL_CONVERT);
        }

        private void tools_openXAconvert(object sender, EventArgs e)
        {
            ctr_tools_funcs((byte)modding_tools.XA_OPEN);
        }


        //open ctr folders
        private void tools_lngFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(Path.Combine(mod.REBUILD_FOLDER, "bigfile"), "lang"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_modelFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(Path.Combine(Path.Combine(Path.Combine(mod.REBUILD_FOLDER, "bigfile"),
                "models"), "racers"), "hi"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_xaFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(mod.REBUILD_FOLDER, "XA"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_tittleFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(Path.Combine(mod.REBUILD_FOLDER, "bigfile"), "screen"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_trackFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(Path.Combine(Path.Combine(mod.REBUILD_FOLDER, "bigfile"), "levels"), "tracks"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_howlFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER =
                Path.GetFullPath(Path.Combine(mod.REBUILD_FOLDER, "SOUNDS"));

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        private void tools_ctrFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(mod.REBUILD_FOLDER))
            {
                MessageBox.Show("cant find:" + mod.REBUILD_FOLDER);
                return;
            }

            mod.TOOLS_OPEN_THIS_FOLDER = mod.REBUILD_FOLDER;

            ctr_tools_funcs((byte)modding_tools.OPEN_FOLDER);
        }

        //open ctr tools gui
        private void tools_openTOOLSgui(object sender, EventArgs e)
        {
            string program = Path.GetFullPath(Path.Combine(Path.Combine(mod.CTR_TOOLS_PATH, "r15"), "CTR-tools-gui.exe"));

            Process.Start(program);
        }


    }
}

