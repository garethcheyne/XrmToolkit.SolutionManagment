"""Generate a modern flat-design icon suite for the toolbar."""
from PIL import Image, ImageDraw, ImageFont
import math
import os

OUT = r"c:\Apps\Projects\XrmToolBox\Solutions\DamSim.SolutionTransferTool\err403.SolutionManagment\Resources\NewGenIcons"
os.makedirs(OUT, exist_ok=True)

SIZE = 32
# Colour palette — modern flat colours
BLUE     = (52, 120, 246)    # primary action
BLUE_DK  = (30, 90, 210)
GREEN    = (52, 199, 89)     # success / activate
GREEN_DK = (36, 160, 70)
RED      = (235, 68, 68)     # cancel / error / deactivate
RED_DK   = (190, 50, 50)
ORANGE   = (255, 159, 10)    # warning / one-time
GREY     = (142, 142, 147)   # neutral
WHITE    = (255, 255, 255)
DARK     = (44, 44, 46)
TEAL     = (90, 200, 250)
PURPLE   = (175, 82, 222)


def new(bg=None):
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    return img, draw


def circle(draw, cx, cy, r, fill):
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill)


def rounded_rect(draw, x0, y0, x1, y1, r, fill):
    draw.rounded_rectangle([x0, y0, x1, y1], radius=r, fill=fill)


def save(img, name):
    path = os.path.join(OUT, name)
    img.save(path, "PNG")
    print(f"  ✓ {name}")


# ─── 1. Solutions32 — Load/Refresh solutions (list icon) ───
def gen_solutions():
    img, d = new()
    # Three horizontal bars with a dot
    rounded_rect(d, 4, 6, 28, 12, 2, BLUE)
    rounded_rect(d, 4, 14, 28, 20, 2, BLUE)
    rounded_rect(d, 4, 22, 28, 28, 2, BLUE)
    # Small white dots on each bar
    circle(d, 8, 9, 1.5, WHITE)
    circle(d, 8, 17, 1.5, WHITE)
    circle(d, 8, 25, 1.5, WHITE)
    save(img, "Solutions32.png")


# ─── 2. Startup32 — Transfer / run (play arrow) ───
def gen_startup():
    img, d = new()
    # Circle background
    circle(d, 16, 16, 14, GREEN)
    # Play triangle
    d.polygon([(12, 8), (12, 24), (25, 16)], fill=WHITE)
    save(img, "Startup32.png")


# ─── 3. Error32 — Cancel (red circle X) ───
def gen_error():
    img, d = new()
    circle(d, 16, 16, 14, RED)
    lw = 3
    d.line([(10, 10), (22, 22)], fill=WHITE, width=lw)
    d.line([(22, 10), (10, 22)], fill=WHITE, width=lw)
    save(img, "Error32.png")


# ─── 4. Connect32 — One-time settings / dependencies (gear+lightning) ───
def gen_connect():
    img, d = new()
    # Gear circle
    circle(d, 16, 16, 12, ORANGE)
    # Inner circle
    circle(d, 16, 16, 6, WHITE)
    # Gear teeth (small rectangles at compass points)
    for angle in range(0, 360, 45):
        rad = math.radians(angle)
        cx = 16 + 12 * math.cos(rad)
        cy = 16 + 12 * math.sin(rad)
        circle(d, cx, cy, 3, ORANGE)
    # Lightning bolt in center
    d.polygon([(17, 7), (13, 17), (16, 17), (14, 26), (20, 15), (17, 15)], fill=DARK)
    save(img, "Connect32.png")


# ─── 5. download1 — Download solution to disk (tray + arrow) ───
def gen_download1():
    img, d = new()
    # Tray
    rounded_rect(d, 3, 22, 29, 28, 3, BLUE)
    # Down arrow shaft
    d.rectangle([14, 4, 18, 18], fill=BLUE)
    # Arrow head
    d.polygon([(9, 16), (16, 24), (23, 16)], fill=BLUE)
    # Slot in tray
    d.rectangle([14, 24, 18, 26], fill=WHITE)
    save(img, "download1.png")


# ─── 6. download — Export solutions (tray arrow up) ───
def gen_download():
    img, d = new()
    # Tray
    rounded_rect(d, 3, 22, 29, 28, 3, BLUE_DK)
    # Up arrow shaft
    d.rectangle([14, 10, 18, 24], fill=BLUE_DK)
    # Arrow head pointing UP
    d.polygon([(9, 13), (16, 4), (23, 13)], fill=BLUE_DK)
    save(img, "download.png")


# ─── 7. icons8_cancel — Remove / deactivate (circle minus) ───
def gen_cancel():
    img, d = new()
    circle(d, 16, 16, 14, RED)
    # Horizontal bar (minus)
    rounded_rect(d, 7, 13, 25, 19, 2, WHITE)
    save(img, "icons8_cancel.png")


