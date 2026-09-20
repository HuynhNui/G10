import os
import glob
import xml.etree.ElementTree as ET

# Check G10.Prototype.csproj
csproj_path = r"C:\Users\Duy\Documents\MyProject\G10\G10.Prototype.csproj"
editor_csproj_path = r"C:\Users\Duy\Documents\MyProject\G10\G10.Prototype.Editor.csproj"

def sync_csproj(csproj, search_dir, exclude_editor=True):
    with open(csproj, "r", encoding="utf-8") as f:
        content = f.read()

    # Find all compile includes
    existing = set()
    for line in content.splitlines():
        if '<Compile Include="' in line:
            path = line.split('<Compile Include="')[1].split('"')[0]
            existing.add(os.path.normpath(path).lower())

    missing = []
    for root, dirs, files in os.walk(search_dir):
        if exclude_editor and "editor" in root.lower():
            continue
        if not exclude_editor and "editor" not in root.lower():
            continue
        for file in files:
            if file.endswith(".cs"):
                full_rel = os.path.relpath(os.path.join(root, file), r"C:\Users\Duy\Documents\MyProject\G10")
                norm = os.path.normpath(full_rel).lower()
                if norm not in existing:
                    missing.append(full_rel)

    print(f"{csproj} missing {len(missing)} files:")
    for m in missing:
        print("  +", m)

    if missing:
        # Add before </ItemGroup>
        add_lines = "\n".join(f'    <Compile Include="{m}" />' for m in missing)
        marker = "</ItemGroup>"
        idx = content.find(marker)
        if idx != -1:
            new_content = content[:idx] + add_lines + "\n  " + content[idx:]
            with open(csproj, "w", encoding="utf-8") as f:
                f.write(new_content)
            print("Updated", csproj)

sync_csproj(csproj_path, r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Scripts", exclude_editor=True)
sync_csproj(editor_csproj_path, r"C:\Users\Duy\Documents\MyProject\G10\Assets\_Project\Scripts\Editor", exclude_editor=False)
