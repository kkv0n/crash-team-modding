#TO DO: MERGE ALL THE UNNECESARY STRINGS
"""
Contains all of the global directory names and functions for user input
TODO: Make the user pass in the game dir
"""
import copy
import logging
import os
import pathlib
import sys
import textwrap
import _files
from pathlib import Path
import importlib



PATHS_FILE = "paths.txt"
ISO_PATH = None
COMPILE_LIST = None
MOD_NAME = None
MOD_DIR = None
GAME_NAME = None
PSX_DIR = None
NAME_ROM = None

# LOAD PATHS FROM TXT FILE
def load_portable_paths(file_path):
    """Load paths from a text file."""
    global ISO_PATH, COMPILE_LIST, MOD_NAME, MOD_DIR, GAME_NAME, PSX_DIR, NAME_ROM
    try:
        with open(file_path, "r") as file:
            for line in file:
                line = line.strip()
                if "=" in line:  
                    key, value = line.split("=", 1)
                    key = key.strip()
                    value = value.strip().strip('"')  # delete (")
                    if key == "ISO_PATH":
                        ISO_PATH = pathlib.Path(value)
                    elif key == "COMPILE_LIST":
                        COMPILE_LIST = os.path.abspath(value)
                    elif key == "MOD_NAME":
                        MOD_NAME = value
                    elif key == "MOD_DIR":
                        MOD_DIR = os.path.abspath(value)
                    elif key == "GAME_NAME":
                        GAME_NAME = value
                    elif key == "PSX_DIR":
                        PSX_DIR = os.path.abspath(value)
                    elif key == "NAME_ROM":
                        NAME_ROM = value
    except FileNotFoundError:
        print(f"Error: the file '{file_path}' dont exist.")
    except Exception as e:
        print(f"Error loading paths: {e}")
        
        
# call to load paths from .txt
load_portable_paths(PATHS_FILE)

COMPILE_FOLDER = Path(os.path.abspath(COMPILE_LIST)).parent
MAKEFILE = os.path.join(COMPILE_FOLDER, "Makefile")

logging.basicConfig(level = logging.DEBUG)
logger = logging.getLogger(__name__)

# FIND SDK FOLDER
def find_main_folder(fname, start_folder):

    current_folder = pathlib.Path(start_folder).resolve()
    while current_folder != current_folder.parent:  
        file_path = os.path.join(current_folder, fname)
        if os.path.exists(file_path):
            return current_folder  
        current_folder = current_folder.parent
    return None

        
def extract_build_id(list_tokens):
    """
    Assumes -DBUILD=value
    TODO: Is the build id always from DBUILD?
    """
    for token in list_tokens:
        if "DBUILD" in token.upper():
            return int(token.split("=")[-1].strip())

    return None

def get_build_id(fname = MAKEFILE) -> int:
    """
    Assumes only one set of CPPFLAGS
    """
    path_file = pathlib.Path(fname)
    if not path_file.exists():
        logger.debug(f"Makefile not found {path_file}")
        return None
    with open(path_file, "r") as file:
        for line in file:
            list_tokens = line.split()
            if len(list_tokens) and list_tokens[0] == "CPPFLAGS":
                return extract_build_id(list_tokens[1:])

remaining_args = copy.deepcopy(sys.argv[1:])
using_cl_args = len(sys.argv) > 1



sys.platform == "win32"
"""
FILE PATHS
"""
LOG_FILE = "crash.log"
CONFIG_FILE = "config.json"
DIR_GAME = find_main_folder(CONFIG_FILE, COMPILE_FOLDER)


CONFIG_PATH = os.path.join(DIR_GAME, CONFIG_FILE)
logger.debug(f"FOLDER_DISTANCE: {DIR_GAME}")
logger.debug(f"CWD: {pathlib.Path.cwd()}")
DISTANCE_LENGTH = str(DIR_GAME).count("/") + 1



"""
UNUSED
"""
SLASH = os.sep
"""
UNUSED
"""




