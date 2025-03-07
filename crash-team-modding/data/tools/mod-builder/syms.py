from __future__ import annotations # to use type in python 3.7

import _files # check_file
from common import request_user_input, is_number, PATHS_FILE
import game_options

import logging

logger = logging.getLogger(__name__)

class Syms():
    VERSION_STATES = {
      'sel_usa': False,
      'sel_pal': False,
      'sel_jap': False,
      'sel_proto': False,
      'sel_japtrial': False
    }
    VERSION_COMMANDS = {
        'sel_usa': 1,
        'sel_pal': 2,
        'sel_jap': 3,
        'sel_proto': 4,
        'sel_japtrial': 5
    }
    def __init__(self, build_id=None) -> None:
        self.version = int()
        self.gv = self.ask_user_for_version(build_id)
        self.syms = dict()
        for file in self.gv.files_symbols:
            self.parse_gcc_file(file)
            

    def ask_user_for_version(self, build_id: int = None):
        
        if build_id is not None:
            return game_options.gameoptions.get_gv_by_build_id(build_id)
        names = game_options.gameoptions.get_version_names()

        
        for version, state in self.VERSION_STATES.items():
            if state:
                
                version_number = self.VERSION_COMMANDS.get(version)
                return game_options.gameoptions.get_gv_by_name(names[version_number - 1])
        
        
        
        try:
            with open(PATHS_FILE, "r") as file:
                for line in file:
                    line = line.strip()
                    if "=" in line:  
                        key, value = line.split("=", 1)
                        key = key.strip()
                        value = value.strip().strip('"')  # delete (")
                        if key == "ROM_REGION":
                            ROM_REGION = int(value)
                            return game_options.gameoptions.get_gv_by_name(names[ROM_REGION - 1])
                            
        except FileNotFoundError:
            print(f"Error: the file '{PATHS_FILE}' dont exist.")
        except Exception as e:
            print(f"Error loading game region: {e}")

    def parse_gcc_file(self, fname: str) -> None:
        """
        TODO: Abstract this out
        """
        if not _files.check_file(fname):
            return
        with open(fname, "r") as file:
            for line in file:
                if line.strip() == "":
                    continue
                original_line = line
                line = [l.strip() for l in line.split("=")]
                if len(line) != 2:
                    logger.error(f"Syntax error in file: {fname} at line {original_line}")
                    continue
                symbol = line[0]
                address = line[1].split(";")[0].strip()
                if not is_number(address):
                    logger.error(f"Invalid address in file: {fname} at line: {original_line}")
                    continue
                address = int(address, 0)
                self.syms[symbol] = address

    def get_files(self) -> list[str]:
        if self.gv is None:
            return None
        return self.gv.files_symbols

    def get_address(self, symbol: str) -> int:
        if symbol in self.syms:
            return self.syms[symbol]
        return None

    def get_version(self) -> str:
        if self.gv is None:
            return None
        return self.gv.version

    def get_build_id(self) -> int:
        if self.gv is None:
            return None
        return self.gv.build_id