# ─── 8. arrow_switch — Switch orgs (swap arrows) ───
def gen_switch():
    img, d = new()
    # Left-pointing arrow (top)
    d.polygon([(4, 10), (12, 4), (12, 8)], fill=TEAL)
    d.rectangle([12, 8, 28, 12], fill=TEAL)
    # Right-pointing arrow (bottom)
    d.polygon([(28, 22), (20, 28), (20, 24)], fill=TEAL)
    d.rectangle([4, 20, 20, 24], fill=TEAL)
    save(img, "arrow_switch.png")


# ─── 9. inbox_download — Import from file (folder + arrow in) ───
def gen_inbox():
    img, d = new()
    # Folder shape
    rounded_rect(d, 2, 8, 29, 28, 3, BLUE)
    # Folder tab
    rounded_rect(d, 2, 5, 14, 11, 2, BLUE)
    # Down arrow into folder
    d.rectangle([14, 2, 18, 16], fill=WHITE)
    d.polygon([(10, 14), (16, 22), (22, 14)], fill=WHITE)
    save(img, "inbox_download.png")


# ─── 10. plus — Add target (circle +) ───
def gen_plus():
    img, d = new()
    circle(d, 16, 16, 14, GREEN)
    lw = 3
    d.line([(16, 7), (16, 25)], fill=WHITE, width=lw)
    d.line([(7, 16), (25, 16)], fill=WHITE, width=lw)
    save(img, "plus.png")


# ─── 11. Refresh icon (circular arrow) ───
def gen_refresh():
    img, d = new()
    # Circular arrow using arc
    d.arc([4, 4, 28, 28], start=30, end=330, fill=BLUE, width=4)
    # Arrow head at the end of the arc
    rad = math.radians(330)
    ex = 16 + 12 * math.cos(rad)
    ey = 16 + 12 * math.sin(rad)
    d.polygon([(ex - 1, ey - 6), (ex + 5, ey + 1), (ex - 4, ey + 1)], fill=BLUE)
    # Arrow head at start
    rad2 = math.radians(30)
    sx = 16 + 12 * math.cos(rad2)
    sy = 16 + 12 * math.sin(rad2)
    d.polygon([(sx + 1, sy + 6), (sx - 5, sy - 1), (sx + 4, sy - 1)], fill=BLUE)
    save(img, "Refresh32.png")


# ─── 12. Success64 — Large success checkmark ───
def gen_success64():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([4, 4, 60, 60], fill=GREEN)
    d.line([(18, 32), (28, 44), (46, 20)], fill=WHITE, width=5)
    img.save(os.path.join(OUT, "Success64.png"), "PNG")
    print("  ✓ Success64.png")


# ─── 13. Error64 — Large error X ───
def gen_error64():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([4, 4, 60, 60], fill=RED)
    d.line([(20, 20), (44, 44)], fill=WHITE, width=5)
    d.line([(44, 20), (20, 44)], fill=WHITE, width=5)
    img.save(os.path.join(OUT, "Error64.png"), "PNG")
    print("  ✓ Error64.png")


# ─── 14. progressbar — animated progress ───
def gen_progressbar():
    img, d = new()
    rounded_rect(d, 2, 12, 30, 20, 4, (220, 220, 225))
    rounded_rect(d, 3, 13, 20, 19, 3, BLUE)
    save(img, "progressbar.png")


# ─── 15. About / info icon ───
def gen_about():
    img, d = new()
    circle(d, 16, 16, 14, PURPLE)
    # "i" letter
    circle(d, 16, 9, 2.5, WHITE)
    rounded_rect(d, 13, 14, 19, 26, 2, WHITE)
    save(img, "icon.png")


# ─── 16. Delete / trash ───
def gen_delete():
    img, d = new()
    # Trash can body
    rounded_rect(d, 8, 10, 24, 28, 3, RED)
    # Lid
    rounded_rect(d, 5, 7, 27, 11, 2, RED_DK)
    # Handle
    rounded_rect(d, 13, 4, 19, 8, 2, RED_DK)
    # Lines on can
    d.line([(13, 14), (13, 24)], fill=WHITE, width=1)
    d.line([(16, 14), (16, 24)], fill=WHITE, width=1)
    d.line([(19, 14), (19, 24)], fill=WHITE, width=1)
    save(img, "delete.png")


# ─── Generate all ───
print("Generating NewGenIcons suite...")
gen_solutions()
gen_startup()
gen_error()
gen_connect()
gen_download1()
gen_download()
gen_cancel()
gen_switch()
gen_inbox()
gen_plus()
gen_refresh()
gen_success64()
gen_error64()
gen_progressbar()
gen_about()
gen_delete()
print(f"\nDone — {len(os.listdir(OUT))} icons in {OUT}")
