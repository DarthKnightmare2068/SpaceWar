#!/usr/bin/env python3
"""
Script to find unused assets in Unity project.
Analyzes all GUIDs in the GameResources folder and checks if they are referenced
in any scene, prefab, or script.
"""

import os
import re
from pathlib import Path
from collections import defaultdict

# Project root
PROJECT_ROOT = Path(r"c:\Users\hoalo\OneDrive\Desktop\SpaceWar")
ASSETS_PATH = PROJECT_ROOT / "Assets"
GAME_RESOURCES_PATH = ASSETS_PATH / "GameResources"
PREFABS_PATH = ASSETS_PATH / "Prefabs"
SCENES_PATH = ASSETS_PATH / "Scenes"
SCRIPTS_PATH = ASSETS_PATH / "Scripts"

# Asset extensions to track (excluding .meta files)
ASSET_EXTENSIONS = {
    '.png', '.jpg', '.jpeg', '.tga', '.psd', '.exr', '.tif', '.tiff',  # Textures
    '.fbx', '.obj', '.blend', '.max', '.ma', '.mb', '.dae',  # Models
    '.mat',  # Materials
    '.prefab',  # Prefabs
    '.mp3', '.wav', '.ogg', '.aiff',  # Audio
    '.shader', '.shadergraph', '.cginc', '.hlsl',  # Shaders
    '.anim', '.controller',  # Animations
    '.unity',  # Scenes
    '.asset',  # ScriptableObjects/other assets
    '.ttf', '.otf',  # Fonts
    '.mp4', '.mov', '.avi',  # Video
    '.json', '.txt', '.xml',  # Data files
    '.lighting',  # Lighting data
}

# Folders to ignore (typically demo/example folders from asset store)
DEMO_FOLDERS = {
    'Demo', 'demo', 'Demo Scenes', 'DemoScene', 'Examples', 'Sample', 'Samples',
    'Demo scene lasers', 'Demo Scenes'
}

def get_guid_from_meta(meta_path):
    """Extract GUID from a .meta file."""
    try:
        with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            match = re.search(r'guid:\s*([a-f0-9]{32})', content)
            if match:
                return match.group(1)
    except Exception as e:
        pass
    return None

def collect_game_resource_assets():
    """Collect all assets in GameResources folder with their GUIDs."""
    assets = {}  # guid -> asset_path
    
    for root, dirs, files in os.walk(GAME_RESOURCES_PATH):
        rel_root = Path(root).relative_to(PROJECT_ROOT)
        
        for file in files:
            if file.endswith('.meta'):
                continue
                
            file_path = Path(root) / file
            ext = file_path.suffix.lower()
            
            if ext in ASSET_EXTENSIONS:
                meta_path = file_path.with_suffix(file_path.suffix + '.meta')
                if meta_path.exists():
                    guid = get_guid_from_meta(meta_path)
                    if guid:
                        assets[guid] = str(file_path.relative_to(PROJECT_ROOT))
    
    return assets

def collect_prefab_assets():
    """Collect all assets in Prefabs folder with their GUIDs."""
    assets = {}
    
    for root, dirs, files in os.walk(PREFABS_PATH):
        for file in files:
            if file.endswith('.meta'):
                continue
            if file.endswith('.prefab'):
                file_path = Path(root) / file
                meta_path = file_path.with_suffix(file_path.suffix + '.meta')
                if meta_path.exists():
                    guid = get_guid_from_meta(meta_path)
                    if guid:
                        assets[guid] = str(file_path.relative_to(PROJECT_ROOT))
    
    return assets

def find_all_guid_references():
    """Find all GUID references in scenes, prefabs, and assets."""
    referenced_guids = set()
    guid_pattern = re.compile(r'guid:\s*([a-f0-9]{32})')
    
    # Search in scenes
    for root, dirs, files in os.walk(SCENES_PATH):
        for file in files:
            if file.endswith('.unity') or file.endswith('.asset'):
                file_path = Path(root) / file
                try:
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read()
                        matches = guid_pattern.findall(content)
                        referenced_guids.update(matches)
                except Exception:
                    pass
    
    # Search in prefabs folder
    for root, dirs, files in os.walk(PREFABS_PATH):
        for file in files:
            if file.endswith('.prefab') or file.endswith('.asset'):
                file_path = Path(root) / file
                try:
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read()
                        matches = guid_pattern.findall(content)
                        referenced_guids.update(matches)
                except Exception:
                    pass
    
    # Search in GameResources prefabs and assets
    for root, dirs, files in os.walk(GAME_RESOURCES_PATH):
        for file in files:
            if file.endswith('.prefab') or file.endswith('.asset') or file.endswith('.mat'):
                file_path = Path(root) / file
                try:
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read()
                        matches = guid_pattern.findall(content)
                        referenced_guids.update(matches)
                except Exception:
                    pass
    
    # Search in scripts for Resources.Load patterns (indirect references)
    # This is a simplified check
    
    return referenced_guids

def is_demo_asset(path):
    """Check if asset is in a demo/example folder."""
    path_parts = Path(path).parts
    for part in path_parts:
        if part in DEMO_FOLDERS:
            return True
    return False

