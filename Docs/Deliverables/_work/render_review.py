from pathlib import Path
from pypdf import PdfReader
from pdf2image import convert_from_path
from PIL import Image, ImageOps, ImageDraw, ImageFont
import json
import sys
sys.stdout.reconfigure(encoding='utf-8')

root=Path('D:/G10/Docs/Deliverables/_work')
poppler='C:/Users/Artermis/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/poppler/Library/bin'
for base in ['G10_SRS_IEEE_830_1998','G10_Game_Concept']:
    pdf=root/(base+'.pdf');out=root/(base+'-final-pages');out.mkdir(exist_ok=True)
    reader=PdfReader(pdf)
    summary=[]
    for i,page in enumerate(reader.pages,1):
        text=page.extract_text() or ''
        lines=[l.strip() for l in text.splitlines() if l.strip()]
        summary.append({'page':i,'chars':len(text),'first':lines[:3],'last':lines[-3:]})
    print(base,json.dumps(summary,ensure_ascii=False))
    convert_from_path(str(pdf),dpi=115,output_folder=str(out),output_file='page',fmt='png',poppler_path=poppler,thread_count=3,paths_only=True)
    files=list(out.glob('*.png'))
    files.sort(key=lambda p:int(p.stem.split('-')[-1]))
    for start in range(0,len(files),6):
        sheet=Image.new('RGB',(1200,1770),'#cdd1d4');dr=ImageDraw.Draw(sheet)
        for j,path in enumerate(files[start:start+6]):
            im=Image.open(path);im.thumbnail((570,540))
            x=(j%2)*600+(600-im.width)//2;y=(j//2)*590+30
            sheet.paste(im,(x,y));dr.text(((j%2)*600+20,(j//2)*590+8),f'{base} page {start+j+1}',fill='black')
        sheet.save(out/f'contact-{start+1}.jpg')
