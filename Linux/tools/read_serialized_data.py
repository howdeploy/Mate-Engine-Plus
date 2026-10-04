"""Read original player data without launching Unity; never edit the originals."""
import json
import sys
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers import TypeTreeHelper

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "steam-source/MateEngineX_Data"
TypeTreeHelper.read_typetree_boost = None
generator = TypeTreeGenerator("6000.2.6f2")
generator.load_local_dll_folder(str(DATA / "Managed"))
reference_generator = TypeTreeGenerator("6000.2.6f2")
reference_generator.load_local_dll_folder(str(DATA / "Managed"))
def fix_nodes(node):
    for item in node.traverse():
        # AssetsTools names string[] nodes "string". UnityPy's primitive
        # reader would mistake the array count for a byte length.
        if item.m_Type == "string" and item.m_Children and item.m_Children[0].m_Type == "Array":
            data = item.m_Children[0].m_Children[-1]
            if data.m_Type != "char": item.m_Type = "vector"
    return node
# The player strips type trees. Generate missing SerializeReference payload
# schemas from its own assemblies rather than dropping their fields.
original_ref_node = TypeTreeHelper.get_ref_type_node
def ref_node(value, assetfile):
    try:
        return original_ref_node(value, assetfile)
    except ValueError:
        typ = value["type"]
        if not typ["class"] or not typ["asm"]:
            return None
        fullname = (typ["ns"] + "." if typ["ns"] else "") + typ["class"]
        node = reference_generator.get_nodes_up(typ["asm"], fullname)
        # The AssetsTools backend prepends a MonoBehaviour header even to
        # ordinary serializable classes. Registry payloads have no such header.
        head = ["m_GameObject", "m_Enabled", "m_Script", "m_Name"]
        if [child.m_Name for child in node.m_Children[:4]] == head:
            node.m_Children = node.m_Children[4:]
        return fix_nodes(node)
TypeTreeHelper.get_ref_type_node = ref_node
paths = [DATA / "globalgamemanagers", DATA / "globalgamemanagers.assets", DATA / "resources.assets", DATA / "sharedassets0.assets", DATA / "level0"]
paths += sorted((DATA / "StreamingAssets/aa/StandaloneWindows64").glob("*.bundle"))
env = UnityPy.load(*map(str, paths))
env.typetree_generator = generator
target = ROOT / "inspection/serialized-original"
target.mkdir(exist_ok=True)
report = []
for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    base = obj.parse_monobehaviour_head()
    script = base.m_Script.read()
    assembly = script.m_AssemblyName
    typename = (script.m_Namespace + "." if script.m_Namespace else "") + script.m_ClassName
    if "Localization" not in assembly and typename not in sys.argv[1:]:
        continue
    entry = {"file": obj.assets_file.name, "path_id": obj.path_id, "type": typename}
    try:
        node = fix_nodes(obj.generate_monobehaviour_node())
        for child in node.m_Children:
            if child.m_Name == "m_Enabled": child.m_MetaFlag = 16384
        tree = obj.read_typetree(nodes=node)
        filename = f"{Path(obj.assets_file.name).name}-{obj.path_id}.json"
        (target / filename).write_text(json.dumps(tree, ensure_ascii=False, indent=2))
        entry["json"] = filename
        entry["fields"] = list(tree)
    except Exception as exc:
        entry["error"] = str(exc)
        try:
            tree = obj.read_typetree(nodes=node, check_read=False)
            (target / f"partial-{Path(obj.assets_file.name).name}-{obj.path_id}.json").write_text(json.dumps(tree, ensure_ascii=True, indent=2))
        except Exception:
            pass
    report.append(entry)
(target / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2))
print(json.dumps({"total": len(report), "errors": sum("error" in row for row in report)}, indent=2))
