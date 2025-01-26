from __future__ import annotations # to use type in python 3.7

from common import DIR_SYMBOLS, CONFIG_PATH, NAME_ROM

import json


class GameVersion:
    """
    Acts like a struct
    """
    def __init__(self, version: str, rom_name: str, files_symbols: list[str], build_id: str):
        self.version = version
        self.rom_name = rom_name
        self.files_symbols = files_symbols
        self.build_id = build_id


class GameOptions:
    def __init__(self) -> None:
        self.versions_by_name = dict()
        self.versions_by_build_id = dict()
        # hardcoded but doesn't change functionality
        self.path_config = CONFIG_PATH
        self.path_dir_symbols = DIR_SYMBOLS

    def load_config(self):
        """
        TODO: Just pass in the data directly to avoid trouble
        TODO: Just pass in the paths directly to avoid more trouble
        TODO: Simplify the config.json structure to avoid this mess
        """
        with open(self.path_config) as file:
            data = json.load(file)
            versions = data["versions"]
            for ver in versions:
                version = list(ver.keys())[0]
                ver_contents = ver[version]
                rom_name = NAME_ROM
                files_symbols = ver_contents["symbols"]
                for i in range(len(files_symbols)):
                    files_symbols[i] = self.path_dir_symbols / files_symbols[i]
                build_id = ver_contents["build_id"]
                gv = GameVersion(version, rom_name, files_symbols, build_id)
                self.versions_by_name[version] = gv
                self.versions_by_build_id[build_id] = gv

    def get_version_names(self) -> list[str]:
        names = list()
        for version in self.versions_by_name.keys():
            names.append(version)
        return names

    def get_gv_by_name(self, name: str) -> GameVersion:
        if name in self.versions_by_name:
            return self.versions_by_name[name]
        return None

    def get_gv_by_build_id(self, build_id: int) -> GameVersion:
        if build_id in self.versions_by_build_id:
            return self.versions_by_build_id[build_id]
        return None

game_options = GameOptions() # why is this initialized here? #are you ubmonster?XD