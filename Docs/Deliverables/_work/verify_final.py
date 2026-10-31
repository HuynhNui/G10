from pathlib import Path
from pypdf import PdfReader
from pdf2image import convert_from_path
import re, json
base=Path('D:/G10/Docs/Deliverables')
for name,count in [('G10_SRS_IEEE_830_1998',21),('G10_Game_Concept',12)]:
    artifact=base/(name+'.doc')
    assert artifact.read_bytes()[:8] == bytes.fromhex('D0CF11E0A1B11AE1'), 'Expected actual binary Word DOC'
    reader=PdfReader(base/'_work'/(name+'.pdf'))
    assert len(reader.pages)==count
    texts=[p.extract_text() for p in reader.pages]
    assert all(len(t)>400 for t in texts), 'Unexpected short or empty page'
    assert all('\ufffd' not in t for t in texts)
    if 'SRS' in name:
        alltext='\n'.join(texts)
        ids=set(re.findall(r'FR-\d{2}',alltext))
        assert all(f'FR-{i:02d}' in ids for i in range(1,39))
    else:
        assert 'không trình bày số 5' not in '\n'.join(texts)
    print(json.dumps({'file':artifact.name,'pages':count,'bytes':artifact.stat().st_size,'format':'Word 97-2003 binary DOC'},ensure_ascii=True))
convert_from_path(str(base/'_work/G10_Game_Concept.pdf'),dpi=115,first_page=5,last_page=5,
    output_folder=str(base/'_work'),output_file='revised-concept-page5',fmt='png',single_file=True,
    poppler_path='C:/Users/Artermis/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/poppler/Library/bin',paths_only=True)
