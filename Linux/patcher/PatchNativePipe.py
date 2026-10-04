#!/usr/bin/env python3
"""Repair the failed-connect FD leak in the exact X3.2.0_6 native plugin.

The shipped x86-64 ELF has equal file/virtual offsets in .text. Replace only
the 27-byte guard in NamedPipeClientUnix::close(): isConnected() -> fd >= 0.
The original libc close call and fd/connected resets remain untouched.
No code is loaded or executed. Unknown binaries are rejected by full SHA256.
"""
import argparse
import hashlib
from pathlib import Path
import sys

ORIGINAL_SHA256 = "971b8e553b8d069318f7b18383f2b610ffcee216b8522abb4b6b38de09cc81e0"
OFFSET = 0x19EE
OLD_GUARD = bytes.fromhex(
    "48 8b 45 f8 48 8b 00 48 83 c0 10 48 8b 00 "
    "48 8b 55 f8 48 89 d7 ff d0 84 c0 74 0e"
)
# mov rax,[rbp-8]; cmp dword ptr [rax+8],0; js 0x1a17; pad to 27 bytes.
NEW_GUARD = bytes.fromhex("48 8b 45 f8 83 78 08 00 78 1f") + b"\x90" * 17


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", nargs="?", type=Path)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true")
    mode.add_argument("--restore", action="store_true")
    args = parser.parse_args()
    if args.check != (args.output is None):
        parser.error("use --check without output, or supply a fresh output path")
    if args.output is not None and args.source.resolve() == args.output.resolve():
        parser.error("refusing to overwrite the input plugin")

    data = args.source.read_bytes()
    guard = data[OFFSET:OFFSET + len(OLD_GUARD)]
    if guard not in (OLD_GUARD, NEW_GUARD):
        raise ValueError("unknown NativeNamedPipe close guard")
    original = data[:OFFSET] + OLD_GUARD + data[OFFSET + len(OLD_GUARD):]
    if hashlib.sha256(original).hexdigest() != ORIGINAL_SHA256:
        raise ValueError("plugin is not the reviewed X3.2.0_6 binary")
    if args.check:
        print("NativeNamedPipe: " + ("original (leaks FDs)" if guard == OLD_GUARD else "FD fix present"))
        return

    result = original if args.restore else (
        original[:OFFSET] + NEW_GUARD + original[OFFSET + len(OLD_GUARD):]
    )
    with args.output.open("xb") as output:
        output.write(result)
    print(hashlib.sha256(result).hexdigest() + "  " + str(args.output))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError) as error:
        sys.exit("NativeNamedPipe patch refused: " + str(error))
