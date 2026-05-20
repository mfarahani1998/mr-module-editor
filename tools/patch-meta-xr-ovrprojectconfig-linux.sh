#!/usr/bin/env bash
set -euo pipefail

# Local Ubuntu/Linux Editor workaround for Meta XR Core SDK 201.0.0.
#
# Default behavior:
#   1. Locate your Unity project.
#   2. Copy Meta XR Core from:
#        Library/PackageCache/com.meta.xr.sdk.core@201.0.0
#      into an embedded project package at:
#        Packages/com.meta.xr.sdk.core
#   3. Patch:
#        Packages/com.meta.xr.sdk.core/Editor/OVRProjectConfig.cs
#
# Why embed?
#   Patching Library/PackageCache is fragile because Unity can regenerate that
#   cache. Embedded packages live in the project Packages/ folder and Unity gives
#   them priority over registry/cache package copies.
#
# Direct PackageCache patching is still available with --no-embed, but it is not
# recommended except as a quick diagnostic.
#
# Usage:
#   ./tools/patch-meta-xr-ovrprojectconfig-linux.sh
#   ./tools/patch-meta-xr-ovrprojectconfig-linux.sh "Source/CUTeR Arm Simulator v2"
#   ./tools/patch-meta-xr-ovrprojectconfig-linux.sh "/absolute/path/to/project"
#   ./tools/patch-meta-xr-ovrprojectconfig-linux.sh "/absolute/path/to/OVRProjectConfig.cs"
#
# Options:
#   --no-embed        Patch the located file directly, usually PackageCache.
#                     This preserves the old transient behavior.
#   --force-reembed   If Packages/com.meta.xr.sdk.core already exists, back it up,
#                     replace it from Library/PackageCache, then patch the fresh
#                     embedded copy.
#
# Environment:
#   META_XR_CORE_VERSION=201.0.0   Override the Meta XR Core package version.

PACKAGE_ID="com.meta.xr.sdk.core"
PACKAGE_VERSION="${META_XR_CORE_VERSION:-201.0.0}"

CACHE_PACKAGE_DIR="Library/PackageCache/${PACKAGE_ID}@${PACKAGE_VERSION}"
CACHE_RELATIVE_FILE="${CACHE_PACKAGE_DIR}/Editor/OVRProjectConfig.cs"

EMBEDDED_PACKAGE_DIR="Packages/${PACKAGE_ID}"
EMBEDDED_RELATIVE_FILE="${EMBEDDED_PACKAGE_DIR}/Editor/OVRProjectConfig.cs"

EMBED_PACKAGE=1
FORCE_REEMBED=0

usage() {
  cat <<EOF_USAGE
Usage:
  $0 [OPTIONS] [UNITY_PROJECT_ROOT_OR_OVRPROJECTCONFIG_FILE]

Examples:
  $0
  $0 "Source/CUTeR Arm Simulator v2"
  $0 "/home/user/project"
  $0 "/home/user/project/Library/PackageCache/com.meta.xr.sdk.core@201.0.0/Editor/OVRProjectConfig.cs"
  $0 --no-embed "/home/user/project"
  $0 --force-reembed "/home/user/project"

Options:
  --no-embed        Patch the located file directly instead of embedding first.
  --force-reembed   Replace an existing embedded package from PackageCache first.
  -h, --help        Show this help.

Environment:
  META_XR_CORE_VERSION=201.0.0   Override the Meta XR Core package version.
EOF_USAGE
}

args=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --no-embed)
      EMBED_PACKAGE=0
      shift
      ;;
    --force-reembed)
      FORCE_REEMBED=1
      shift
      ;;
    --)
      shift
      while [[ $# -gt 0 ]]; do
        args+=("$1")
        shift
      done
      ;;
    -* )
      echo "ERROR: Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
    *)
      args+=("$1")
      shift
      ;;
  esac
done

if [[ "${#args[@]}" -gt 1 ]]; then
  usage >&2
  exit 2
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: python3 is required." >&2
  exit 1
fi

if ! command -v realpath >/dev/null 2>&1; then
  echo "ERROR: realpath is required." >&2
  exit 1
