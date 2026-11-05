"""Database/API regression tests using Python's standard library.

Run `dotnet build MovieMind.slnx` first, then `python3 -m unittest discover -s tests -v`.
Every test uses an isolated SQLite database and poster directory.
"""

import base64
import json
import os
from pathlib import Path
import socket
import sqlite3
import subprocess
import tempfile
import time
import unittest
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
import uuid


ROOT = Path(__file__).resolve().parents[1]
PNG = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/l1sAAAAASUVORK5CYII="
)


class CatalogTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="moviemind-test-")
        self.addCleanup(self.directory.cleanup)
        self.servers = []
        self.addCleanup(self.stop_servers)
        self.base = self.start_server("MovieMind.Web")

    def stop_servers(self):
        for process, log in self.servers:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
            log.close()
        self.servers.clear()

    def start_server(self, project):
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        base = f"http://127.0.0.1:{port}"
        env = dict(os.environ, ASPNETCORE_ENVIRONMENT="Development",
                   Database__Provider="Sqlite", Storage__DataDirectory=self.directory.name)
        log = tempfile.TemporaryFile(mode="w+")
        process = subprocess.Popen(
            ["dotnet", "run", "--project", project, "--no-build", "--no-launch-profile", "--urls", base],
            cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT,
        )
        self.servers.append((process, log))
        deadline = time.monotonic() + 25
        while time.monotonic() < deadline:
            try:
                with urlopen(base + "/api/films", timeout=1) as response:
                    if response.status == 200:
                        return base
            except (URLError, TimeoutError):
                pass
            if process.poll() is not None:
                break
            time.sleep(0.1)
        log.seek(0)
        self.fail("Server did not start:\n" + log.read())

    def request(self, path, method="GET", data=None, headers=None, base=None):
        headers = headers or {}
        if isinstance(data, dict):
            data = json.dumps(data).encode()
            headers["Content-Type"] = "application/json"
        request = Request((base or self.base) + path, data=data, headers=headers, method=method)
        try:
            response = urlopen(request, timeout=10)
        except HTTPError as error:
            response = error
        with response:
            body = response.read()
            if "json" in response.headers.get("Content-Type", ""):
                body = json.loads(body)
            return response.status, body

    def film(self):
        return dict(
            id=str(uuid.uuid4()), title="Database round trip", originalTitle="Original title",
            year=2024, duration=123, ageRating="PG-13", rating=8.7,
            genres=["Drama", "Sci-Fi"], director="Director", composer="Composer",
            writers=["Writer One", "Writer Two"],
            cast=[dict(name="Actor One", role="Captain"), dict(name="Actor Two", role="Pilot")],
            synopsis="Full synopsis " + "x" * 2200, awards="Best fictional picture",
            country="Moldova", language="Romanian", trailerUrl="https://example.com/trailer",
            poster="", art=dict(palette=4, motif="stars"),
        )

    def save(self, film):
        status, body = self.request("/api/films/" + film["id"], "PUT", film)
        self.assertEqual(status, 200, body)
        return body

    def upload(self, content=PNG, name="poster.png", content_type="image/png"):
        boundary = "moviemind-test-boundary"
        data = (f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{name}"\r\n'
                f"Content-Type: {content_type}\r\n\r\n").encode() + content + f"\r\n--{boundary}--\r\n".encode()
        return self.request("/api/posters", "POST", data,
                            {"Content-Type": f"multipart/form-data; boundary={boundary}"})

    def test_all_fields_persist_across_server_restart(self):
        film = self.film()
        status, poster = self.upload()
        self.assertEqual(status, 200, poster)
        film["poster"] = poster["url"]
        saved = self.save(film)
        self.assertGreater(saved["createdAt"], 0)
        for key, value in film.items():
            self.assertEqual(saved[key], value, key)
        self.stop_servers()
        self.base = self.start_server("MovieMind.Web")
        status, loaded = self.request("/api/films/" + film["id"])
        self.assertEqual(status, 200)
        self.assertEqual(saved, loaded)
        self.assertEqual(self.request(film["poster"]), (200, PNG))
        with sqlite3.connect(Path(self.directory.name) / "moviemind.db") as db:
            self.assertEqual(db.execute('SELECT Title, CatalogRating FROM Movies').fetchone(),
                             (film["title"], 8.7))
            self.assertEqual(db.execute('SELECT COUNT(*) FROM MovieGenres').fetchone()[0], 2)
            self.assertEqual(db.execute('SELECT COUNT(*) FROM MovieActors').fetchone()[0], 2)

    def test_edits_preserve_cast_order_and_replace_relationships(self):
        film = self.film()
        self.save(film)
        film["cast"] = [dict(name="Actor Two", role="Lead"), dict(name="Actor Three", role="Support")]
        film["genres"] = ["Comedy"]
        film["writers"] = []
        film["rating"] = 0
        film["synopsis"] = "Edited"
        saved = self.save(film)
        for key, value in film.items():
            self.assertEqual(saved[key], value, key)
        self.assertEqual(self.request("/api/films/" + film["id"])[1], saved)
        self.assertEqual(len(self.request("/api/films")[1]), 1)

    def test_invalid_fields_never_write_a_film(self):
        for changes in [dict(title=" "), dict(year=1700), dict(duration=1.5),
                        dict(rating=11), dict(trailerUrl="javascript:alert(1)"),
                        dict(genres=None), dict(writers=[None]), dict(cast=[None]),
                        dict(art=None), dict(poster="/media/posters/missing.jpg"),
                        dict(cast=[dict(name="Same", role="A"), dict(name="Same", role="B")])]:
            with self.subTest(changes=changes):
                film = self.film()
                film.update(changes)
                status, body = self.request("/api/films/" + film["id"], "PUT", film)
                self.assertEqual(status, 400, body)
        self.assertEqual(self.request("/api/films")[1], [])

    def test_upload_rejects_wrong_type_and_oversize(self):
        for content, name, content_type in [(b"not an image", "fake.jpg", "image/jpeg"),
                                          (b"<svg></svg>", "test.svg", "image/svg+xml")]:
            self.assertEqual(self.upload(content, name, content_type)[0], 400)
        self.assertEqual(self.upload(PNG + b"x" * (10 * 1024 * 1024))[0], 413)

    def test_missing_uploaded_poster_cannot_be_saved(self):
        film = self.film()
        film["poster"] = "/media/posters/" + "a" * 32 + ".jpg"
        self.assertEqual(self.request("/api/films/" + film["id"], "PUT", film)[0], 400)

    def test_delete_removes_join_rows_and_returns_not_found(self):
        film = self.film()
        self.save(film)
        self.assertEqual(self.request("/api/films/" + film["id"], "DELETE")[0], 204)
        self.assertEqual(self.request("/api/films/" + film["id"])[0], 404)
        self.assertEqual(self.request("/api/films")[1], [])
        with sqlite3.connect(Path(self.directory.name) / "moviemind.db") as db:
            self.assertEqual(db.execute("SELECT COUNT(*) FROM MovieActors").fetchone()[0], 0)
            self.assertEqual(db.execute("SELECT COUNT(*) FROM MovieGenres").fetchone()[0], 0)

    def test_api_and_frontend_hosts_share_database_and_posters(self):
        film = self.film()
        film["poster"] = self.upload()[1]["url"]
        saved = self.save(film)
        api_base = self.start_server("MovieMind/MovieMind.API.csproj")
        self.assertEqual(self.request("/api/films/" + film["id"], base=api_base), (200, saved))
        self.assertEqual(self.request(film["poster"], base=api_base), (200, PNG))
        # The controllers already present on Dev must keep seeing the shared catalog.
        status, legacy_movie = self.request("/api/movies/" + film["id"], base=api_base)
        self.assertEqual(status, 200, legacy_movie)
        self.assertEqual(legacy_movie["title"], film["title"])
        for endpoint, expected_count in [("genres", 2), ("actors", 2)]:
            status, items = self.request("/api/" + endpoint, base=api_base)
            self.assertEqual(status, 200, items)
            self.assertEqual(len(items), expected_count)
        status, schema = self.request("/openapi/v1.json", base=api_base)
        self.assertEqual(status, 200, schema)
        self.assertIn("/api/films", schema["paths"])

    def test_cross_origin_mutations_are_rejected(self):
        film = self.film()
        self.assertEqual(self.request("/api/films/" + film["id"], "PUT", film,
                                     {"Origin": "https://another-site.example"})[0], 403)


if __name__ == "__main__":
    unittest.main()
