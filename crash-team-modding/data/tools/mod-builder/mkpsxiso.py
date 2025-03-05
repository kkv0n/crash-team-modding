"""
TO DO: RESTORE GAME_OPTIONS --PENTA3
"""
# im not going to modify this just to use ctr-tools rebuild functions, who cares

import _files # check_file, delete_file, create_directory, delete_directory
import common
from common import ISO_PATH, MOD_NAME, OUTPUT_FOLDER, COMPILE_LIST , FILE_LIST , MOD_DIR, PLUGIN_PATH, request_user_input, get_build_id, COMPILE_FOLDER, NAME_ROM, PATHS_FILE, cli_clear

from disc import Disc
from compile_list import CompileList, free_sections
from syms import Syms
from pathlib import Path

import importlib
import logging
import os
import pathlib
import pdb
import pyxdelta
import pymkpsxiso
import shutil
import sys
import xml.etree.ElementTree as et
import game_options
logger = logging.getLogger(__name__)

MB = 1024 * 1024

def _copyfileobj_patched(fsrc, fdst, length=64*MB):
    """Patches shutil method to hugely improve copy speed"""
    while True:
        buf = fsrc.read(length)
        if not buf:
            break
        fdst.write(buf)

shutil.copyfileobj = _copyfileobj_patched # overwrites a class method directly (dangerous)