fi

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
input="${args[0]:-}"
project_root=""
target=""
source_cache_package_dir=""
explicit_target_kind=""

is_unity_project_root() {
  local root="$1"
  [[ -d "$root/Assets" && -d "$root/Packages" ]] || [[ -f "$root/ProjectSettings/ProjectVersion.txt" ]]
}

candidate_project_root_from_file() {
  local file="$1"
  local rp
  rp="$(realpath "$file")"

  if [[ "$rp" == *"/${EMBEDDED_RELATIVE_FILE}" ]]; then
    printf '%s\n' "${rp%/${EMBEDDED_RELATIVE_FILE}}"
  elif [[ "$rp" == *"/${CACHE_RELATIVE_FILE}" ]]; then
    printf '%s\n' "${rp%/${CACHE_RELATIVE_FILE}}"
  fi
}

classify_target_file() {
  local file="$1"
  local rp
  rp="$(realpath "$file")"

  if [[ "$(basename "$rp")" != "OVRProjectConfig.cs" ]]; then
    echo "ERROR: Target is not OVRProjectConfig.cs: $rp" >&2
    exit 1
  fi

  if [[ "$rp" == *"/${EMBEDDED_RELATIVE_FILE}" ]]; then
    explicit_target_kind="embedded"
    project_root="${rp%/${EMBEDDED_RELATIVE_FILE}}"
    target="$rp"
  elif [[ "$rp" == *"/${CACHE_RELATIVE_FILE}" ]]; then
    explicit_target_kind="cache"
    project_root="${rp%/${CACHE_RELATIVE_FILE}}"
    source_cache_package_dir="${project_root}/${CACHE_PACKAGE_DIR}"
    target="$rp"
  else
    # Keep exact-file patching possible for unusual layouts, but do not try to
    # embed because we cannot safely infer the package root from an arbitrary file.
    explicit_target_kind="custom"
    project_root="$repo_root"
    target="$rp"
    if [[ "$EMBED_PACKAGE" -eq 1 ]]; then
      echo "WARNING: Exact target is not under the expected PackageCache or embedded package path." >&2
      echo "         Patching this exact file directly instead of embedding first:" >&2
      echo "         $rp" >&2
      EMBED_PACKAGE=0
    fi
  fi
}

find_candidate_files_in() {
  local root="$1"
  {
    find "$root" -path "*/${EMBEDDED_RELATIVE_FILE}" -type f 2>/dev/null || true
    find "$root" -path "*/${CACHE_RELATIVE_FILE}" -type f 2>/dev/null || true
  } | sort -u
}

choose_project_root_from_candidates() {
  local search_root="$1"
  local -a files roots preferred

  mapfile -t files < <(find_candidate_files_in "$search_root")

  if [[ "${#files[@]}" -eq 0 ]]; then
    return 1
  fi

  mapfile -t roots < <(
    for file in "${files[@]}"; do
      candidate_project_root_from_file "$file"
    done | sed '/^$/d' | sort -u
  )

  if [[ "${#roots[@]}" -eq 1 ]]; then
    project_root="${roots[0]}"
    return 0
  fi

  preferred=()
  for root in "${roots[@]}"; do
    if [[ "$root" == *"/Source/CUTeR Arm Simulator v2" ]]; then
      preferred+=("$root")
    fi
  done

  if [[ "${#preferred[@]}" -eq 1 ]]; then
    project_root="${preferred[0]}"
    return 0
  fi

  echo "ERROR: Found multiple Unity project candidates:" >&2
  printf '  %s\n' "${roots[@]}" >&2
  echo "Pass the Unity project root explicitly, for example:" >&2
  echo "  $0 \"Source/CUTeR Arm Simulator v2\"" >&2
  exit 1
}

