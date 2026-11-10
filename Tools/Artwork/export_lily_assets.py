"""Export original artist layers; no generated/repainted artwork. Run in creature-artwork-venv."""
import hashlib
import json
import shutil
from pathlib import Path
from psd_tools import PSDImage

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / '.tools/lily-artist-sources'
OUT = ROOT / 'Assets/_Project/Art/Lily25Days'
QA = ROOT / 'Docs/QA/LilyArtistIntegration/source-inspection'
OUT.mkdir(parents=True, exist_ok=True)
QA.mkdir(parents=True, exist_ok=True)
expected = {row['id']: row for row in json.loads((SOURCE / 'sources.json').read_text(encoding='utf-8'))}
for source_id, row in expected.items():
    candidates = list(SOURCE.glob(source_id + '.*'))
    if len(candidates) != 1 or hashlib.sha256(candidates[0].read_bytes()).hexdigest() != row['sha256']:
        raise ValueError('Source missing or changed: ' + source_id)
manifest = []

def save(image, name, source, operation):
    path = OUT / (name + '.png')
    image.save(path)
    manifest.append(dict(name=name, source=source.name, source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                         operation=operation, size=list(image.size), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))

p = SOURCE / '1SRp10cZNl5Upy_JrUcD7W7kko3ehgWnd.psd'
psd = PSDImage.open(p)
merged = psd.topil().convert('RGBA')
for i, name in enumerate(['BioPlastic', 'Letonium', 'Explosive', 'FireStone']):
    board = psd[i]
    image = merged.crop(board.bbox)
    # Keep complete board as reference before any transparent margin reduction.
    image.save(QA / f'{name}-full-artboard.png')
    # Explicit reviewed regions exclude only distant disconnected swatches;
    # retain the item's original painted light, shell, and fine edge pixels.
    regions = [(620, 40, 1400, 880), (690, 90, 1260, 930), (620, 110, 1400, 930), (580, 70, 1480, 950)]
    image = image.crop(regions[i])
    save(image, name, p, dict(artboard=i, source_rect=list(board.bbox), reviewed_local_region=regions[i],
                            note='Native saved composite; no resize, recolor, or background keying. Disconnected swatch pixels excluded.'))

p = SOURCE / '1od7o9pT-3jSj7B9X1fioHHrWgDpPafUh.psd'
psd = PSDImage.open(p)
merged = psd.topil().convert('RGBA')
save(merged.crop((203,38,1725,965)), 'LilyDossier', p, dict(artboard=0, source_rect=[203,38,1725,965], composite='saved native'))
save(merged.crop((2422,114,3630,914)), 'EmmaCard', p, dict(artboard=1, source_rect=[2422,114,3630,914], composite='saved native; original unknown birth date retained'))
paper = psd[2].composite(viewport=(4283,112,5787,957), alpha=0, layer_filter=lambda l: l.visible and l.kind != 'type')
save(paper, 'CoordinatePaper', p, dict(artboard=2, source_rect=[4283,112,5787,957], composite='original layers except placeholder coordinate type layers; live UI supplies actual X/Y/depth'))

p = SOURCE / '1ZEtYyM3i4RZXmg0Y4svgsgQqrzQKXJBu.psd'
psd = PSDImage.open(p)
background = psd[0].composite(viewport=(0,0,1920,1080), layer_filter=lambda l: l.visible and l.kind != 'type' and l.name != 'LILY')
save(background, 'MenuBackground', p, dict(artboard=0, composite='original background/character/ghost/weathering layers; logo and placeholder type excluded'))
logo = psd[0][4].composite()
save(logo, 'Lily25DaysLogo', p, dict(index_path=[0,4], composite='native raster wordmark'))

kenney = SOURCE / 'kenney'
for source, target in [('PNG/Double/Panel/panel-001.png', 'MenuButton.png'), ('License.txt','Kenney-License.txt')]:
    shutil.copy2(kenney/source, OUT/target)
shutil.copy2(SOURCE/'UTM AmericanaBEx.ttf', OUT/'UTM AmericanaBEx.ttf')
shutil.copy2(SOURCE/'UTM Avo.ttf', OUT/'UTM Avo.ttf')
(ROOT/'Docs/QA/LilyArtistIntegration/export-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Exported', len(manifest), 'native PSD images plus selected supplied Kenney UI and UTM font.')
