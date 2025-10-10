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
from openpyxl import load_workbook
import openpyxl.utils
from google.oauth2.service_account import Credentials
from googleapiclient.discovery import build

ROOT = Path(__file__).resolve().parent
VAR = ROOT / "var"
VAR.mkdir(exist_ok=True)

CACHE_FILE = VAR / "cache.json"
CREDENTIALS = ROOT / "auth.json"
BASE_NAME = "analytics"

# --- Clean old exports in VAR ---
for ext in (".xls", ".xlsx", ".xlsx.xls", ".xls.xlsx"):
    fp = VAR / f"{BASE_NAME}{ext}"
    if fp.exists():
        fp.unlink()

# --- Auth (Drive + Sheets) ---
SERVICE_ACCOUNT_FILE = str(CREDENTIALS)
SCOPES = ["https://www.googleapis.com/auth/drive.readonly"]

creds = Credentials.from_service_account_file(SERVICE_ACCOUNT_FILE, scopes=SCOPES)
drive = build("drive", "v3", credentials=creds)

gc = pygsheets.authorize(service_file=SERVICE_ACCOUNT_FILE)
spreadsheet_id = gc.open(BASE_NAME).id

# --- Export XLSX через Drive API ---
excel_path = VAR / f"{BASE_NAME}.xlsx"
request = drive.files().export_media(
    fileId=spreadsheet_id,
    mimeType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
)
with open(excel_path, "wb") as f:
    f.write(request.execute())

print(f"✅ Файл аналитики был успешно выгружен: {excel_path}")

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


def parse_excel_formulas(material: dict, excel_path: str, wb_formulas=None, wb_values=None) -> dict | None:
    parser_type = material.get("parserType")
    if parser_type not in ("formula", "value"):
        print(f"Пропущен материал '{material.get('materialName')}' — неизвестный parserType: {parser_type}")
        return None

    properties = material.get("properties", [])
    if not properties:
        print(f"Пропущен материал '{material.get('materialName')}' — нет свойств.")
        return None

    close_books = False
    if wb_formulas is None or wb_values is None:
        wb_formulas = openpyxl.load_workbook(excel_path, data_only=False)
        wb_values = openpyxl.load_workbook(excel_path, data_only=True)
        close_books = True

    sheet_name = material.get("sheet")

    if sheet_name not in wb_values.sheetnames:
        print(f"Лист '{sheet_name}' не найден для '{material.get('materialName')}'.")
        if close_books:
            wb_formulas.close()
            wb_values.close()
        return None

    ws_v = wb_values[sheet_name]
    ws_f = wb_formulas[sheet_name]

    ref_col = next((p["colLetter"] for p in properties if p.get("name") == "med"), properties[0]["colLetter"])
    last_row = None
    for row in range(ws_v.max_row, 0, -1):
        cell = ws_v[f"{ref_col}{row}"]
        if cell.value not in (None, ""):
            last_row = row
            break

    if last_row is None:
        print(f"Нет данных для '{material.get('materialName')}'.")
        if close_books:
            wb_formulas.close()
            wb_values.close()
        return None

    date_val = ws_v[f"A{last_row}"].value
    property_values = []

    if parser_type == "formula":
        med_prop = next((p for p in properties if p.get("name") == "med"), None)
        if not med_prop:
            print(f"'{material.get('materialName')}' — нет поля med.")
            if close_books:
                wb_formulas.close()
                wb_values.close()
            return None

        cell_addr = f"{med_prop['colLetter']}{last_row}"
        cell_formula = ws_f[cell_addr].value
        cell_val = ws_v[cell_addr].value

        min_val = max_val = None
        if isinstance(cell_formula, str) and cell_formula.startswith("="):
            numbers = re.findall(r"\d+(?:\.\d+)?", cell_formula)
            if len(numbers) >= 2:
                min_val, max_val = float(numbers[0]), float(numbers[1])

        if isinstance(cell_val, (int, float)):
            cell_val = float(cell_val)

        for prop in properties:
            if prop["name"] == "min":
                val = min_val
            elif prop["name"] == "max":
                val = max_val
            elif prop["name"] == "med":
                val = cell_val
            else:
                val = None

            property_values.append({
                "propertyId": prop["propertyId"],
                "value": val
            })

    else:  # parserType == "value"
        for prop in properties:
            col = prop["colLetter"]
            val = ws_v[f"{col}{last_row}"].value
            if isinstance(val, (int, float)):
                val = float(val)
            property_values.append({
                "propertyId": prop["propertyId"],
                "value": val
            })

    return {
        "materialId": material["materialId"],
        "dateValues": [
            {
                "date": date_val.strftime("%Y-%m-%d") if hasattr(date_val, "strftime") else str(date_val) if date_val else None,
                "propertyValues": property_values
            }
        ]
    }


def parse_dataset(config: dict, excel_path: Path, cache: dict) -> dict:
    sheets = pd.read_excel(str(excel_path), sheet_name=None, header=[0, 1])
    wb_formulas = openpyxl.load_workbook(excel_path, data_only=False)
    wb_values = openpyxl.load_workbook(excel_path, data_only=True)
    payload = {"data": []}

    for material in config["materials"]:
        ptype = material.get("parserType", "old")
        if ptype == "old":
            result = parse_old(material, sheets)
        elif ptype == "new":
            result = parse_new(material, sheets)
        elif ptype in ("formula", "value"):
            result = parse_excel_formulas(material, excel_path, wb_formulas, wb_values)
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
