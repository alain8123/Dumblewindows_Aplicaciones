"""API de estudiantes de Hogwarts.

Este modulo expone una API FastAPI para consultar estudiantes historicos y
gestionar matriculas nuevas.

Arquitectura:
    * MariaDB (Enrollment) contiene unicamente las matriculas nuevas creadas
      por la API.
    * Derby, HSQLDB, H2 y Oracle contienen fuentes historicas separadas por
      casas; se consultan por identificador cuando hace falta.
    * Los CSV de /app/data sirven para busquedas historicas e inicializan las
      tablas historicas vacias.
    * SQLite, en CACHE_PATH, guarda respuestas historicas temporalmente durante
      CACHE_TTL_SECONDS; no es la base principal de estudiantes.

Seguridad:
    Las rutas de consulta y matricula requieren la cabecera X-API-Key.
    La clave esperada se lee de API_ACCESS_KEY y se compara con tiempo
    constante. La clave no debe escribirse en el codigo ni distribuirse con
    aplicaciones cliente.

Rutas publicas:
    /salud y /health informan del estado del servicio. Las rutas en espanol
    son la interfaz documentada; sus alias en ingles se mantienen para
    compatibilidad y se ocultan del esquema OpenAPI.
"""
from __future__ import annotations

import csv
import hmac
import json
import os
import re
import sqlite3
import time
from contextlib import contextmanager
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Any, Iterator

import jaydebeapi
import jpype
import oracledb
from fastapi import Depends, FastAPI, HTTPException, Query, Security
from fastapi.security import APIKeyHeader
from pydantic import BaseModel, ConfigDict, Field
from sqlalchemy import Date, DateTime, Integer, String, create_engine, select
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, sessionmaker

MARIADB_URL = os.getenv("MARIADB_URL", "mysql+pymysql://hogwarts_app:hogwarts_app@mariadb:3306/hogwarts")
# SQLite es independiente de MariaDB: solo almacena respuestas temporales.
CACHE_PATH = Path(os.getenv("CACHE_PATH", "/data/cache.sqlite3"))
CACHE_TTL = timedelta(seconds=int(os.getenv("CACHE_TTL_SECONDS", "86400")))
CSV_DIR = Path(os.getenv("HISTORY_CSV_DIR", "/app/data"))
JDBC = "/opt/jdbc"
JDBC_JARS = [str(path) for path in Path(JDBC).glob("*.jar")]
if not jpype.isJVMStarted():
    jpype.startJVM(classpath=JDBC_JARS)

engine = create_engine(MARIADB_URL, pool_pre_ping=True)
Session = sessionmaker(engine, expire_on_commit=False)

api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)

def require_api_key(key: str | None = Security(api_key_header)) -> None:
    """Exige la clave compartida antes de consultar o modificar estudiantes."""
    expected = os.getenv("API_ACCESS_KEY", "")
    if not expected or not key or not hmac.compare_digest(key, expected):
        raise HTTPException(status_code=401, detail="Clave de API no válida")

class Base(DeclarativeBase):
    """Base SQLAlchemy para las matrículas nuevas de MariaDB."""
    pass

