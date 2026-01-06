#!/usr/bin/env python3
import os
import sys
import json
import glob
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(ROOT, 'src')
OUT_DIR = os.path.join(ROOT, 'PawSharp-Reference')

def parse_xml_file(path):
    tree = ET.parse(path)
    root = tree.getroot()
    assembly = root.find('assembly').find('name').text if root.find('assembly') is not None else os.path.basename(path)
    members = []
    for m in root.findall('.//member'):
        mid = m.get('name')
        kind = mid.split(':')[0] if mid and ':' in mid else ''
        # friendly name: strip the prefix
        friendly = mid.split(':',1)[1] if mid and ':' in mid else mid
        summary_el = m.find('summary')
        summary = ''.join(summary_el.itertext()).strip() if summary_el is not None else ''
        remarks_el = m.find('remarks')
        remarks = ''.join(remarks_el.itertext()).strip() if remarks_el is not None else ''
        returns_el = m.find('returns')
        returns = ''.join(returns_el.itertext()).strip() if returns_el is not None else ''
        example_el = m.find('example')
        example = ''.join(example_el.itertext()).strip() if example_el is not None else ''
        params = []
        for p in m.findall('param'):
            pname = p.get('name')
            ptext = ''.join(p.itertext()).strip()
            params.append({'name': pname, 'description': ptext})

        members.append({
            'id': mid,
            'kind': kind,
            'name': friendly,
            'summary': summary,
            'remarks': remarks,
            'returns': returns,
            'example': example,
            'params': params
        })
    return assembly, members

def find_xml_docs():
    patterns = [os.path.join(SRC, '*', 'bin', 'Debug', 'net8.0', '*.xml'),
                os.path.join(SRC, '*', 'bin', 'Release', 'net8.0', '*.xml')]
    files = []
    for p in patterns:
        files.extend(glob.glob(p))
    return files

def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    xml_files = find_xml_docs()
    if not xml_files:
        print('No XML doc files found. Run `dotnet build` first.', file=sys.stderr)
        sys.exit(1)

    index = []
    for xf in xml_files:
        assembly, members = parse_xml_file(xf)
        safe_name = assembly.replace(' ', '_').replace('/', '_')
        out_path = os.path.join(OUT_DIR, f'{safe_name}.json')
        with open(out_path, 'w', encoding='utf-8') as f:
            json.dump({'assembly': assembly, 'source': xf, 'members': members}, f, ensure_ascii=False, indent=2)
        index.append({'assembly': assembly, 'file': os.path.basename(out_path)})

    with open(os.path.join(OUT_DIR, 'index.json'), 'w', encoding='utf-8') as f:
        json.dump(index, f, ensure_ascii=False, indent=2)

    print(f'Generated docs for {len(index)} assemblies into {OUT_DIR}')

if __name__ == '__main__':
    main()
