"""Offline Roslyn source compilation; never launches/connects to Unity Editor.

Reads Unity's existing Bee response inputs. Replaces source lists with current
nearest-asmdef sources, redirects every explicit compiler output to the QA folder,
and rebuilds runtime references before dependent editor/test assemblies. Default
mode prepares only; --run explicitly invokes the installed Roslyn compiler.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
RSP_ROOT = ROOT / 'Library/Bee/artifacts/1900b0aE.dag'
OUT = ROOT / 'Docs/QA/Zone03Cinematic/offline-build'
NAMES = ('G10.Prototype', 'G10.Prototype.Editor',
         'G10.Prototype.EditModeTests', 'G10.Prototype.PlayModeTests')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def slash(path):
    return str(path).replace('\\', '/')


def dump(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def resolve_assemblies():
    definitions, guids = {}, {}
    for path in (ROOT/'Assets').rglob('*.asmdef'):
        data = json.loads(path.read_text(encoding='utf-8-sig'))
        definitions[path.parent.resolve()] = (data['name'], path, data)
        meta = path.with_suffix(path.suffix+'.meta')
        if meta.exists():
            match = re.search(r'^guid:\s*(\S+)', meta.read_text(), re.M)
            if match:
                guids[match.group(1)] = data['name']
    boundaries = {folder: value[0] for folder, value in definitions.items()}
    for path in (ROOT/'Assets').rglob('*.asmref'):
        reference = json.loads(path.read_text(encoding='utf-8-sig'))['reference']
        name = guids.get(reference[5:]) if reference.startswith('GUID:') else reference
        if not name:
            raise RuntimeError('Unresolved asmref: '+slash(path))
        boundaries[path.parent.resolve()] = name
    sources = {name: [] for name in NAMES}
    for path in (ROOT/'Assets/_Project').rglob('*.cs'):
        folder = path.parent.resolve()
        while folder != ROOT and folder not in boundaries:
            folder = folder.parent
        name = boundaries.get(folder)
        if name in sources:
            sources[name].append(path)
    for files in sources.values():
        files.sort(key=lambda p: slash(p).lower())
    metadata = {name: (path,data) for name,path,data in definitions.values() if name in NAMES}
    assert set(metadata) == set(NAMES), 'Expected project assembly definitions not found'
    return sources, metadata


def compiler_paths():
    runtime_rsp = (RSP_ROOT/(NAMES[0]+'.rsp')).read_text(encoding='utf-8-sig')
    match = re.search(r'[-/]r:"([^"]+/Editor/Data)/Managed/', runtime_rsp)
    if not match:
        raise RuntimeError('Cannot identify Unity compiler from recorded references')
    data = Path(match.group(1))
    dotnet, csc = data/'NetCoreRuntime/dotnet.exe', data/'DotNetSdkRoslyn/csc.dll'
    if not dotnet.is_file() or not csc.is_file():
        raise RuntimeError('Recorded Unity Roslyn compiler is unavailable')
    return dotnet, csc


def prepare(sources, metadata):
    OUT.mkdir(parents=True, exist_ok=True)
    if not OUT.resolve().is_relative_to((ROOT/'Docs/QA/Zone03Cinematic').resolve()):
        raise RuntimeError('Unsafe output path')
    manifest = []
    for name in NAMES:
        template = RSP_ROOT/(name+'.rsp')
        options, references, prior_sources = [], [], []
        for line in template.read_text(encoding='utf-8-sig').splitlines():
            line = line.strip()
            if not line:
                continue
            if line.strip('"').lower().endswith('.cs') and not line.startswith(('-', '/')):
                prior_sources.append(line.strip('"'))
                continue
            if re.match(r'^[-/](out|refout|pdb|doc|errorlog|generatedfilesout):',line,re.I):
                continue
            reference = re.match(r'^[-/](?:r|reference):"?([^"\n]+)"?$',line,re.I)
            if reference:
                path = Path(reference.group(1))
                matched = next((n for n in NAMES if path.name in (n+'.dll',n+'.ref.dll')),None)
                if matched:
                    path = OUT/path.name
                    line = '-r:"'+slash(path)+'"'
                else:
                    absolute = path if path.is_absolute() else ROOT/path
                    if not absolute.is_file():
                        raise RuntimeError('Missing recorded reference: '+slash(absolute))
                references.append(slash(path))
            options.append(line)
        options += ['-out:"'+slash(OUT/(name+'.dll'))+'"',
                    '-refout:"'+slash(OUT/(name+'.ref.dll'))+'"',
                    '-pdb:"'+slash(OUT/(name+'.pdb'))+'"',
                    '-generatedfilesout:"'+slash(OUT/'generated'/name)+'"']
        (OUT/'generated'/name).mkdir(parents=True,exist_ok=True)
        options += ['"'+slash(p)+'"' for p in sources[name]]
        rsp = OUT/(name+'.rsp');rsp.write_text('\n'.join(options)+'\n',encoding='utf-8')
        asmdef, definition = metadata[name]
        manifest.append(dict(name=name,template=slash(template.relative_to(ROOT)),
            template_sha256=sha(template),response_file=slash(rsp.relative_to(ROOT)),
            source_count=len(sources[name]),sources={slash(p.relative_to(ROOT)):sha(p) for p in sources[name]},
            newly_added_sources=sorted(set(slash(p.relative_to(ROOT)) for p in sources[name])-set(prior_sources)),
            asmdef=slash(asmdef.relative_to(ROOT)),asmdef_sha256=sha(asmdef),
            current_declared_references=definition.get('references',[]),recorded_reference_paths=references))
    dump(OUT/'inputs.json',manifest)
    return manifest


def run(manifest,dotnet,csc):
    env=os.environ.copy();temp=OUT/'temp';temp.mkdir(exist_ok=True)
    env.update(DOTNET_CLI_HOME=str(OUT/'dotnet-home'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1',DOTNET_MULTILEVEL_LOOKUP='0',
               TMP=str(temp),TEMP=str(temp))
    results=[];runtime_ok=False
    for assembly in manifest:
        name=assembly['name']
        if name!=NAMES[0] and not runtime_ok:
            results.append(dict(name=name,skipped=True,reason='Current runtime compilation failed; stale runtime references are forbidden.'))
            continue
        command=[str(dotnet),str(csc),'@'+str(OUT/(name+'.rsp'))]
        process=subprocess.run(command,cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,
            text=True,encoding='utf-8',errors='replace',timeout=180,
            creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        log=OUT/(name+'.log');log.write_text(process.stdout,encoding='utf-8')
        diagnostics=[line for line in process.stdout.splitlines() if re.search(r'\b(?:error|warning)\s+(?:CS|AD|SG)\d+',line)]
        changed=[p for p,h in assembly['sources'].items() if not (ROOT/p).is_file() or sha(ROOT/p)!=h]
        result=dict(name=name,exit_code=process.returncode,success=process.returncode==0,
            source_count=assembly['source_count'],log=slash(log.relative_to(ROOT)),diagnostics=diagnostics,
            changed_sources_during_compile=changed,
            asmdef_changed_during_compile=sha(ROOT/assembly['asmdef'])!=assembly['asmdef_sha256'])
        results.append(result)
        if name==NAMES[0]:runtime_ok=process.returncode==0
        print(json.dumps(result),flush=True)
    final_sources,_=resolve_assemblies()
    membership_changes={a['name']:sorted(set(a['sources']) ^ {slash(p.relative_to(ROOT)) for p in final_sources[a['name']]}) for a in manifest}
    stable=all(not r.get('changed_sources_during_compile') and not r.get('asmdef_changed_during_compile') for r in results) and not any(membership_changes.values())
    summary=dict(mode='Offline Roslyn source compilation only',
        success=len(results)==4 and all(r.get('success',False) for r in results) and stable,
        source_snapshot_stable=stable,source_membership_changes_during_compile=membership_changes,
        compiler=slash(csc),host=slash(dotnet),assemblies=results,
        limitations=['Does not launch or connect to Unity Editor.',
                     'Does not import/validate shaders, scenes, serialized assets or runtime behavior.',
                     'Does not execute EditMode/PlayMode tests; test assemblies are compiled only.',
                     'Retains existing Unity-generated defines/references/analyzers; dependency import state is the recorded one.'])
    dump(OUT/'results.json',summary)
    return 0 if summary['success'] else 1


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run',action='store_true',help='Invoke Roslyn; default only prepares inputs.')
    args=parser.parse_args();sources,metadata=resolve_assemblies();dotnet,csc=compiler_paths()
    manifest=prepare(sources,metadata)
    print(json.dumps(dict(prepared=True,assemblies={a['name']:a['source_count'] for a in manifest},
        compiler=slash(csc),output=slash(OUT.relative_to(ROOT)),run=args.run)),flush=True)
    return run(manifest,dotnet,csc) if args.run else 0


if __name__=='__main__':
    sys.exit(main())
