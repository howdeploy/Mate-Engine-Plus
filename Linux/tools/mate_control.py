#!/usr/bin/env python3
"""Local, acknowledged commands for one known Mate Engine Plus installation."""

import argparse
import json
import os
from pathlib import Path
import socket
import stat
import subprocess
import sys
import time
import uuid

ACTIONS = {"status", "dances.list", "dance.play", "dance.stop", "dance.next", "dance.previous", "hide.edge",
           "portrait.set", "seat.dock", "seat.window", "move", "quit", "launch"}


def players(player_dir):
    executable = (player_dir / "MateEngineX.x86_64").resolve(strict=True)
    found = []
    for process in Path("/proc").glob("[0-9]*"):
        try:
            if process.stat().st_uid != os.getuid() or (process / "exe").resolve() != executable:
                continue
            arguments = (process / "cmdline").read_bytes().decode().split("\0")
            instance = int(arguments[arguments.index("--instance") + 1]) if "--instance" in arguments else 0
            found.append((int(process.name), instance))
        except (OSError, ValueError, IndexError, UnicodeError):
            continue
    return found


def request(pid, payload):
    runtime = Path(f"/run/user/{os.getuid()}")
    path = runtime / f"mateengine-voice-{pid}.sock"
    try:
        info = path.lstat()
    except FileNotFoundError:
        raise RuntimeError("MateEngine voice bridge is not available; the updated application must be built and restarted") from None
    if not stat.S_ISSOCK(info.st_mode) or info.st_uid != os.getuid() or info.st_mode & 0o077:
        raise RuntimeError("MateEngine voice socket is not private")
    identity = uuid.uuid4().hex
    message = dict({"x": 0.5, "y": 0.5, "fraction": 0.5}, **payload)
    message.update(version=1, id=identity, deadlineUtc=time.time() + 5)
    encoded = json.dumps(message, ensure_ascii=False, allow_nan=False).encode() + b"\n"
    if len(encoded) > 8192:
        raise ValueError("MateEngine command is too large")
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
        connection.settimeout(8)
        connection.connect(str(path))
        connection.sendall(encoded)
        data = bytearray()
        while b"\n" not in data:
            chunk = connection.recv(4096)
            if not chunk:
                raise RuntimeError("MateEngine closed the connection without acknowledgement; command was not retried")
            data.extend(chunk)
            if len(data) > 262144:
                raise RuntimeError("MateEngine response is too large")
    result = json.loads(data.split(b"\n", 1)[0])
    if result.get("version") != 1 or result.get("id") != identity:
        raise RuntimeError("MateEngine response does not match the request")
    if result.get("ok") is not True:
        raise RuntimeError(result.get("error") or "MateEngine rejected the command")
    if result.get("pid") != pid:
        raise RuntimeError("MateEngine response belongs to a different process")
    return result


def control(player_dir, payload, *, instance=None, config_home=None, all_instances=False):
    player_dir = Path(player_dir).resolve(strict=True)
    if not isinstance(payload, dict) or payload.get("command") not in ACTIONS:
        raise ValueError("Unknown MateEngine command")
    running = players(player_dir)
    if payload["command"] == "launch":
        if running:
            return {"already_running": True, "pids": [pid for pid, _ in running]}
        launcher = player_dir / "launch.sh"
        if not launcher.is_file() or not os.access(launcher, os.X_OK):
            raise RuntimeError("MateEngine launcher is missing or is not executable")
        args = ["systemd-run", "--user", "--collect", "--quiet", "--service-type=exec",
                "--unit=alice-pc-mateengine-" + uuid.uuid4().hex]
        if config_home:
            args.append("--setenv=XDG_CONFIG_HOME=" + str(Path(config_home).resolve(strict=True)))
        args.extend(["--", str(launcher)])
        if instance is not None:
            args.extend(["--instance", str(instance)])
        subprocess.run(args, check=True, capture_output=True, timeout=10)
        return {"launch_requested": True}
    if all_instances and payload["command"] != "quit":
        raise ValueError("Only quit may target all instances")
    if instance is not None:
        running = [(pid, index) for pid, index in running if index == instance]
    if not running:
        if payload["command"] == "quit":
            return {"already_closed": True}
        raise RuntimeError("MateEngine is not running")
    if not all_instances and len(running) != 1:
        raise RuntimeError("Several MateEngine instances are running; select --instance explicitly")
    results = [request(pid, payload) for pid, _ in running]
    return {"instances": results}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player-dir", type=Path, required=True)
    parser.add_argument("--config-home", type=Path)
    parser.add_argument("--instance", type=int)
    parser.add_argument("--all-instances", action="store_true")
    parser.add_argument("--command", choices=sorted(ACTIONS))
    args = parser.parse_args()
    if args.instance is not None and args.instance < 0:
        parser.error("--instance must be non-negative")
    payload = {"command": args.command} if args.command else json.loads(sys.stdin.buffer.read(8193))
    print(json.dumps(control(args.player_dir, payload, instance=args.instance,
                             config_home=args.config_home, all_instances=args.all_instances), ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except (OSError, RuntimeError, ValueError, subprocess.SubprocessError) as error:
        print(json.dumps({"error": str(error)}, ensure_ascii=False), file=sys.stderr)
        raise SystemExit(1)
