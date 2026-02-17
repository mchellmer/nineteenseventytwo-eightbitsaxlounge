#!/usr/bin/env python3
"""Locate tokens.db files in the repo and print their `tokens` table rows.

Usage:
  python ui/scripts/check_tokens_db.py

This script does not modify the DB; it only reads and prints rows.
"""
import os
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def find_dbs(root: Path):
    for dirpath, dirs, files in os.walk(root):
        for f in files:
            if f == "tokens.db":
                yield Path(dirpath) / f


def inspect_db(path: Path):
    print("\nFound tokens.db at:", path)
    try:
        conn = sqlite3.connect(path)
        conn.row_factory = sqlite3.Row
        cur = conn.cursor()
        cur.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [r[0] for r in cur.fetchall()]
        print("Tables:", tables)
        if "tokens" in tables:
            cur.execute("SELECT user_id, token, refresh FROM tokens")
            rows = cur.fetchall()
            if not rows:
                print("tokens table is empty")
            else:
                print(f"tokens rows ({len(rows)}):")
                for r in rows:
                    # Output full token and refresh values
                    print(f"  user_id={r['user_id']} token={r['token']} refresh={r['refresh']}")
        else:
            print("No 'tokens' table present in this DB.")
        conn.close()
    except Exception as e:
        print("Error reading DB:", e)


def main():
    found = list(find_dbs(ROOT))
    if not found:
        print("No tokens.db files found under", ROOT)
        return
    for db in found:
        inspect_db(db)

if __name__ == '__main__':
    main()