resolve_project_root() {
  if [[ -n "$input" ]]; then
    if [[ -f "$input" ]]; then
      classify_target_file "$input"
      return 0
    fi

    if [[ -d "$input" ]]; then
      local rp
      rp="$(realpath "$input")"

      if [[ -f "$rp/${CACHE_RELATIVE_FILE}" || -f "$rp/${EMBEDDED_RELATIVE_FILE}" ]] || is_unity_project_root "$rp"; then
        project_root="$rp"
        return 0
      fi

      if choose_project_root_from_candidates "$rp"; then
        return 0
      fi

      echo "ERROR: Could not find Meta XR Core package files under: $rp" >&2
      echo "Open the Unity project once so Package Manager creates Library/PackageCache, then rerun." >&2
      exit 1
    fi

    echo "ERROR: Path does not exist: $input" >&2
    exit 1
  fi

  # No input: prefer the current git root if it is a Unity project, otherwise
  # search under the git root/current directory like the previous script did.
  if is_unity_project_root "$repo_root" || [[ -f "$repo_root/${CACHE_RELATIVE_FILE}" || -f "$repo_root/${EMBEDDED_RELATIVE_FILE}" ]]; then
    project_root="$(realpath "$repo_root")"
    return 0
  fi

  if choose_project_root_from_candidates "$repo_root"; then
    return 0
  fi

  if [[ "$repo_root" != "$PWD" ]] && choose_project_root_from_candidates "$PWD"; then
    return 0
  fi

  echo "ERROR: Could not find ${CACHE_RELATIVE_FILE} or ${EMBEDDED_RELATIVE_FILE} under:" >&2
  echo "  $repo_root" >&2
  echo "Open the Unity project once so Package Manager creates Library/PackageCache, then rerun." >&2
  exit 1
}

backup_existing_embedded_package() {
  local embedded_dir="$1"
  local backup_root="$2"
  local timestamp="$3"
  local backup_dir="${backup_root}/${PACKAGE_ID}.embedded.${timestamp}"

  mkdir -p "$backup_root"
  cp -a "$embedded_dir" "$backup_dir"
  echo "Existing embedded package backed up to:"
  echo "  $backup_dir"
}

ensure_embedded_package() {
  local root="$1"
  local embedded_dir="${root}/${EMBEDDED_PACKAGE_DIR}"
  local embedded_file="${root}/${EMBEDDED_RELATIVE_FILE}"
  local cache_dir="${source_cache_package_dir:-${root}/${CACHE_PACKAGE_DIR}}"
  local backup_root="${root}/Temp/MetaXRLinuxPatchBackups"
  local timestamp
  timestamp="$(date +%Y%m%d-%H%M%S)"

  if [[ -d "$embedded_dir" && "$FORCE_REEMBED" -eq 1 ]]; then
    echo "--force-reembed requested. Replacing existing embedded Meta XR Core package."
    backup_existing_embedded_package "$embedded_dir" "$backup_root" "$timestamp"
    rm -rf "$embedded_dir"
  fi

  if [[ -d "$embedded_dir" ]]; then
    echo "Embedded Meta XR Core package already exists; leaving it in place:"
    echo "  $embedded_dir"
  else
    if [[ ! -d "$cache_dir" ]]; then
      echo "ERROR: Cannot embed Meta XR Core because PackageCache source was not found:" >&2
      echo "  $cache_dir" >&2
      echo "Open the Unity project once so Package Manager creates Library/PackageCache, then rerun." >&2
      echo "If Unity Asset Store auth is still broken, install Meta XR Core through the Meta npm scoped registry first." >&2
      exit 1
    fi

    if [[ ! -f "$cache_dir/package.json" ]]; then
      echo "ERROR: PackageCache source does not look like a UPM package; package.json missing:" >&2
      echo "  $cache_dir/package.json" >&2
      exit 1
    fi

    mkdir -p "${root}/Packages"
    cp -a "$cache_dir" "$embedded_dir"
    chmod -R u+rwX "$embedded_dir"

    echo "Embedded Meta XR Core package created:"
    echo "  $embedded_dir"
  fi

  if [[ ! -f "$embedded_file" ]]; then
    echo "ERROR: Embedded OVRProjectConfig.cs was not found:" >&2
    echo "  $embedded_file" >&2
    exit 1
  fi

  # This package.json check is informational only. It helps catch accidental
  # wrong-folder copies without blocking if Meta changes formatting later.
  if [[ -f "$embedded_dir/package.json" ]] && ! grep -q '"name"[[:space:]]*:[[:space:]]*"com\.meta\.xr\.sdk\.core"' "$embedded_dir/package.json"; then
    echo "WARNING: Embedded package.json does not appear to name ${PACKAGE_ID}:" >&2
    echo "  $embedded_dir/package.json" >&2
  fi

  target="$embedded_file"
}

