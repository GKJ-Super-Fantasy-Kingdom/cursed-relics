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
draw.rectangle((5, 5, 58, 58), fill="#362438")
draw.rectangle((7, 7, 56, 56), fill="#201523")
for x, y in ((6, 6), (54, 6), (6, 54), (54, 54)):
    draw.rectangle((x, y, x + 3, y + 3), fill="#A54860")
    draw.point((x + 1, y + 1), fill="#CE8B78")

# Low-contrast lozenge under the open silhouette of an upright gold crown.
draw.polygon([(29, 9), (52, 32), (29, 55), (7, 32)], fill="#362438")
draw.line([(10, 32), (29, 13), (49, 33)], fill="#5C435B")
draw.ellipse((13, 42, 52, 50), fill="#170f20")
crown = [(10, 18), (14, 18), (21, 27), (26, 15), (29, 12), (33, 14),
         (38, 27), (47, 17), (51, 17), (53, 20), (47, 43), (43, 47),
         (17, 47), (13, 43), (8, 21)]
draw.polygon(crown, fill="#160c17")
draw.polygon([(11, 21), (13, 21), (21, 31), (27, 17), (29, 15), (31, 17),
              (37, 31), (48, 20), (50, 20), (44, 43), (41, 45), (18, 45),
              (15, 42)], fill="#4C2944")
draw.polygon([(12, 22), (21, 32), (29, 16), (37, 32), (48, 22), (44, 36),
              (43, 39), (17, 39)], fill="#754854")
draw.polygon([(13, 24), (21, 32), (29, 17), (35, 31), (27, 29), (23, 37), (17, 36)], fill="#B47258")
draw.line([(13, 23), (21, 33), (29, 17), (36, 32)], fill="#F6BD80", width=2)
draw.line([(38, 32), (48, 22)], fill="#CE8B78", width=2)
draw.polygon([(29, 23), (35, 30), (30, 38), (24, 31)], fill="#362438")
draw.polygon([(29, 25), (33, 30), (29, 35), (26, 30)], fill="#A54860")
draw.line([(29, 27), (28, 30), (31, 31), (29, 35)], fill="#F28D97", width=2)
draw.line([(29, 35), (27, 38), (30, 40), (28, 43)], fill="#A54860", width=2)
draw.line([(17, 40), (25, 40)], fill="#CE8B78", width=2)
draw.line([(32, 40), (44, 40)], fill="#A05E59", width=2)
draw.line([(19, 43), (26, 43)], fill="#9E6556", width=2)

# Garnet wax seal. The forked fracture remains visible at 32-pixel thumbnail size.
draw.polygon([(44, 36), (51, 36), (55, 39), (58, 44), (57, 51), (53, 56),
              (46, 57), (40, 54), (37, 48), (38, 42)], fill="#160c17")
draw.polygon([(44, 38), (51, 38), (55, 42), (56, 47), (53, 53), (48, 55),
              (42, 52), (39, 47), (41, 41)], fill="#822F51")
draw.line([(40, 46), (42, 42), (45, 40), (50, 40)], fill="#D86A7D", width=2)
draw.line([(43, 51), (47, 53), (52, 50), (54, 46)], fill="#52203B", width=2)
draw.line([(50, 42), (45, 46), (50, 47), (46, 51)], fill="#F28D97", width=2)
draw.line([(45, 46), (43, 44)], fill="#F28D97")

root = Path(__file__).resolve().parents[1]
canvas.resize((256, 256), Image.Resampling.NEAREST).save(root / "thunderstore/icon.png", optimize=True)
