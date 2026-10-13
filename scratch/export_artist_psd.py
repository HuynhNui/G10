from pathlib import Path
from PIL import Image, ImageOps
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer

root=Path(r'D:\G10')
art=root/'Assets/_Project/Art'
out=Path(r'D:\hinhchup\Mau_Anh_Sinh_Vat_3_Lop.psd')
size=(1920,1080)
psd=PSDImage.new('RGBA',size,color=(0,0,0,255))
bg=ImageOps.fit(Image.open(art/'PhotoCaptureRuntime/Z1_BG_OpenWater_Mid_A.png').convert('RGBA'),size,method=Image.Resampling.LANCZOS)
PixelLayer.frompil(bg,psd,name='01 - Nen nuoc (Background)')
creature=Image.open(root/'Assets/_Project/Content/Zone01/Creatures/001/Preview.png').convert('RGBA')
creature.thumbnail((630,840),Image.Resampling.LANCZOS)
PixelLayer.frompil(creature,psd,name='02 - Sinh vat 001 (Creature)',left=(1920-creature.width)//2,top=1080-creature.height-90)
overlay=ImageOps.fit(Image.open(art/'PhotoCaptureRuntime/Z1_Overlay_Fog_Mid.png').convert('RGBA'),size,method=Image.Resampling.LANCZOS)
layer=PixelLayer.frompil(overlay,psd,name='03 - Lop phu suong (Overlay)')
layer.opacity=45
psd.save(out)
check=PSDImage.open(out)
assert check.size==size and len(check)==3
assert all(layer.visible for layer in check)
assert check[1].topil().getchannel('A').getextrema()[0]==0
check.composite().save(root/'scratch/artist_brief/psd_preview.png')
print({'file':str(out),'bytes':out.stat().st_size,'size':check.size,'layers':[(l.name,l.opacity,l.bbox) for l in check]})
