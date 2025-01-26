from common import TEXTURES_FOLDER
from image import create_images, clear_images, get_image_list
from clut import clear_cluts, get_clut_list

import logging
import textwrap

logger = logging.getLogger(__name__)

RECT_SIZE = 8
CHAR_SIZE = 1
SHORT_SIZE = 2

img_names = []
clut_names = []

def construct_header() -> str:
    """
    TODO: Verify this works
    """
    buffer = """
    typedef struct
    {
        short x, y, w, h;
    } RECT;

    typedef struct
    {
        char * image;
        RECT * pos;
    } Texture;

    typedef struct
    {
        short * clut;
        RECT * pos;
    } CLUT;

    """
    return textwrap.dedent(buffer) # remove indentation

def export_objects(obj_list, is_img: bool) -> str:
    buffer = str()
    for obj in obj_list:
        buffer += obj.as_c_struct()
        if is_img:
            img_names.append(obj.name)
        else:
            clut_names.append(obj.name)
    return buffer + "\n"

def create_texture_struct() -> str:
    def fill_struct(struct_type: str, struct_name: str, names: list[str]) -> str:
        buffer = struct_type + " " + struct_name + '[] __attribute__ ((section (".sdata"))) = {\n'
        counter = 0
        for name in names:
            buffer += " " * 4 + "[" + str(counter) + "] =\n"
            buffer += " " * 4 + "{\n"
            buffer += " " * 8 + ".image = " + name + ",\n"
            buffer += " " * 8 + ".pos = &" + name + "_pos,\n"
            buffer += " " * 4 + "},\n"
            counter += 1
        buffer += "};\n\n"
        return buffer

    return fill_struct("Texture", "textures", img_names) + fill_struct("CLUT", "cluts", clut_names)

def clear_cache() -> None:
    clear_images()
    clear_cluts()
    img_names.clear()
    clut_names.clear()

def export_as_c() -> None:
    img_count = create_images(TEXTURES_FOLDER)
    if img_count == 0:
        print(f"0 images found. No textures were exported")
        return
    buffer = construct_header()
    buffer += export_objects(get_image_list(), True)
    buffer += export_objects(get_clut_list(), False)
    buffer += create_texture_struct()
    path_out = TEXTURES_FOLDER / "newtex.c"
    with open(path_out, "w") as file:
        file.write(buffer)
    print(f"Textures successfully exported in {path_out}")
    clear_cache()