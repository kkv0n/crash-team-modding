using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Crash_Team_Mod_Header
{
    public class modding
    {

        public Process psx_cmd;
        public StreamWriter psx_input;

        //important strings
        //TO DO: REDUCE THE NUMBER OF STRINGS
        public string PATHS_FILE;
        public string BACKUP_FOLDER;       //if your og folder have spaces
        public string PREV_SDK;
        public string TEMP_ROM;
        public string TEMP_SDK;
        public string BIGTOOL;
        public string COMPILE_LIST;
        public string MOD_NAME;
        public string MOD_DIR;
        public string ISO_PATH;
        public string MODDED_ROM;
        public string XDELTA_PATCH;
        public string NAME_ROM;
        public string PREV_PSX_DIR;
        public string PSX_DIR;
        public string PSX_TOOLS_PATH;
        public string REBUILD_FOLDER;
        public string CTR_TOOLS_PATH;
        public string DEBUG_PATH;
        public string BACKUP_MODDIR;
        public string ROM_REGION;
        public string DUCK_PATH;
        public string MODEL_PATH;
        public string LNG_PATH;
        public string XA_PATH;
        public string TOOLS_OPEN_THIS_FOLDER;
        public string DESIRED_ROM;
        public string ROM_TO_EXTRACT;

        //enum gui tasks
        public enum psx : byte
        {
            CLOSE,
            RESTART,
            TEMP_PATH,
            DUCK,
            RELOAD,
            OPEN,
            SEARCH_SDK,
            COMPILE,
            CLEAN_C,
            BUILD_ROM,
            CLEAN_R,
            XDELTA,
            EXTRACT,
            DISASSEMBLY,
            TEXTURES,
        } //TO DO: ENUM MAX TASKS

        public enum region : byte
        {
            CTR_USA = psx.TEXTURES + 1,
            CTR_PAL,
            CTR_JAPAN,
            CTR_PROTO,
            CTR_J_TRIAL
        }

        public enum modding_tools : byte
        {
            EXTRACT,
            REBUILD,
            XDELTA,
            LNG_CONVERT,
            MODEL_CONVERT,
            XA_OPEN,
            OPEN_FOLDER

        } 

        public enum paths : byte
        {
            READ_PATHS,
            REFRESH_ISO,
            WRITE_PATHS,
            SAVE_DUCK
        }

        public enum gui : byte
        {
            MAIN,
            ADVANCED,
           SET_DUCK_PATH
        }
    }
}