resolve_project_root

if [[ -z "$project_root" ]]; then
  echo "ERROR: Internal error: project_root was not resolved." >&2
  exit 1
fi

project_root="$(realpath "$project_root")"

if [[ "$EMBED_PACKAGE" -eq 1 ]]; then
  # If the user gave an exact embedded file, it is already embedded. Otherwise,
  # make or reuse Packages/com.meta.xr.sdk.core and patch that file.
  if [[ "$explicit_target_kind" == "embedded" ]]; then
    target="$(realpath "$target")"
  else
    ensure_embedded_package "$project_root"
  fi
else
  if [[ -z "$target" ]]; then
    if [[ -f "$project_root/${CACHE_RELATIVE_FILE}" ]]; then
      target="$project_root/${CACHE_RELATIVE_FILE}"
    elif [[ -f "$project_root/${EMBEDDED_RELATIVE_FILE}" ]]; then
      target="$project_root/${EMBEDDED_RELATIVE_FILE}"
    else
      echo "ERROR: Could not find a file to patch:" >&2
      echo "  $project_root/${CACHE_RELATIVE_FILE}" >&2
      echo "  $project_root/${EMBEDDED_RELATIVE_FILE}" >&2
      exit 1
    fi
  fi
fi

if [[ "$(basename "$target")" != "OVRProjectConfig.cs" ]]; then
  echo "ERROR: Target is not OVRProjectConfig.cs: $target" >&2
  exit 1
fi

if [[ ! -f "$target" ]]; then
  echo "ERROR: Target file does not exist: $target" >&2
  exit 1
fi

if [[ "$target" == *"/${CACHE_RELATIVE_FILE}" && "$EMBED_PACKAGE" -eq 0 ]]; then
  echo "WARNING: Patching PackageCache directly. This can be lost when Unity regenerates Library/PackageCache." >&2
  echo "         Run without --no-embed to patch a persistent embedded package copy instead." >&2
  echo
fi

echo "Unity project root:"
echo "  $project_root"
echo "Patch mode:"
if [[ "$target" == *"/${EMBEDDED_RELATIVE_FILE}" ]]; then
  echo "  embedded package"
else
  echo "  direct file / PackageCache"
fi
echo "Target file:"
echo "  $target"
echo

# Remove stale same-directory backups from earlier manual patch attempts.
# Leaving backup files inside PackageCache can produce Unity warnings such as:
# "Asset Packages/com.meta.xr.sdk.core/... has no meta file, but it's in an immutable folder."
# We remove them from embedded packages too, because .bak C# files inside Editor/
# can also be noisy/confusing in Unity's asset pipeline.
stale_count=0
while IFS= read -r -d '' stale_file; do
  rm -f "$stale_file"
  stale_count=$((stale_count + 1))
done < <(find "$(dirname "$target")" -maxdepth 1 -type f -name 'OVRProjectConfig.cs.bak*' -print0 2>/dev/null)

if [[ "$stale_count" -gt 0 ]]; then
  echo "Removed $stale_count stale same-directory backup file(s)."
  echo
fi

export TARGET_OVR_PROJECT_CONFIG="$target"
export UNITY_PROJECT_ROOT="$project_root"

python3 <<'PY'
from pathlib import Path
from datetime import datetime
import os
import re
import sys

target = Path(os.environ["TARGET_OVR_PROJECT_CONFIG"])
project_root = Path(os.environ["UNITY_PROJECT_ROOT"])

text = target.read_text()
original_text = text

block_re = re.compile(
    r"public\s+static\s+int\[\]\s+horizonOsSdkVersions\s*=\s*Enumerable\.Range\(.*?\.ToArray\(\);",
    re.S,
)

match = block_re.search(text)

