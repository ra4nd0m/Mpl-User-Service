import pandas as pd
import json
import re
import os
import openpyxl  # (used by pandas for xlsx)
import requests
import pygsheets
import glob
from datetime import datetime
from pathlib import Path

# --- Paths anchored to the script directory ---
ROOT = Path(__file__).resolve().parent
VAR = ROOT / "var"
VAR.mkdir(exist_ok=True)

CACHE_FILE = VAR / "cache.json"
CREDENTIALS = ROOT / "auth.json"
BASE_NAME = "analytics"

# --- Auth ---
gc = pygsheets.authorize(service_file=str(CREDENTIALS))
spreadsheet = gc.open("analytics")

# --- Clean old exports in VAR ---
for ext in (".xls", ".xlsx", ".xlsx.xls", ".xls.xlsx"):
    fp = VAR / f"{BASE_NAME}{ext}"
    if fp.exists():
        fp.unlink()

# --- Export (XLS only in your setup) ---
spreadsheet.export(
    file_format=pygsheets.ExportType.XLS,
    path=str(VAR),
    filename=BASE_NAME  # no extension
)

# --- Locate the created file (prefer xlsx/xls explicitly) ---
candidates = sorted(
    list(VAR.glob(f"{BASE_NAME}.xlsx")) + list(VAR.glob(f"{BASE_NAME}.xls"))
)
if not candidates:
    raise FileNotFoundError("Экспорт не создал файл — проверь доступы/права")

excel_path = candidates[0]
print(f"✅ Файл аналитики был успешно загружен: {excel_path}")

# --- Cache helpers ---
def load_cache():
    if CACHE_FILE.exists():
        with CACHE_FILE.open("r", encoding="utf-8") as f:
            return json.load(f)
    return {}

def save_cache(cache: dict):
    with CACHE_FILE.open("w", encoding="utf-8") as f:
        json.dump(cache, f, ensure_ascii=False, indent=2)

# --- Parsers (unchanged) ---
def parse_old(material: dict, sheets: dict) -> dict | None:
    if material.get("parserType") != "old":
        return None
    sheet_name = material["sheet"]
    df = sheets[sheet_name]

    date_col = material["date"]["header"]
    material_id = material["materialId"]

    props = material["properties"]
    prop_headers = [p["header"] for p in props]

    cols_to_check = [(material["materialName"], h) for h in prop_headers]

    df_filtered = df.dropna(subset=cols_to_check, how="all")
    if df_filtered.empty:
        return None

    last_row = df_filtered.iloc[-1]
    last_date = last_row[date_col]
    if isinstance(last_date, pd.Series):
        last_date = last_date.iloc[0]
    if hasattr(last_date, "strftime"):
        last_date = last_date.strftime(material["date"]["format"])
    else:
        last_date = str(last_date)

    property_values = []
    for p in props:
        col = (material["materialName"], p["header"])
        if col in df.columns:
            val = last_row[col]
            if pd.notna(val):
                property_values.append({
                    "propertyId": p["propertyId"],
                    "value": str(val)
                })

    return {
        "materialId": material_id,
        "dateValues": [{"date": last_date, "propertyValues": property_values}]
    }

def parse_new(material: dict, sheets: dict) -> dict | None:
    if material.get("parserType") != "new":
        return None

    sheet_name = material["sheet"]
    df = sheets[sheet_name]
    df_simple = df.copy()
    df_simple.columns = df_simple.columns.get_level_values(0)

    date_pattern = re.compile(r"\((\d{2}\.\d{2}\.\d{4})\)")
    last_date = None
    last_date_col = None

    for col in reversed(df.columns):
        if isinstance(col[0], str):
            match = date_pattern.search(col[0])
            if match and col[1] == "Цена":
                if df[col].notna().any():
                    last_date = datetime.strptime(
                        match.group(1), "%d.%m.%Y"
                    ).strftime(material["date"]["format"])
                    last_date_col = col[0]
                    break

    if not last_date:
        print(f"Не найдена дата с данными для {material['materialId']}")
        return None

    f = material["rowFilter"]
    row_idx = df_simple[
        (df_simple["Страна"].astype(str) == f["country"]) &
        (df_simple["Порт"].astype(str) == f["port"]) &
        (df_simple["Вид материала"].astype(str) == f["type"])
    ].index

    if row_idx.empty:
        print(f"Не найдено: {f}")
        return None

    row_idx = row_idx[0]

    props = []
    for p in material["properties"]:
        col = (last_date_col, p["column"])
        value = df.loc[row_idx, col]
        props.append({
            "propertyId": p["propertyId"],
            "value": str(value) if pd.notna(value) else None
        })

    return {
        "materialId": material["materialId"],
        "dateValues": [{"date": last_date, "propertyValues": props}]
    }

def parse_dataset(config: dict, excel_path: Path, cache: dict) -> dict:
    sheets = pd.read_excel(str(excel_path), sheet_name=None, header=[0, 1])
    payload = {"data": []}

    for material in config["materials"]:
        ptype = material.get("parserType", "old")
        if ptype == "old":
            result = parse_old(material, sheets)
        elif ptype == "new":
            result = parse_new(material, sheets)
        else:
            raise ValueError(f"Неизвестный parserType: {ptype}")

        if result:
            m_id = str(result["materialId"])
            m_date = result["dateValues"][0]["date"]

            if cache.get(m_id) == m_date:  # debounce
                print(f"[SKIP] Material {m_id} уже отправлен на {m_date}")
                continue

            cache[m_id] = m_date
            payload["data"].append(result)

    return payload

def send_payload(payload: dict, config: dict):
    server = os.getenv("SERVER_URL") or config["server"]["url"]
    route = os.getenv("SERVER_ROUTE") or config["server"]["route"]
    url = server.rstrip("/") + "/" + route.lstrip("/")
    print(f"[INFO] Отправка на {url}")
    response = requests.post(url, json=payload, timeout=10)
    print(f"[INFO] Ответ сервера: {response.status_code} {response.text}")

if __name__ == "__main__":
    CONFIG_PATH = ROOT / "config.json"
    with CONFIG_PATH.open("r", encoding="utf-8") as f:
        config = json.load(f)

    cache = load_cache()

    # Use the freshly exported file (ignore/override config path)
    payload = parse_dataset(config, excel_path, cache)

    if payload["data"]:
        send_payload(payload, config)
        save_cache(cache)
    else:
        print("[INFO] Нет новых данных для отправки")
