"""Render the original relic pixel sprites onto the game's 16 × 16 grid."""

from pathlib import Path

from PIL import Image, ImageColor


PALETTE = {
    "o": "#100B17",  # contour and deep recesses
    "d": "#A96235", "g": "#E5A033", "y": "#FFE04A", "i": "#FFF0BA",
    "a": "#8C393D", "r": "#D75B55", "c": "#FF8C73",
    "b": "#2C6A81", "t": "#36A5B3", "m": "#6DE3D2", "k": "#B5F3DE",
    "q": "#786F83", "s": "#AFA8B6", "w": "#E4DCDF",
    "p": "#917097", "u": "#5F425F", "n": "#AB7D68", "f": "#DCB795",
}

# Each character is one fill pixel. Dots leave room for the contour or transparency.
SPRITES = {
    "PaupersCrown": (
        ".....y......",
        "....iyi.....",
        "y...yyy...yg",
        "ig.yyyg..gyg",
        "ygyyyygggygd",
        ".yyyryggggd.",
        ".yygcrggggd.",
        ".ggggrgdgdd.",
        ".dggggddddd.",
        "..yygggggd..",
    ),
    "LastWatch": (
        "....gg....",
        "....ig....",
        "...gygd...",
        "..iyyggd..",
        "..yygdgd..",
        "..yygogd..",
        ".iyyogdgd.",
        ".yygogddg.",
        "gygogdddgg",
        ".yyyggggg.",
        "....dd....",
        ".....g....",
    ),
    "CinderGospel": (
        "....wwiiy...",
        "..iwwiiiig..",
        ".iiiiiipggg.",
        ".puuppppgugd",
        ".pupupgcguud",
        ".pupurcyruud",
        ".pupcgrirupd",
        ".puurrgcrupd",
        ".puupcrrpuud",
        ".puwwwiiggd.",
        "..wwiiiiid..",
        "...gddddd...",
    ),
    "HourglassOfAsh": (
        "iyyyyyyygg",
        "dggggggddd",
        ".kkmmttbb.",
        "..mtytbb..",
        "...tytb...",
        "....yb....",
        "...bytb...",
        "..btyybt..",
        ".kyyyggtb.",
        "igggggggyd",
        "ddggddddgd",
    ),
    "GlassCrown": (
        ".....k......",
        "....kmt.....",
        "k...mmtt...m",
        "kt..mttb..mt",
        ".tmmmtbbtmtt",
        ".ttmtmbtttb.",
        "..tmbtttmb..",
        "..bbtmtbbb..",
        "...mttbtb...",
        "...tbbbbb...",
    ),
    "HollowBulwark": (
        "...ssgs...",
        ".wwsqqgsq.",
        "wssqbbbsqq",
        "ssqboobqqq",
        "ssbboobqqq",
        ".sbooobqq.",
        ".sbooobqq.",
        "..bobbqq..",
        "..bqqqq...",
        "...sgq....",
        "....q.....",
    ),
    "FerrymansLedger": (
        ".ggppupp..",
        ".gpuppppu.",
        ".guupppuuu",
        ".gupgogpuu",
        ".gupgiopu.",
        ".guugggpu.",
        ".guuupppuu",
        ".ggppppgpu",
        ".ydgggggdu",
        "..iwwwiid.",
        "...iyydd..",
    ),
    "VulturesCovenant": (
        ".....ifi...",
        "....iifnn..",
        "....ifnonn.",
        "....ffnoyg.",
        "...pnnddddd",
        "...pufnn...",
        "...upf.....",
        "..upu......",
        ".ppu.......",
        "ppuu.......",
        "up.........",
    ),
    "GildedRampart": (
        "...yg...yg...",
        "yg.yy...gg.yg",
        "yg.yy...gd.gg",
        "iyyyyggggggdg",
        "yygdggdgddddd",
        "ggggggygdgggd",
        "ggdggoooggddd",
        "gggdodddodddd",
        "gggdo...odddd",
        "ygggo...odgdd",
        "ddggo...oddgd",
    ),
    "RedMercy": (
        "...iygg...",
        "...dggd...",
        "....mb....",
        "....tb....",
        "...mttb...",
        "..mmtbbt..",
        ".kmccrtaa.",
        ".tmricraa.",
        ".ttiiiara.",
        ".ttricara.",
        "..trraaab.",
        "...ctba...",
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