class Enrollment(Base):
    """Matrícula nueva de Secretaría; nunca es una fila histórica."""
    __tablename__ = "enrollments"
    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    student_id: Mapped[str] = mapped_column(String(64), unique=True)
    full_name: Mapped[str] = mapped_column(String(200))
    house: Mapped[str | None] = mapped_column(String(80), nullable=True)
    birth_date: Mapped[date | None] = mapped_column(Date, nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    updated_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

class EnrollmentIn(BaseModel):
    """Campos recibidos al crear o modificar una matrícula."""
    student_id: str = Field(min_length=1, max_length=64)
    full_name: str = Field(min_length=1, max_length=200)
    house: str | None = Field(default=None, max_length=80)
    birth_date: date | None = None

class EnrollmentOut(EnrollmentIn):
    """Matrícula devuelta con su identificador interno de MariaDB."""
    model_config = ConfigDict(from_attributes=True)
    id: int

class StudentOut(BaseModel):
    """Formato común de respuesta para estudiantes de cualquier origen."""
    id: str
    nombre: str
    casa: str
    alias: str | None = None
    especie: str | None = None
    genero: str | None = None
    nacimiento: str | None = None
    fecha_nacimiento: str | None = None
    nacionalidad: str | None = None
    patronus: str | None = None
    patronus_origen: str | None = None
    imagen: str | None = None
    wiki: str | None = None
    origen: str

app = FastAPI(
    title="API de estudiantes de Hogwarts",
    version="2.0.0",
    description=("MariaDB contiene solo matrículas nuevas. Derby, HSQLDB, H2 y Oracle "
                 "son fuentes históricas de consulta; SQLite almacena la caché temporal. "
                 "Las rutas en inglés siguen operativas para clientes anteriores pero no aparecen en /docs."),
)

SOURCES = {
    "Gryffindor": {"csv": "gryffindor.csv", "kind": "derby"},
    "Slytherin": {"csv": "slytherin.csv", "kind": "hsqldb"},
    "Hufflepuff": {"csv": "hufflepuff.csv", "kind": "h2"},
    "Ravenclaw": {"csv": "ravenclaw.csv", "kind": "oracle"},
}


def cache_db() -> sqlite3.Connection:
    """Abre SQLite y prepara la tabla independiente de respuestas cacheadas."""
    CACHE_PATH.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(CACHE_PATH)
    db.row_factory = sqlite3.Row
    db.execute("CREATE TABLE IF NOT EXISTS student_cache (cache_key TEXT PRIMARY KEY, payload TEXT NOT NULL, cached_at TEXT NOT NULL)")
    db.commit()
    return db


def format_birth(value: str | None) -> tuple[str | None, str | None]:
    """Convierte fechas exactas al formato dd/mm/yyyy; respeta fechas inciertas."""
    if not value:
        return None, None
    match = re.search(r"\b(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})\b", value)
    if not match:
        return value, None
    months = {"january": 1, "february": 2, "march": 3, "april": 4, "may": 5, "june": 6,
              "july": 7, "august": 8, "september": 9, "october": 10, "november": 11, "december": 12}
    month = months.get(match.group(2).lower())
    if not month:
        return value, None
    formatted = f"{int(match.group(1)):02d}/{month:02d}/{match.group(3)}"
    return formatted, formatted


def csv_rows(house: str) -> list[dict[str, str]]:
    """Lee el CSV inicial de cada casa para inicialización y listado."""
    path = CSV_DIR / SOURCES[house]["csv"]
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as file:
        rows = list(csv.DictReader(file))
    for row in rows:
        if "género" in row and "genero" not in row:
            row["genero"] = row["género"]
    return rows


def jdbc_spec(kind: str) -> tuple[str, str, list[str]]:
    """Elige el controlador JDBC de Derby, HSQLDB o H2."""
    if kind == "derby":
        return "org.apache.derby.jdbc.ClientDriver", os.getenv("DERBY_URL", ""), [f"{JDBC}/derby.jar", f"{JDBC}/derbynet.jar", f"{JDBC}/derbyshared.jar", f"{JDBC}/derbyclient.jar", f"{JDBC}/derbytools.jar"]
    if kind == "hsqldb":
        return "org.hsqldb.jdbc.JDBCDriver", os.getenv("HSQLDB_URL", ""), [f"{JDBC}/hsqldb.jar"]
    return "org.h2.Driver", os.getenv("H2_URL", ""), [f"{JDBC}/h2.jar"]


@contextmanager
def source_connection(kind: str) -> Iterator[Any]:
    """Gestiona la conexión a Oracle Thin o al motor JDBC correspondiente."""
    if kind == "oracle":
        dsn = f"{os.getenv('ORACLE_HOST', 'oracle')}:{os.getenv('ORACLE_PORT', '1521')}/{os.getenv('ORACLE_SERVICE', 'FREEPDB1')}"
        connection = oracledb.connect(user=os.getenv("ORACLE_USER", "hogwarts_app"), password=os.getenv("ORACLE_PASSWORD", "hogwarts_app"), dsn=dsn)
        try:
            yield connection
        finally:
            connection.close()
        return
    driver, url, jars = jdbc_spec(kind)
    credentials = ["APP", "APP"] if kind == "derby" else (["SA", ""] if kind == "hsqldb" else ["SA", ""])
    connection = jaydebeapi.connect(driver, url, credentials)
    try:
        yield connection
    finally:
        connection.close()


def create_history_tables() -> None:
    """Crea y carga tablas históricas vacías durante el arranque inicial."""
    statements = {
        "derby": 'CREATE TABLE students (id VARCHAR(64) PRIMARY KEY, nombre VARCHAR(2000), casa VARCHAR(40), alias VARCHAR(2000), especie VARCHAR(100), genero VARCHAR(30), nacimiento VARCHAR(1000), nacionalidad VARCHAR(2000), patronus VARCHAR(2000), patronus_origen VARCHAR(2000), imagen VARCHAR(1000), wiki VARCHAR(1000))',
        "hsqldb": 'CREATE TABLE students (id VARCHAR(64) PRIMARY KEY, nombre VARCHAR(2000), casa VARCHAR(40), alias VARCHAR(2000), especie VARCHAR(100), genero VARCHAR(30), nacimiento VARCHAR(1000), nacionalidad VARCHAR(2000), patronus VARCHAR(2000), patronus_origen VARCHAR(2000), imagen VARCHAR(1000), wiki VARCHAR(1000))',
        "h2": 'CREATE TABLE students (id VARCHAR(64) PRIMARY KEY, nombre VARCHAR(2000), casa VARCHAR(40), alias VARCHAR(2000), especie VARCHAR(100), genero VARCHAR(30), nacimiento VARCHAR(1000), nacionalidad VARCHAR(2000), patronus VARCHAR(2000), patronus_origen VARCHAR(2000), imagen VARCHAR(1000), wiki VARCHAR(1000))',
        "oracle": 'CREATE TABLE students (id VARCHAR2(64) PRIMARY KEY, nombre VARCHAR2(2000), casa VARCHAR2(40), alias VARCHAR2(2000), especie VARCHAR2(100), genero VARCHAR2(30), nacimiento VARCHAR2(1000), nacionalidad VARCHAR2(2000), patronus VARCHAR2(2000), patronus_origen VARCHAR2(2000), imagen VARCHAR2(1000), wiki VARCHAR2(1000))',
    }
    insert = "INSERT INTO students (id,nombre,casa,alias,especie,genero,nacimiento,nacionalidad,patronus,patronus_origen,imagen,wiki) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)"
    oracle_insert = "INSERT INTO students (id,nombre,casa,alias,especie,genero,nacimiento,nacionalidad,patronus,patronus_origen,imagen,wiki) VALUES (:1,:2,:3,:4,:5,:6,:7,:8,:9,:10,:11,:12)"
    for house, config in SOURCES.items():
        kind = config["kind"]
        try:
            with source_connection(kind) as connection:
                cursor = connection.cursor()
                try:
                    cursor.execute(statements[kind])
                    connection.commit()
                except Exception as error:
                    if "already exists" not in str(error).lower() and "name is already used" not in str(error).lower():
                        raise
                cursor.execute("SELECT COUNT(*) FROM students")
                count = int(cursor.fetchone()[0])
                if count == 0:
                    for row in csv_rows(house):
                        values = [row.get(key) or None for key in ("id", "nombre", "casa", "alias", "especie", "genero", "nacimiento", "nacionalidad", "patronus", "patronus_origen", "imagen", "wiki")]
                        cursor.execute(oracle_insert if kind == "oracle" else insert, values)
                    connection.commit()
                cursor.close()
        except Exception as error:
            print(f"history initialization pending for {house}: {error}", flush=True)


def row_to_student(row: Any, house: str) -> StudentOut:
    """Normaliza una fila histórica y anota la base de datos de origen."""
    values = {key: (None if value is None else str(value)) for key, value in zip(("id", "nombre", "casa", "alias", "especie", "genero", "nacimiento", "nacionalidad", "patronus", "patronus_origen", "imagen", "wiki"), row)}
    nacimiento, fecha = format_birth(values.get("nacimiento"))
    values["nacimiento"] = nacimiento
    values["fecha_nacimiento"] = fecha
    values["origen"] = f"{SOURCES[house]['kind']}:{house}"
    return StudentOut(**values)


def query_history(student_id: str) -> StudentOut | None:
    """Busca por ID en los cuatro motores históricos sin modificarlos."""
    for house, config in SOURCES.items():
        try:
            with source_connection(config["kind"]) as connection:
                cursor = connection.cursor()
                cursor.execute("SELECT id,nombre,casa,alias,especie,genero,nacimiento,nacionalidad,patronus,patronus_origen,imagen,wiki FROM students WHERE id = :1" if config["kind"] == "oracle" else "SELECT id,nombre,casa,alias,especie,genero,nacimiento,nacionalidad,patronus,patronus_origen,imagen,wiki FROM students WHERE id = ?", [student_id])
                row = cursor.fetchone()
                cursor.close()
                if row:
                    return row_to_student(row, house)
        except Exception as error:
            print(f"history query pending for {house}: {error}", flush=True)
    return None


def find_history(name: str | None, house: str | None, limit: int) -> list[StudentOut]:
    """Lista desde los CSV originales, no desde las tablas históricas vivas."""
    houses = [house] if house in SOURCES else list(SOURCES)
    result: list[StudentOut] = []
    for current_house in houses:
        for raw in csv_rows(current_house):
            if name and name.lower() not in (raw.get("nombre") or "").lower():
                continue
            result.append(row_to_student([raw.get(k) for k in ("id", "nombre", "casa", "alias", "especie", "genero", "nacimiento", "nacionalidad", "patronus", "patronus_origen", "imagen", "wiki")], current_house))
            if len(result) >= limit:
                return result
    return result


def cached(key: str) -> Any | None:
    """Consulta SQLite y devuelve resultados solo mientras no expire el TTL."""
    with cache_db() as db:
        row = db.execute("SELECT payload,cached_at FROM student_cache WHERE cache_key=?", (key,)).fetchone()
        if not row or datetime.fromisoformat(row["cached_at"]) + CACHE_TTL <= datetime.utcnow():
            return None
        return json.loads(row["payload"])


def cache(key: str, payload: Any) -> None:
    """Guarda respuestas de consultas históricas en el archivo SQLite."""
    with cache_db() as db:
        db.execute("INSERT OR REPLACE INTO student_cache(cache_key,payload,cached_at) VALUES(?,?,?)", (key, json.dumps(payload, ensure_ascii=False), datetime.utcnow().isoformat()))
        db.commit()

@app.on_event("startup")
def startup() -> None:
    """Prepara MariaDB, SQLite y, si es necesario, las casas históricas."""
    Base.metadata.create_all(engine)
    cache_db().close()
    time.sleep(3)
    create_history_tables()

@app.get("/health", include_in_schema=False)
@app.get("/salud", tags=["Sistema"], summary="Estado del servicio", description="Comprueba MariaDB y devuelve R01, fuentes y ubicación de la caché SQLite.")
def health() -> dict[str, Any]:
    """Comprueba MariaDB y devuelve el estado R01 del servicio."""
    with Session() as session:
        session.execute(select(Enrollment.id).limit(1)).first()
    return {"status": "ok", "code": "R01", "cache": str(CACHE_PATH), "sources": {house: config["kind"] for house, config in SOURCES.items()}, "no_migration": True}

@app.get("/students", dependencies=[Depends(require_api_key)], response_model=list[StudentOut], include_in_schema=False)
@app.get("/estudiantes", dependencies=[Depends(require_api_key)], response_model=list[StudentOut], tags=["Consultas"], summary="Buscar estudiantes históricos", description="Busca por nombre y casa en los CSV de referencia, no en las tablas históricas vivas. No incluye nuevas matrículas de MariaDB. Almacena temporalmente el resultado en SQLite.")
def search_students(name: str | None = Query(default=None), house: str | None = Query(default=None), limit: int = Query(default=20, ge=1, le=100)) -> list[StudentOut]:
    """Busca históricos en los CSV de referencia y almacena el resultado en SQLite."""
    key = f"search:{name or ''}:{house or ''}:{limit}"
    hit = cached(key)
    if hit is not None:
        return [StudentOut(**item) for item in hit]
    result = find_history(name, house, limit)
    cache(key, [item.model_dump() for item in result])
    return result

@app.get("/students/{student_id}", dependencies=[Depends(require_api_key)], response_model=StudentOut, include_in_schema=False)
@app.get("/estudiantes/{student_id}", dependencies=[Depends(require_api_key)], response_model=StudentOut, tags=["Consultas"], summary="Consultar estudiante por ID", description="Consulta primero MariaDB, después SQLite y, si no existe, los cuatro motores históricos. Solo cachea los resultados históricos.")
def get_student(student_id: str) -> StudentOut:
    """Consulta MariaDB, después SQLite y finalmente las bases de las casas."""
    with Session() as session:
        current = session.scalar(select(Enrollment).where(Enrollment.student_id == student_id))
        if current:
            return StudentOut(id=current.student_id, nombre=current.full_name, casa=current.house or "", origen="mariadb")
    hit = cached(f"student:{student_id}")
    if hit is not None:
        return StudentOut(**hit)
    result = query_history(student_id)
    if not result:
        raise HTTPException(404, "Estudiante no encontrado")
    cache(f"student:{student_id}", result.model_dump())
    return result

@app.post("/enrollments", dependencies=[Depends(require_api_key)], response_model=EnrollmentOut, status_code=201, include_in_schema=False)
@app.post("/matriculas", dependencies=[Depends(require_api_key)], response_model=EnrollmentOut, status_code=201, tags=["Matrículas"], summary="Crear matrícula", description="Crea una matrícula nueva en MariaDB; no escribe en bases históricas.")
def create_enrollment(data: EnrollmentIn) -> EnrollmentOut:
    """Inserta una matrícula nueva únicamente en MariaDB."""
    with Session.begin() as session:
        if session.scalar(select(Enrollment).where(Enrollment.student_id == data.student_id)):
            raise HTTPException(409, "student_id ya existe")
        row = Enrollment(**data.model_dump())
        session.add(row)
        session.flush()
        return row

@app.put("/enrollments/{student_id}", dependencies=[Depends(require_api_key)], response_model=EnrollmentOut, include_in_schema=False)
@app.put("/matriculas/{student_id}", dependencies=[Depends(require_api_key)], response_model=EnrollmentOut, tags=["Matrículas"], summary="Modificar matrícula", description="Actualiza exclusivamente una matrícula nueva de MariaDB.")
def update_enrollment(student_id: str, data: EnrollmentIn) -> EnrollmentOut:
    """Modifica en MariaDB la matrícula nueva indicada."""
    with Session.begin() as session:
        row = session.scalar(select(Enrollment).where(Enrollment.student_id == student_id))
        if not row:
            raise HTTPException(404, "Matrícula no encontrada en MariaDB")
        for key, value in data.model_dump().items():
            setattr(row, key, value)
        return row

@app.delete("/enrollments/{student_id}", dependencies=[Depends(require_api_key)], include_in_schema=False)
@app.delete("/matriculas/{student_id}", dependencies=[Depends(require_api_key)], tags=["Matrículas"], summary="Eliminar matrícula", description="Elimina exclusivamente una matrícula nueva de MariaDB.")
def delete_enrollment(student_id: str) -> dict[str, str]:
    """Elimina de MariaDB una matrícula nueva; no toca las casas."""
    with Session.begin() as session:
        row = session.scalar(select(Enrollment).where(Enrollment.student_id == student_id))
        if not row:
            raise HTTPException(404, "Matrícula no encontrada en MariaDB")
        session.delete(row)
    return {"status": "deleted"}

