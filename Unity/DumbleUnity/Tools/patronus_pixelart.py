"""Genera los spritesheets pixel art de los patronus (8 frames de 120x120 en horizontal).

Uso:  python Tools/patronus_pixelart.py [--out carpeta] [--preview carpeta] [--only a,b,c]
Por defecto escribe en Assets/Resources/PatronusPixelSprites/<animal>_pixel_8f.png

El motor de dibujo está en patronus_core.py y cada especie en patronus_species.py.
"""
import os
import sys

from PIL import Image

from patronus_core import FRAMES, W, render_animal
from patronus_species import ANIMALS


def main():
    here = os.path.dirname(os.path.abspath(__file__))

    def opt(flag, default=None):
        return sys.argv[sys.argv.index(flag) + 1] if flag in sys.argv else default

    out = opt("--out", os.path.join(here, "..", "Assets", "Resources", "PatronusPixelSprites"))
    preview = opt("--preview")
    only = opt("--only")
    only = only.split(",") if only else None
    os.makedirs(out, exist_ok=True)
    for name, fn in ANIMALS.items():
        if only and name not in only:
            continue
        sheet = render_animal(name, fn)
        sheet.save(os.path.join(out, f"{name}_pixel_8f.png"))
        print("ok", name)
        if preview:
            os.makedirs(preview, exist_ok=True)
            bg = (8, 10, 20, 255)
            frames = []
            for i in range(FRAMES):
                f = Image.new("RGBA", (W, W), bg)
                f.alpha_composite(sheet.crop((i * W, 0, (i + 1) * W, W)))
                frames.append(f.resize((W * 3, W * 3), Image.NEAREST).convert("RGB"))
            frames[0].save(os.path.join(preview, f"{name}.gif"), save_all=True, append_images=frames[1:],
                           duration=110, loop=0)
            # 4 frames ampliados x3 para revisar
            strip = Image.new("RGB", (W * 3 * 4, W * 3))
            for k in range(4):
                strip.paste(frames[k * 2], (k * W * 3, 0))
            strip.save(os.path.join(preview, f"{name}.png"))


if __name__ == "__main__":
    main()
