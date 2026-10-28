import json
from pathlib import Path
import os
import stat
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
import mate_control


class ControlTests(unittest.TestCase):
    def test_ambiguous_instances_are_not_broadcast(self):
        with tempfile.TemporaryDirectory() as directory, \
                patch.object(mate_control, "players", return_value=[(101, 0), (102, 1)]), \
                patch.object(mate_control, "request") as request:
            with self.assertRaisesRegex(RuntimeError, "Several"):
                mate_control.control(directory, {"command": "dance.play"})
            request.assert_not_called()
            mate_control.control(directory, {"command": "dance.play"}, instance=1)
            request.assert_called_once_with(102, {"command": "dance.play"})

    def test_launch_does_not_duplicate_an_existing_pet(self):
        with tempfile.TemporaryDirectory() as directory, \
                patch.object(mate_control, "players", return_value=[(101, 0)]), \
                patch.object(mate_control.subprocess, "run") as run:
            self.assertTrue(mate_control.control(directory, {"command": "launch"})["already_running"])
            run.assert_not_called()

    def test_socket_permissions_are_checked_before_sending_anything(self):
        for mode in (stat.S_IFSOCK | 0o666, stat.S_IFREG | 0o600):
            with patch.object(Path, "lstat", return_value=SimpleNamespace(st_uid=os.getuid(), st_mode=mode)), \
                    patch.object(mate_control.socket, "socket") as connect:
                with self.assertRaisesRegex(RuntimeError, "not private"):
                    mate_control.request(101, {"command": "dance.play"})
                connect.assert_not_called()

    def test_request_has_a_deadline_and_rejects_a_wrong_ack_without_retry(self):
        connection = Mock()
        connection.__enter__ = Mock(return_value=connection)
        connection.__exit__ = Mock(return_value=False)
        connection.recv.return_value = b'{"version":1,"id":"wrong","pid":101,"ok":true}\n'
        with patch.object(Path, "lstat", return_value=SimpleNamespace(st_uid=os.getuid(), st_mode=stat.S_IFSOCK | 0o600)), \
                patch.object(mate_control.socket, "socket", return_value=connection) as connect, \
                patch.object(mate_control.time, "time", return_value=100):
            with self.assertRaisesRegex(RuntimeError, "does not match"):
                mate_control.request(101, {"command": "seat.dock"})
            sent = json.loads(connection.sendall.call_args.args[0])
            self.assertEqual(sent["deadlineUtc"], 105)
            self.assertEqual(sent["fraction"], 0.5)
            self.assertEqual(connect.call_count, 1)
            connection.sendall.assert_called_once()


if __name__ == "__main__":
    unittest.main()
