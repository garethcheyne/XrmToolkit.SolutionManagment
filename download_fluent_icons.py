"""
Download Microsoft Fluent UI System Icons and convert SVG to PNG.
Icons are from: https://github.com/microsoft/fluentui-system-icons (MIT License)

Colours match the toolbar function:
  Blue   #3478F6  — list/refresh/download/export/import/search
  Green  #34C759  — play/start/add/success
  Red    #EB4444  — cancel/error/delete/remove/deactivate
  Orange #FF9F0A  — settings/config
  Teal   #5AC8FA  — swap/switch
  Purple #AF52DE  — info/about
"""
import os
import re
import urllib.request
import cairosvg

BASE_URL = "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/assets"
OUTPUT_DIR = os.path.join("DamSim.SolutionTransferTool", "Resources", "FluentIcons")
os.makedirs(OUTPUT_DIR, exist_ok=True)

STYLE = "filled"

# Colour palette
BLUE   = "#3478F6"
GREEN  = "#34C759"
RED    = "#EB4444"
ORANGE = "#FF9F0A"
TEAL   = "#5AC8FA"
PURPLE = "#AF52DE"

# Mapping: (output_filename, fluent_dir_name, fluent_snake_name, svg_size, png_size, colour)
ICONS = [
    # --- Toolbar icons (32x32) ---
    ("Solutions32.png",      "Apps List",        "apps_list",        32, 32, BLUE),
    ("Startup32.png",        "Play",             "play",             32, 32, GREEN),
    ("Connect32.png",        "Settings",         "settings",         32, 32, ORANGE),
    ("Error32.png",          "Dismiss Circle",   "dismiss_circle",   32, 32, RED),
    ("Refresh32.png",        "Arrow Sync",       "arrow_sync",       24, 32, BLUE),
    ("download1.png",        "Arrow Download",   "arrow_download",   32, 32, BLUE),
    ("arrow_switch.png",     "Arrow Swap",       "arrow_swap",       24, 32, TEAL),
    ("download.png",         "Arrow Upload",     "arrow_upload",     32, 32, BLUE),
    ("inbox_download.png",   "Folder Open",      "folder_open",      24, 32, BLUE),
    ("icons8_cancel.png",    "Subtract Circle",  "subtract_circle",  32, 32, RED),
    ("icon.png",             "Info",             "info",             32, 32, PURPLE),
    ("plus.png",             "Add Circle",       "add_circle",       32, 32, GREEN),
    ("delete.png",           "Delete",           "delete",           32, 32, RED),
    ("Search32.png",         "Search",           "search",           32, 32, BLUE),
    ("PauseCircle32.png",    "Pause Circle",     "pause_circle",     32, 32, RED),

    # --- Status icons (64x64) ---
    ("Success64.png",        "Checkmark Circle", "checkmark_circle", 48, 64, GREEN),
    ("Error64.png",          "Error Circle",     "error_circle",     48, 64, RED),
]


def colorize_svg(svg_bytes: bytes, hex_colour: str) -> bytes:
    """Replace the default Fluent fill (#212121) with the target colour."""
    svg_text = svg_bytes.decode("utf-8")
    # Replace fill attributes and style fills
    svg_text = re.sub(r'fill="#212121"', f'fill="{hex_colour}"', svg_text, flags=re.IGNORECASE)
    svg_text = re.sub(r'fill:#212121', f'fill:{hex_colour}', svg_text, flags=re.IGNORECASE)
    return svg_text.encode("utf-8")


print(f"Downloading {len(ICONS)} Fluent UI icons (coloured)...\n")

success = 0
failed = 0

for output_name, dir_name, snake_name, svg_size, png_size, colour in ICONS:
    url = f"{BASE_URL}/{dir_name.replace(' ', '%20')}/SVG/ic_fluent_{snake_name}_{svg_size}_{STYLE}.svg"
    output_path = os.path.join(OUTPUT_DIR, output_name)

    print(f"  {output_name:25s} <- {dir_name} ({svg_size}px) colour={colour}")

    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req) as resp:
            svg_data = resp.read()

        # Recolour the SVG before conversion
        svg_data = colorize_svg(svg_data, colour)

        cairosvg.svg2png(
            bytestring=svg_data,
            write_to=output_path,
            output_width=png_size,
            output_height=png_size,
        )
        success += 1
        print(f"    -> OK ({png_size}x{png_size})")

    except Exception as e:
        failed += 1
        print(f"    -> FAILED: {e}")

print(f"\nDone: {success} succeeded, {failed} failed")
print(f"Output directory: {os.path.abspath(OUTPUT_DIR)}")
