"""Genera configuracion propia sin imprimir secretos ni sobrescribir archivos."""
from pathlib import Path
import secrets


def main():
    destination = Path(__file__).resolve().parent / ".env"
    values = {
        "IP_API": "0.0.0.0",
        "API_PORT": "8000",
        "API_ACCESS_KEY": secrets.token_hex(32),
        "DB_APP_PASSWORD": "A" + secrets.token_hex(16),
        "MARIADB_ROOT_PASSWORD": "A" + secrets.token_hex(24),
        "ORACLE_ADMIN_PASSWORD": "A" + secrets.token_hex(24),
    }
    try:
        with destination.open("x", encoding="utf-8") as output:
            for name, value in values.items():
                print(f"{name}={value}", file=output)
    except FileExistsError:
        raise SystemExit("Ya existe .env. No se ha sobrescrito.")
    print("Creado .env con claves nuevas. No lo compartas ni lo subas a Git.")


if __name__ == "__main__":
    main()