if not match:
    idx = text.find("horizonOsSdkVersions")
    if idx >= 0:
        start = max(0, idx - 400)
        end = min(len(text), idx + 900)
        print("Found 'horizonOsSdkVersions', but not in the expected format.")
        print("Context:")
        print(text[start:end])
    else:
        print("Could not find 'horizonOsSdkVersions' in target file.")
    sys.exit(1)

block = match.group(0)
patched = block

# Expected original:
#   Enumerable.Range(minSdkVersion, finalSdkVersion - minSdkVersion + 1)
patched = re.sub(
    r"Enumerable\.Range\(\s*minSdkVersion\s*,\s*finalSdkVersion\s*-\s*minSdkVersion\s*\+\s*1\s*\)",
    "Enumerable.Range(minSdkVersion, System.Math.Max(1, finalSdkVersion - minSdkVersion + 1))",
    patched,
    count=1,
)

# Expected original:
#   Enumerable.Range(version2Start, currentSdkVersion - version2Start + 1)
patched = re.sub(
    r"Enumerable\.Range\(\s*version2Start\s*,\s*currentSdkVersion\s*-\s*version2Start\s*\+\s*1\s*\)",
    "Enumerable.Range(version2Start, System.Math.Max(1, currentSdkVersion - version2Start + 1))",
    patched,
    count=1,
)

# Repair a previous accidental local patch where the Concat range used minSdkVersion
# instead of version2Start.
patched = re.sub(
    r"\.Concat\(\s*Enumerable\.Range\(\s*minSdkVersion\s*,\s*System\.Math\.Max\(\s*1\s*,\s*currentSdkVersion\s*-\s*minSdkVersion\s*\+\s*1\s*\)\s*\)\s*\)",
    ".Concat(Enumerable.Range(version2Start, System.Math.Max(1, currentSdkVersion - version2Start + 1)))",
    patched,
    count=1,
)

patched = re.sub(
    r"\.Concat\(\s*Enumerable\.Range\(\s*minSdkVersion\s*,\s*currentSdkVersion\s*-\s*minSdkVersion\s*\+\s*1\s*\)\s*\)",
    ".Concat(Enumerable.Range(version2Start, System.Math.Max(1, currentSdkVersion - version2Start + 1)))",
    patched,
    count=1,
)

wanted_1 = "Enumerable.Range(minSdkVersion, System.Math.Max(1, finalSdkVersion - minSdkVersion + 1))"
wanted_2 = "Enumerable.Range(version2Start, System.Math.Max(1, currentSdkVersion - version2Start + 1))"

if wanted_1 in patched and wanted_2 in patched and patched == block:
    print("Already patched. No changes made.")
    sys.exit(0)

if wanted_1 not in patched or wanted_2 not in patched:
    print("ERROR: The horizonOsSdkVersions block did not match the expected patch shape.")
    print()
    print("Current block:")
    print(block)
    print()
    print("Attempted patched block:")
    print(patched)
    sys.exit(1)

new_text = text[:match.start()] + patched + text[match.end():]

backup_dir = project_root / "Temp" / "MetaXRLinuxPatchBackups"
backup_dir.mkdir(parents=True, exist_ok=True)

timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
mode = "embedded" if "/Packages/com.meta.xr.sdk.core/" in str(target) else "direct"
backup_path = backup_dir / f"OVRProjectConfig.cs.{mode}.{timestamp}.bak"
backup_path.write_text(original_text)

target.write_text(new_text)

print("Patched OVRProjectConfig.cs successfully.")
print("Backup written outside package/cache folder:")
print(f"  {backup_path}")
print()
print("Patched horizonOsSdkVersions block:")
print(patched)
PY

echo
if [[ "$target" == *"/${EMBEDDED_RELATIVE_FILE}" ]]; then
  echo "Done. Patched embedded Meta XR Core package:"
  echo "  ${project_root}/${EMBEDDED_PACKAGE_DIR}"
  echo "Unity should now use this project-local package instead of the PackageCache copy."
  echo "If Unity later updates Meta XR Core and you want a fresh embedded copy, rerun with --force-reembed."
else
  echo "Done. Reopen Unity after this patch. If Unity regenerates Library/PackageCache, rerun this script."
fi
