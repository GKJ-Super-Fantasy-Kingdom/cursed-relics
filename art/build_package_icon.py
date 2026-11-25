"""Draw the Cursed Relics package icon on a 64-pixel grid."""

from pathlib import Path

from PIL import Image, ImageDraw


canvas = Image.new("RGB", (64, 64), "#100c17")
draw = ImageDraw.Draw(canvas)

# Copper frame, plum field, and inset crimson corner studs.
draw.rectangle((2, 2, 61, 61), fill="#57364a")
draw.rectangle((3, 3, 60, 60), fill="#a56b50")
draw.line([(3, 59), (3, 3), (60, 3)], fill="#cf926b")
draw.line([(4, 60), (60, 60), (60, 4)], fill="#774b42")
draw.rectangle((5, 5, 58, 58), fill="#352236")
draw.rectangle((7, 7, 56, 56), fill="#22172b")
for x, y in ((6, 6), (54, 6), (6, 54), (54, 54)):
    draw.rectangle((x, y, x + 3, y + 3), fill="#924359")
    draw.point((x + 1, y + 1), fill="#d87080")

# Low-contrast lozenge under the open silhouette of an upright gold crown.
draw.polygon([(29, 9), (52, 32), (29, 55), (7, 32)], fill="#2a1a31")
draw.line([(10, 32), (29, 13), (49, 33)], fill="#3a243d")
draw.ellipse((13, 42, 52, 50), fill="#170f20")
crown = [(10, 18), (14, 18), (21, 27), (26, 15), (29, 12), (33, 14),
         (38, 27), (47, 17), (51, 17), (53, 20), (47, 43), (43, 47),
         (17, 47), (13, 43), (8, 21)]
draw.polygon(crown, fill="#160c17")
draw.polygon([(11, 21), (13, 21), (21, 31), (27, 17), (29, 15), (31, 17),
              (37, 31), (48, 20), (50, 20), (44, 43), (41, 45), (18, 45),
              (15, 42)], fill="#ab6248")
draw.polygon([(12, 22), (21, 32), (29, 16), (37, 32), (48, 22), (44, 36),
              (43, 39), (17, 39)], fill="#eda94f")
draw.line([(13, 23), (21, 33), (29, 17), (36, 32)], fill="#ffe3a1", width=2)
draw.line([(38, 32), (48, 22)], fill="#ffe3a1", width=2)
draw.polygon([(29, 23), (34, 30), (30, 37), (25, 31)], fill="#683044")
draw.polygon([(29, 25), (32, 30), (29, 34), (27, 30)], fill="#d24c60")
draw.point((29, 27), fill="#ffb4a0")
draw.line([(17, 40), (44, 40)], fill="#6c3d36", width=2)
draw.line([(19, 42), (42, 42)], fill="#f2af56", width=2)

# Garnet wax seal. The forked fracture remains visible at 32-pixel thumbnail size.
draw.polygon([(44, 36), (51, 36), (55, 39), (58, 44), (57, 51), (53, 56),
              (46, 57), (40, 54), (37, 48), (38, 42)], fill="#160c17")
draw.polygon([(44, 38), (51, 38), (55, 42), (56, 47), (53, 53), (48, 55),
              (42, 52), (39, 47), (41, 41)], fill="#7d314e")
draw.line([(40, 46), (42, 42), (45, 40), (50, 40)], fill="#ce5c70", width=2)
draw.line([(43, 51), (47, 53), (52, 50), (54, 46)], fill="#52203b", width=2)
draw.line([(50, 42), (45, 46), (50, 47), (46, 51)], fill="#f39589", width=2)
draw.line([(45, 46), (43, 44)], fill="#f39589")

root = Path(__file__).resolve().parents[1]
canvas.resize((256, 256), Image.Resampling.NEAREST).save(root / "thunderstore/icon.png", optimize=True)