def categorize_unused(unused_assets):
    """Categorize unused assets by type."""
    categories = defaultdict(list)
    
    for guid, path in unused_assets.items():
        ext = Path(path).suffix.lower()
        is_demo = is_demo_asset(path)
        
        if is_demo:
            categories['Demo/Example Assets'].append(path)
        elif ext in {'.png', '.jpg', '.jpeg', '.tga', '.psd', '.exr', '.tif', '.tiff'}:
            categories['Textures'].append(path)
        elif ext in {'.fbx', '.obj', '.blend', '.max', '.ma', '.mb', '.dae'}:
            categories['3D Models'].append(path)
        elif ext == '.mat':
            categories['Materials'].append(path)
        elif ext == '.prefab':
            categories['Prefabs'].append(path)
        elif ext in {'.mp3', '.wav', '.ogg', '.aiff'}:
            categories['Audio'].append(path)
        elif ext in {'.shader', '.shadergraph', '.cginc', '.hlsl'}:
            categories['Shaders'].append(path)
        elif ext == '.unity':
            categories['Scenes'].append(path)
        else:
            categories['Other'].append(path)
    
    return categories

def main():
    print("=" * 70)
    print("UNITY UNUSED ASSET FINDER")
    print("=" * 70)
    print()
    
    # Collect all assets in GameResources
    print("Collecting assets from GameResources...")
    game_resource_assets = collect_game_resource_assets()
    print(f"  Found {len(game_resource_assets)} assets in GameResources")
    
    # Collect prefab assets
    print("Collecting assets from Prefabs...")
    prefab_assets = collect_prefab_assets()
    print(f"  Found {len(prefab_assets)} prefabs in Prefabs folder")
    
    # Find all referenced GUIDs
    print("Scanning for GUID references in scenes and prefabs...")
    referenced_guids = find_all_guid_references()
    print(f"  Found {len(referenced_guids)} unique GUID references")
    
    # Find unused assets
    print()
    print("Analyzing unused assets...")
    unused_assets = {}
    
    for guid, path in game_resource_assets.items():
        if guid not in referenced_guids:
            unused_assets[guid] = path
    
    # Categorize
    categories = categorize_unused(unused_assets)
    
    # Calculate sizes
    total_unused_size = 0
    for guid, path in unused_assets.items():
        try:
            full_path = PROJECT_ROOT / path
            if full_path.exists():
                total_unused_size += full_path.stat().st_size
        except:
            pass
    
    # Report
    print()
    print("=" * 70)
    print("REPORT: UNUSED ASSETS")
    print("=" * 70)
    print()
    print(f"Total assets in GameResources: {len(game_resource_assets)}")
    print(f"Unused assets: {len(unused_assets)}")
    print(f"Estimated size of unused assets: {total_unused_size / 1024 / 1024:.2f} MB")
    print()
    
    # Print by category
    for category, paths in sorted(categories.items()):
        print(f"\n{category} ({len(paths)} files):")
        print("-" * 50)
        
        # Calculate category size
        cat_size = 0
        for path in paths:
            try:
                full_path = PROJECT_ROOT / path
                if full_path.exists():
                    cat_size += full_path.stat().st_size
            except:
                pass
        
        print(f"Size: {cat_size / 1024 / 1024:.2f} MB")
        
        # Show first 10 examples
        for path in sorted(paths)[:10]:
            print(f"  - {path}")
        if len(paths) > 10:
            print(f"  ... and {len(paths) - 10} more")
    
    # Write detailed report to file
    report_path = PROJECT_ROOT / "UNUSED_ASSETS_REPORT.md"
    with open(report_path, 'w', encoding='utf-8') as f:
        f.write("# Unused Assets Report\n\n")
        f.write(f"Generated: {__import__('datetime').datetime.now().isoformat()}\n\n")
        f.write(f"## Summary\n\n")
        f.write(f"- Total assets in GameResources: {len(game_resource_assets)}\n")
        f.write(f"- Unused assets: {len(unused_assets)}\n")
        f.write(f"- Estimated size: {total_unused_size / 1024 / 1024:.2f} MB\n\n")
        
        f.write("## IMPORTANT NOTES\n\n")
        f.write("1. **Demo/Example folders** are from Asset Store packages and can likely be deleted safely.\n")
        f.write("2. Before deleting any assets, create a backup!\n")
        f.write("3. Some assets may be loaded dynamically via `Resources.Load()` - check scripts.\n")
        f.write("4. Prefabs inside GameResources may use other GameResources assets.\n\n")
        
        f.write("## Unused Assets by Category\n\n")
        
        for category, paths in sorted(categories.items()):
            cat_size = 0
            for path in paths:
                try:
                    full_path = PROJECT_ROOT / path
                    if full_path.exists():
                        cat_size += full_path.stat().st_size
                except:
                    pass
            
            f.write(f"### {category} ({len(paths)} files, {cat_size / 1024 / 1024:.2f} MB)\n\n")
            for path in sorted(paths):
                f.write(f"- `{path}`\n")
            f.write("\n")
    
    print(f"\n\nDetailed report written to: {report_path}")
    
    # List safe-to-delete folders
    print("\n" + "=" * 70)
    print("SAFE TO DELETE (Demo/Example folders from Asset Store):")
    print("=" * 70)
    demo_folders_found = set()
    for path in unused_assets.values():
        if is_demo_asset(path):
            parts = Path(path).parts
            for i, part in enumerate(parts):
                if part in DEMO_FOLDERS:
                    demo_folders_found.add(str(Path(*parts[:i+1])))
    
    for folder in sorted(demo_folders_found):
        print(f"  - {folder}")

if __name__ == "__main__":
    main()