DIR_SYMBOLS = os.path.join(DIR_GAME, "symbols")
PLUGIN_PATH = os.path.join(DIR_GAME, "plugins")
GAME_INCLUDE_PATH = os.path.join(DIR_GAME, "include")
OVERLAYLD = os.path.join(COMPILE_FOLDER, "overlay.ld")
FILE_LIST = "fileList.txt"
SRC_FOLDER = "src/"
OUTPUT_FOLDER = os.path.join(COMPILE_FOLDER, "output")
BACKUP_FOLDER = os.path.join(COMPILE_FOLDER, "backup")
DEBUG_FOLDER = os.path.join(COMPILE_FOLDER, "debug") 
OBJ_FOLDER = os.path.join(DEBUG_FOLDER, "obj", "")
DEP_FOLDER = os.path.join(DEBUG_FOLDER, "dep", "")
COMP_SOURCE = os.path.join(DEBUG_FOLDER, "source.txt")
TEXTURES_FOLDER = pathlib.Path("newtex")
TEXTURES_OUTPUT_FOLDER = os.path.join(TEXTURES_FOLDER, "output")
MAP_INIT_PATH = os.path.join(COMPILE_FOLDER, "mod.map")
ELF_INIT_PATH = os.path.join(COMPILE_FOLDER, "mod.elf")
GCC_MAP_FILE = os.path.join(DEBUG_FOLDER, "mod.map")
GCC_ELF_FILE = os.path.join(DEBUG_FOLDER, "mod.elf")
GCC_OUT_FILE = os.path.join(DEBUG_FOLDER, "gcc_out.txt")
TRIMBIN_OFFSET = os.path.join(DEBUG_FOLDER, "offset.txt")
COMPILATION_RESIDUES = [OVERLAYLD, MAKEFILE, "comport.txt"]
REDUX_MAP_FILE = os.path.join(DEBUG_FOLDER, "redux.map")
SETTINGS_FILE = os.path.join(PSX_DIR, "data", "settings.json")
SETTINGS_PATH = os.path.abspath(SETTINGS_FILE)
DISC_FILE = "disc.json"
DISC_PATH = os.path.join(DIR_GAME, DISC_FILE)
TOOLS_PATH = os.path.join(PSX_DIR, "data", "tools")
MIPS_PATH = os.path.join(TOOLS_PATH, "mips", "bin")
PYTHON_PORTABLE = os.path.join(TOOLS_PATH, "Python", "Python310", "python.exe")
COMMENT_SYMBOL = "//"   

HEXDIGITS = ["A", "B", "C", "D", "E", "F"]

def show_paths():
    """
    DEBUG LOGS WHEN YOU
    OPEN THE SCRIPT, NEEDED
    TO VERIFY IF THE PATHS
    ARE CORRECT
    """     
    logger.debug(f"DIR_GAME: {DIR_GAME}")
    logger.debug(f"MOD_DIR: {MOD_DIR}")
    logger.debug(f"MOD_NAME: {MOD_NAME}")
    logger.debug(f"COMPILATION_RESIDUES: {COMPILATION_RESIDUES}")
    logger.debug(f"GCC_MAP_FILE: {GCC_MAP_FILE}")
    logger.debug(f"CONFIG_PATH: {CONFIG_PATH}")
    logger.debug(f"DEBUG_FOLDER: {DEBUG_FOLDER}")
    logger.debug(f"GAME_NAME: {GAME_NAME}")
    logger.debug(f"MIPS_PATH: {MIPS_PATH}")
    logger.debug(f"COMPILE_LIST: {COMPILE_LIST}")
    logger.debug(f"COMPILE_FOLDER: {COMPILE_FOLDER}")
    logger.debug(f"TRIMBIN_OFFSET: {TRIMBIN_OFFSET}")
    logger.debug(f"SETTINGS_PATH: {SETTINGS_PATH}")
    logger.debug(f"PSX_DIR: {PSX_DIR}")
    logger.debug(f"OBJ_FOLDER: {OBJ_FOLDER}")

def request_user_input(first_option: int, last_option: int, intro_msg: str, error_msg: str) -> int:
    """
    TODO: Convert this to click
    """
    if using_cl_args and len(remaining_args) == 0:
        raise Exception("ERROR: Not enough arguments to complete command.")

    if not using_cl_args:
        print(textwrap.dedent(intro_msg))

    raise_exception = False
    i = 0
    while True:
        try:
            i = int(input()) if not using_cl_args else int(remaining_args.pop(0))
            if (i < first_option) or (i > last_option):
                if using_cl_args:
                    raise_exception = True
                    break
                else:
                    print(textwrap.dedent(error_msg))
            else:
                break
        except:
            if using_cl_args:
                raise_exception = True
                break
            else:
                print(textwrap.dedent(error_msg))

    if raise_exception:
        raise Exception(textwrap.dedent(error_msg))

    return i

def is_number(s: str) -> bool:
    is_hex = False
    if len(s) > 1 and s[0] == "-":
        s = s[1:]
    if len(s) > 2 and s[:2] == "0x":
        s = s[2:]
        is_hex = True
    if len(s) == 0:
        return False
    for char in s:
        if not ((char.isdigit()) or (is_hex and char.upper() in HEXDIGITS)):
            return False
    return True

def cli_clear() -> None:
    if os.name == "nt":
        os.system("cls")
    else:
        os.system("clear")