class Mkpsxiso:
    def __init__(self) -> None:
        path = os.path.join(PLUGIN_PATH, "plugin.py")
        spec = importlib.util.spec_from_file_location("plugin", path)
        self.plugin = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.plugin)

        
    def find_iso(self, instance_version) -> bool:
        if not _files.check_file(os.path.join(ISO_PATH, NAME_ROM)):
            print(f"Please insert your {instance_version.version} game in {ISO_PATH} and rename it to {NAME_ROM}")
            return False
        return True

    def ask_user_for_version(self):
        names = game_options.gameoptions.get_version_names()
        for version, state in Syms.VERSION_STATES.items():
            if state:
                
                version_number = Syms.VERSION_COMMANDS.get(version)
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

    def extract_iso_to_xml(self, instance_version, dir_out, fname_out: str) -> None:
        """
        NOTE: We're converting some of the pathlibs to strings
            because we don't know if the pymkpsxiso or self.plugin support pathlib yet

        """
        rom_path = os.path.join(ISO_PATH, NAME_ROM)
        _files.create_directory(dir_out)
        # TODO: Find out if the plugin and pymk... support pathlib
        pymkpsxiso.dump(rom_path, str(dir_out), str(fname_out))
        self.plugin.extract(f"{Path(PLUGIN_PATH)}{os.sep}", f"{Path(dir_out)}{os.sep}", f"{instance_version.version}")

    def abort_build_request(self) -> bool:
        iso_error = """
        ERROR:
        ISO BUILD ABORTED,
        CHECK YOUR ROM PATH
        """
        
        print(iso_error)
        
        return True

    def patch_iso(self, version: str, build_id: int, dir_in_build, modified_rom_name: str, fname_xml: str) -> bool:
        """
        dir_in_build and xml are paths
        TODO: Refactor this since it's doing way too much
        """
        disc = Disc(version)
        sym = Syms(build_id)
        modded_files = dict()
        iso_changed = False
        xml_tree = et.parse(fname_xml)
        dir_tree = xml_tree.findall(".//directory_tree")[0]
        build_lists = [COMPILE_FOLDER] # cwd
        while build_lists:
            prefix = build_lists.pop(0)
            bl = (os.path.join(prefix, COMPILE_LIST)) # NOT SURE ABOUT THIS, CAN BE WRONG!
            free_sections()
            with open(bl, "r") as file:
                for line in file:
                    instance_cl = CompileList(line, sym, prefix=COMPILE_FOLDER)
                    if not instance_cl.should_build():
                        continue

                    # if it's a file to be overwritten in the game
                    df = disc.get_df(instance_cl.game_file)
                    if df is not None:
                        # checking file start boundaries
                        if instance_cl.address < df.address:
                            error_msg = f"""
                            [ISO-py] ERROR: Cannot overwrite {df.physical_file}
                            Base address {hex(df.address)} is bigger than the requested address {hex(instance_cl.address)}
                            At line: {instance_cl.original_line}
                            """
                            print(error_msg)
                            if self.abort_build_request():
                                return False
                            continue

                        # checking whether the original file exists and retrieving its size
                        game_file = os.path.join(dir_in_build, df.physical_file)
                        if not _files.check_file(game_file):
                            if self.abort_build_request():
                                return False
                            continue
                        game_file_size = os.path.getsize(game_file)

                        # checking whether the modded file exists and retrieving its size
                        mod_file = instance_cl.get_output_name()
                        if not _files.check_file(mod_file):
                            if self.abort_build_request():
                                return False
                            continue
                        mod_size = os.path.getsize(mod_file)

                        # Checking potential file size overflows and warning the user about them
                        offset = instance_cl.address - df.address + df.offset
                        if (mod_size + offset) > game_file_size:
                            logger.warning(f"{mod_file} will increase total file size of {game_file}\n")

                        mod_data = bytearray()
                        with open(mod_file, "rb") as mod:
                            mod_data = bytearray(mod.read())
                        if game_file not in modded_files:
                            modified_game_file = os.path.join(dir_in_build, df.physical_file)
                            modded_stream = open(modified_game_file, "r+b") # BUG: Should this be closed?
                            modded_files[game_file] = [modded_stream, bytearray(modded_stream.read())]
                            iso_changed = True

                        modded_stream = modded_files[game_file][0]
                        modded_buffer = modded_files[game_file][1]
                        # Add zeroes if the new total file size is more than the original file size
                        for i in range(len(modded_buffer), offset + mod_size):
                            modded_buffer.append(0)
                        for i in range(mod_size):
                            modded_buffer[i + offset] = mod_data[i]

                    # if it's not a file to be overwritten in the game
                    # assume it's a new file to be inserted in the disc
                    else:
                        filename = (instance_cl.section_name + ".bin").upper()
                        filename_len = len(filename)
                        if filename_len > 12:
                            filename = filename[(filename_len - 12):] # truncate
                        mod_file = os.path.join(OUTPUT_FOLDER, instance_cl.section_name + ".bin")
                        dst = os.path.join(dir_in_build, filename)
                        shutil.copyfile(mod_file, dst)
                        contents = {
                            "name": filename,
                            "source": os.path.join(modified_rom_name, filename),
                            "type": "data"
                        }
                        element = et.Element("file", contents)
                        dir_tree.insert(-1, element)
                        iso_changed = True

                # writing changes to files we overwrote
                for game_file in modded_files:
                    modded_stream = modded_files[game_file][0]
                    modded_buffer = modded_files[game_file][1]
                    modded_stream.seek(0)
                    modded_stream.write(modded_buffer)
                    modded_stream.close()
                if iso_changed:
                    xml_tree.write(fname_xml)

        return iso_changed

    def convert_xml(self, fname, fname_out, modified_rom_name: str,fextra: list[str]) -> None:
        xml_tree = et.parse(fname) # filename

        if fextra:
            for element in xml_tree.iter("directory_tree"):
                for srcName in fextra:
                    new_element = et.Element('file')
                    discName = srcName.split('/')[-1]
                    new_element.set('name',discName)
                    new_element.set('source',os.path.join(modified_rom_name, discName))
                    new_element.set('type','data')
                    new_element.tail = '\n\t\t'
                    element.append(new_element)
                break

        for element in xml_tree.iter():
            key = "source"
            if key in element.attrib:
                element_source = element.attrib[key].split("/")
                element_source[0] = modified_rom_name
                element_source = "/".join(element_source)
                element.attrib[key] = element_source
        xml_tree.write(fname_out)

    def extract_game(self, string1, string2, string3) -> None:
        self.extract_iso_to_xml(string1, string2, string3)
        
        
    def build_iso(self, only_extract=False) -> None:

        instance_version = self.ask_user_for_version()
        last_compiled_version = get_build_id()
        if last_compiled_version is not None and instance_version.build_id != last_compiled_version:
            print(f"WARNING: iso build was requested for version: {instance_version.version} but last compiled version was: {game_options.gameoptions.get_gv_by_build_id(last_compiled_version.version).version}. ")
            print(f"This could mean that some output files may contain data for the wrong version, resulting in a corrupted disc.")
            if self.abort_build_request():
                return
        rom_name = Path(NAME_ROM).stem
        extract_folder = os.path.join(ISO_PATH, rom_name)
        xml = f"{extract_folder}.xml"
        self.extract_game(instance_version, extract_folder, xml)
        if only_extract or not _files.check_file(COMPILE_LIST):
            return

           
        modified_rom_name = f"{rom_name}_{MOD_NAME}"
        build_files_folder = os.path.join(ISO_PATH, modified_rom_name)
        new_xml = f"{build_files_folder}.xml"
        _files.delete_directory(build_files_folder)
        print(f"Copying files.")
        shutil.copytree(extract_folder, build_files_folder)

        extraFiles = []

        #check for optional fileList.txt
        if os.path.exists(os.path.join(MOD_DIR, FILE_LIST)):
            logger.info("Adding files from fileList.txt ...")
            with open(os.path.join(MOD_DIR, FILE_LIST, 'r')) as file:
                lines = file.readlines()
            for line in lines:
                cleaned_line = line.strip().replace(' ', '').replace('\t','')
                if "//" in cleaned_line:
                    continue
                words = cleaned_line.split(',')
                if words[0] == instance_version.version:
                    srcFile = words[1]

                    if len(words) < 3:
                        extraFiles.append(srcFile)
                        shutil.copyfile(os.path.join(MOD_DIR, srcFile, build_files_folder, words[1].split('/')[-1]))
                    else:
                        shutil.copyfile(os.path.join(MOD_DIR, srcFile, build_files_folder, words[2]))
        

        print(f"Converting XML...")
        self.convert_xml(xml, new_xml, modified_rom_name,extraFiles)
        build_bin = f"{build_files_folder}.bin"
        build_cue = f"{build_files_folder}.cue"
        logger.info("Patching files...")
        if self.patch_iso(instance_version.version, instance_version.build_id, build_files_folder, modified_rom_name, new_xml):
            print(f"Building iso...")
            self.plugin.build(f"{str(PLUGIN_PATH)}{os.sep}", f"{str(build_files_folder)}{os.sep}", f"{instance_version.version}")
            pymkpsxiso.make(str(build_bin), str(build_cue), str(new_xml))
            print(f"Build completed.")
        else:
            logger.warning("No files changed. ISO building skipped.")
            print(f"Error: No files changed, ISO building was skipped")

    def xdelta(self) -> None:
        cli_clear()
        instance_version = self.ask_user_for_version()
        original_game = os.path.join(ISO_PATH, NAME_ROM)
        rom_name = Path(NAME_ROM).stem
        mod_name = f"{rom_name}_{MOD_NAME}"
        modded_game = os.path.join(ISO_PATH, f"{mod_name}.bin")
        if not _files.check_file(original_game):
            print(f"Make sure your vanilla rom is in {ISO_PATH}.")
            return
        if not _files.check_file(modded_game):
            print(f"Make sure you builded your modded rom before trying to generate a xdelta patch.")
            return
        print(f"Generating xdelta patch...")
        output = os.path.join(ISO_PATH, f"{mod_name}.xdelta")
        pyxdelta.run(str(original_game), str(modded_game), str(output))
        print(f"{output} generated!")

    def clean(self) -> None:    
        cli_clear()        
        for version in game_options.gameoptions.get_version_names():
            instance_version = game_options.gameoptions.get_gv_by_name(version)
            rom_name = Path(NAME_ROM).stem
            modified_rom_name = f"{rom_name}_{MOD_NAME}"
            build_files_folder = os.path.join(ISO_PATH, modified_rom_name)
            build_cue = f"{build_files_folder}.cue"
            build_bin = f"{build_files_folder}.bin"
            build_xml = f"{build_files_folder}.xml"
            build_xdelta = f"{build_files_folder}.xdelta"

            extract_xml = f"{rom_name}.xml"
            extract_folder = os.path.join(ISO_PATH, rom_name)
            _files.delete_directory(build_files_folder)
            _files.delete_file(build_bin)
            _files.delete_file(build_cue)
            _files.delete_file(build_xml)
            _files.delete_file(build_xdelta)

    def extract_iso(self) -> None:
        self.build_iso(only_extract=True)
