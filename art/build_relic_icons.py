"""Render the original relic pixel sprites onto the game's 16 × 16 grid."""

from pathlib import Path

from PIL import Image, ImageColor


PALETTE = {
    "o": "#201523",  # card-frame outline and deep recesses
    "d": "#A96235", "g": "#E5A033", "y": "#FFE04A", "i": "#FFF0BA",
    "r": "#D75B55", "c": "#FF8C73",
    "b": "#2C6A81", "t": "#36A5B3", "m": "#6DE3D2", "k": "#B5F3DE",
    "q": "#786F83", "s": "#AFA8B6", "w": "#E4DCDF",
    "p": "#917097", "u": "#5C435B", "n": "#AB7D68", "f": "#DCB795",
    "l": "#362438",  # dark plum rail
    "v": "#822F51", "x": "#A54860", "h": "#F28D97",  # crimson shadow, inlay, glint
}

# Each character is one fill pixel. Dots leave room for the contour or transparency.
SPRITES = {
    "PaupersCrown": (
        ".....y......",
        "....iyi.....",
        "y...yyy...yg",
        "ig.yyyg..gyg",
        "ygyyyygggygd",
        ".yyyxyggggd.",
        ".yyvhxgggld.",
        ".gggxvgguld.",
        ".dggguddlll.",
        "..yygggggu..",
    ),
    "LastWatch": (
        "....gg....",
        "....ig....",
        "...gygd...",
        "..iyyggd..",
        "..yygdgd..",
        "..yyghgd..",
        ".iyyxvdud.",
        ".yygxldug.",
        "gygvluddgg",
        ".yyyggggg.",
        "....lu....",
        ".....x....",
    ),
    "CinderGospel": (
        "....wwiiy...",
        "..iwwiiiig..",
        ".iiiiiipggg.",
        ".pluppppguld",
        ".plpupgcguld",
        ".plpuvcyxuld",
        ".plpcgxirupd",
        ".pluvxgchupd",
        ".plupxvvpuud",
        ".puwwwiiggd.",
        "..wwiiiiid..",
        "...gddddd...",
    ),
    "HourglassOfAsh": (
        "iyyyyyyygg",
        "dggggggldd",
        ".kkmmttbb.",
        "..mtytbl..",
        "...tytl...",
        "....xl....",
        "...bhxb...",
        "..bthylt..",
        ".kyyggvlb.",
        "igggggggyd",
        "ddggduldgd",
    ),
    "GlassCrown": (
        ".....k......",
        "....kmt.....",
        "k...mmtt...m",
        "kt..mttb..mt",
        ".tmmmtbbtmtt",
        ".ttmtmxcttb.",
        "..tmbhvtmb..",
        "..bbtxtbub..",
        "...mttlub...",
        "...tbbull...",
    ),
    "HollowBulwark": (
        "...sshv...",
        ".wwsqqxsq.",
        "wssqbbvsqq",
        "ssqboobquq",
        "ssbboobquq",
        ".sbooobuq.",
        ".sbooobul.",
        "..bobbll..",
        "..buqul...",
        "...shv....",
        "....l.....",
    ),
    "FerrymansLedger": (
        ".ggppupp..",
        ".gpuppppl.",
        ".glupppuul",
        ".glpgxgpul",
        ".glpxhvpu.",
        ".glugxvpu.",
        ".gluuuppul",
        ".ggppppgpl",
        ".ydgggggul",
        "..iwwwiid.",
        "...iyydd..",
    ),
    "VulturesCovenant": (
        ".....ifi...",
        "....iifnn..",
        "....ifnhxn.",
        "....ffnoyg.",
        "...pnnddddd",
        "...pvxnn...",
        "...uxh.....",
        "..upv......",
        ".ppl.......",
        "ppul.......",
        "ul.........",
    ),
    "GildedRampart": (
        "...yg...yg...",
        "yg.yy...gg.yg",
        "yg.yy...gd.gg",
        "iyyyyggggggdg",
        "yygdggdgdlldd",
        "ggggggxgdgggd",
        "ggdgghxvggddd",
        "gggdxldlodddd",
        "gggdo...oldld",
        "ygggo...ovgld",
        "ddggo...oxlgl",
    ),
    "RedMercy": (
        "...iygg...",
        "...dggd...",
        "....mb....",
        "....tv....",
        "...mttb...",
        "..mmtblt..",
        ".kmhxrtvv.",
        ".tmxihxvl.",
        ".ttiiivxl.",
        ".ttxihxvl.",
        "..txxvvll.",
        "...htll...",
    ),
}


def render(rows):
    fill = {(x, y): tone for y, row in enumerate(rows) for x, tone in enumerate(row) if tone != "."}
    left, right = min(x for x, _ in fill), max(x for x, _ in fill)
    top, bottom = min(y for _, y in fill), max(y for _, y in fill)
    dx = (15 - right - left) // 2
    dy = (15 - bottom - top) // 2
    assert 0 < left + dx and right + dx < 15 and 0 < top + dy and bottom + dy < 15
    image = Image.new("RGBA", (16, 16))
    contour = ImageColor.getrgb(PALETTE["o"]) + (255,)
    for x, y in fill:
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            image.putpixel((nx + dx, ny + dy), contour)
    for (x, y), tone in fill.items():
        image.putpixel((x + dx, y + dy), ImageColor.getrgb(PALETTE[tone]) + (255,))
    return image


if __name__ == "__main__":
    destination = Path(__file__).resolve().parent
    for name, rows in SPRITES.items():
        render(rows).save(destination / f"gkj_CR_{name}.png", optimize=True)
