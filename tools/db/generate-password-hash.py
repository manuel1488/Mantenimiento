#!/usr/bin/env python3
"""
Genera un hash de contraseña compatible con ASP.NET Core Identity v3.
Algoritmo: PBKDF2-HMACSHA256, 100 000 iteraciones, salt aleatorio de 16 bytes.

Uso:
    python3 generate-password-hash.py
    python3 generate-password-hash.py "MiContraseña@123"
"""

import hashlib
import os
import struct
import base64
import sys


def generate_hash(password: str) -> str:
    salt = os.urandom(16)
    dk = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, 100_000, dklen=32)

    # Formato Identity v3:
    # [0x01] | prf(4 bytes BE) | iterations(4 bytes BE) | saltLen(4 bytes BE) | salt | subkey
    buf = bytearray([0x01]) + struct.pack(">III", 1, 100_000, len(salt)) + salt + dk
    return base64.b64encode(bytes(buf)).decode("ascii")


def main():
    if len(sys.argv) > 1:
        password = sys.argv[1]
    else:
        import getpass
        password = getpass.getpass("Password: ")
        confirm  = getpass.getpass("Confirm:  ")
        if password != confirm:
            print("ERROR: Las contraseñas no coinciden.")
            sys.exit(1)

    print(generate_hash(password))


if __name__ == "__main__":
    main()
