# files modified by penta3
# made for "embedded system" compatibility purposes like my potato pc :p

"""
Reads in the all the diretories
Does stuff with them
Runs in basically an infinite while loop
TODO: Replace with Click
"""
import _files # check_file, check_files, delete_file, create_directory, delete_directory
from makefile import Makefile, clean_pch
from compile_list import CompileList, free_sections, print_errors
from syms import Syms
from redux import Redux
from common import MOD_NAME, GAME_NAME, LOG_FILE, COMPILE_LIST, DEBUG_FOLDER, BACKUP_FOLDER, OUTPUT_FOLDER, COMPILATION_RESIDUES, TEXTURES_FOLDER, TEXTURES_OUTPUT_FOLDER, request_user_input, cli_clear, DISC_PATH, SETTINGS_PATH, COMPILE_FOLDER, GCC_ELF_FILE, MIPS_PATH, PATHS_FILE, PYTHON_PORTABLE, show_paths
from mkpsxiso import Mkpsxiso
from nops import Nops
from image import create_images, clear_images, dump_images
from clut import clear_cluts, dump_cluts
from c import export_as_c

import logging
import os
import pathlib
import glob
import subprocess
import sys
import game_options

logger = logging.getLogger(__name__)

class Main:
    def __init__(self) -> None:

        self.redux = Redux()
        self.mkpsxiso = Mkpsxiso()
        self.nops = Nops()
        self.debug = show_paths
        self.actions = {
            'start_compile': self.compile,
            'clean_comp': self.clean_files,
            'mod_extract': self.mkpsxiso.extract_iso, # makes it awkward to pass arguments
            'mod_build': self.mkpsxiso.build_iso,
            'mod_xdelta': self.mkpsxiso.xdelta,
            'clean_iso': self.mkpsxiso.clean,
            7   :   self.redux.hot_reload, # not supported -penta3
            8   :   self.redux.restore, # not supported -penta3
            9   :   self.patch_disc_files, # not supported -penta3
            10  :   self.restore_disc_files, # not supported -penta3
            11  :   self.replace_textures, # not supported -penta3
            12  :   self.redux.restore_textures, #not supported -penta3
            13  :   self.redux.start_emulation, #not supported -penta3
            14  :   self.nops.hot_reload, # not supported -penta3
            15  :   self.nops.restore, # not supported -penta3
            'show_paths': self.debug,
            'make_disasm': self.disasm,
            'export_texturesc': export_as_c,
            'psx_exit': self.shutdown,
            
        }
        self.num_options = len(self.actions)
        """
        USE PORTABLE PYTHON PATH
        """
        self.python = PYTHON_PORTABLE # unused ??


    def shutdown(self):
        logger.info("EXITING")
        sys.exit(0)

    def update_title(self):
        """ TODO: Identify these commands """
        os.system("title " + self.window_title)

    def get_options(self) -> int:
        intro_msg = """
        BUILDLIST WAS SELECTED:

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
        
        
        v0.2 by penta3
        Choose button to start.
        """
        while True:
            user_input = input(intro_msg + "\n: ").strip().lower()

                
            if len(user_input.split()) == 2:
                command, state = user_input.split()
        
                
                if command in Syms.VERSION_COMMANDS and state == "true":
                    
                    for cmd in Syms.VERSION_STATES:
                        Syms.VERSION_STATES[cmd] = False
        
                    
                    Syms.VERSION_STATES[command] = True
                    print(f"Updated rom version: {command} is now selected.")
                else:
                    print(f"ERROR: Invalid command or state '{user_input}'. Please try again.")
                continue  

            
            if user_input in self.actions:
               
                self.actions[user_input]()
            elif user_input == "quit":
                print("Exiting program.")
                break
            else:
                print(f"ERROR: Invalid command '{user_input}'. Please try again.")


    def abort_compilation(self, is_warning: bool) -> None:
        if is_warning:
            logger.warning("Aborting ongoing compilations.")
            print(f"ERROR: Compilation Aborted")

    def compile(self) -> None:
        if not _files.check_file(COMPILE_LIST):
            return
        instance_symbols = Syms()
        make = Makefile(instance_symbols.get_build_id(), instance_symbols.get_files())
        # parsing compile list
        free_sections()
        with open(COMPILE_LIST, "r") as file:
            for line in file:
                cl = CompileList(line, instance_symbols)
                if not cl.should_ignore():
                    make.add_cl(cl)
        if print_errors[0]:
            compile_error = "ERROR: Compilation failed, check debug files"
            print(compile_error)
            self.abort_compilation(is_warning=True)
                
        if make.build_makefile():
            if not make.make():
                self.abort_compilation(is_warning=True)
        else:
            self.abort_compilation(is_warning=True)

    def clean_files(self) -> None:
        cli_clear()
        _files.delete_directory(DEBUG_FOLDER)
        _files.delete_directory(BACKUP_FOLDER)
        _files.delete_directory(OUTPUT_FOLDER)
        _files.delete_directory(TEXTURES_OUTPUT_FOLDER)
        clean_pch()
        for file in COMPILATION_RESIDUES:
            _files.delete_file(file)
        leftovers = glob.glob(f"{COMPILE_FOLDER}/**/*.o", recursive=True) + glob.glob(f"{COMPILE_FOLDER}/**/*.dep", recursive=True)
        for leftover in leftovers:
            _files.delete_file(leftover)


    def patch_disc_files(self) -> None:
        self.redux.patch_disc_files(restore_files=False)

    def restore_disc_files(self) -> None:
        intro_msg = """
        WARNING: Make sure you're running the original ISO.
        Would you like to restore patched files to the original ones?
        1 - Yes
        2 - No
        """
        error_msg = "ERROR: Invalid input. Please enter 1 for Yes or 2 for No."
        willRestore = request_user_input(first_option=1, last_option=2, intro_msg=intro_msg, error_msg=error_msg) == 1
        if (willRestore):
            self.redux.patch_disc_files(restore_files=willRestore)

    def replace_textures(self) -> None:
        _files.create_directory(TEXTURES_OUTPUT_FOLDER)
        img_count = create_images(TEXTURES_FOLDER)
        if img_count == 0:
            logger.warning("0 images found. No textures were replaced")
            return
        dump_images(TEXTURES_OUTPUT_FOLDER)
        dump_cluts(TEXTURES_OUTPUT_FOLDER)
        self.redux.replace_textures()
        clear_images()
        clear_cluts()

    def disasm(self) -> None:
        path_in = os.path.abspath(GCC_ELF_FILE)
        path_out = os.path.join(DEBUG_FOLDER, "disasm.txt")
        with open(path_out, "w") as file:
            mips_disasm = os.path.join(MIPS_PATH, "mipsel-none-elf-objdump")
            command = [mips_disasm, "-d", path_in]
            subprocess.call(command, stdout=file, stderr=subprocess.STDOUT)
        logger.info(f"Disassembly saved at {path_out}")

    def exec(self):
        while not _files.check_files([COMPILE_LIST, DISC_PATH, SETTINGS_PATH]):
         print(f"ERROR: Cant find buildList.txt, disc.json or settings.json.")
        game_options.gameoptions.load_config()
        while True:
            i = self.get_options()
            self.actions[i]()

if __name__ == "__main__":
    try:
        main = Main()
        main.exec()
    except Exception as e:
        logging.basicConfig(filename=LOG_FILE, filemode="w", format='%(levelname)s:%(message)s')
        logging.exception(e)