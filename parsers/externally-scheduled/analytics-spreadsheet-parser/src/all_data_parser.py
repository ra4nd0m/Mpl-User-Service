import pandas as pd
import json
import re
import os
import requests
from datetime import datetime

CACHE_FILE = "cache.json"


def load_cache():
    if os.path.exists(CACHE_FILE):
        with open(CACHE_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_cache(cache: dict):
    with open(CACHE_FILE, "w", encoding="utf-8") as f:
        json.dump(cache, f, ensure_ascii=False, indent=2)


# 🔹 парсер для стандартных листов с 1-9
def parse_old(material: dict, sheets: dict) -> dict | None:
    if material.get("parserType") != "old":
        return None

    sheet_name = material["sheet"]
    df = sheets[sheet_name]

    date_col = material["date"]["header"]
    props = material["properties"]

    prop_headers = [p["header"] for p in props]
    cols_to_check = [(material["materialName"], h) for h in prop_headers]

    # фильтруем пустые строки
    df_filtered = df.dropna(subset=cols_to_check, how="all")
    if df_filtered.empty:
        return None

    date_values = []
    for _, row in df_filtered.iterrows():
        row_date = row[date_col]
        if hasattr(row_date, "strftime"):
            row_date = row_date.strftime(material["date"]["format"])
        else:
            row_date = str(row_date)

        property_values = []
        for p in props:
            col = (material["materialName"], p["header"])
            if col in df.columns:
                val = row[col]
                if pd.notna(val):
                    property_values.append({
                        "propertyId": p["propertyId"],
                        "value": str(val)
                    })

        if property_values:
            date_values.append({
                "date": row_date,
                "propertyValues": property_values
            })

    if not date_values:
        return None

    return {
        "materialId": material["materialId"],
        "dateValues": date_values
    }


# 🔹 парсер для 10 листа
def parse_new(material: dict, sheets: dict) -> dict | None:
    if material.get("parserType") != "new":
        return None

    sheet_name = material["sheet"]
    df = sheets[sheet_name]
    df_simple = df.copy()
    df_simple.columns = df_simple.columns.get_level_values(0)

    date_pattern = re.compile(r"\((\d{2}\.\d{2}\.\d{4})\)")
    f = material["rowFilter"]

    # ищем строку по фильтру
    row_idx = df_simple[
        (df_simple["Страна"].astype(str) == f["country"]) &
        (df_simple["Порт"].astype(str) == f["port"]) &
        (df_simple["Вид материала"].astype(str) == f["type"])
    ].index

    if row_idx.empty:
        print(f"Не найдено: {f}")
        return None
    row_idx = row_idx[0]

    date_values = []
    for col in df.columns:
        if isinstance(col[0], str):
            match = date_pattern.search(col[0])
            if match and col[1] == "Цена":
                date_str = datetime.strptime(
                    match.group(1), "%d.%m.%Y"
                ).strftime(material["date"]["format"])

                props = []
                for p in material["properties"]:
                    col2 = (col[0], p["column"])
                    value = df.loc[row_idx, col2]
                    if pd.notna(value):
                        props.append({
                            "propertyId": p["propertyId"],
                            "value": str(value)
                        })

                if props:
                    date_values.append({
                        "date": date_str,
                        "propertyValues": props
                    })

    if not date_values:
        return None

    return {
        "materialId": material["materialId"],
        "dateValues": date_values
    }


# 🔹 основной обработчик
# 🔹 основной обработчик
def parse_dataset(config: dict) -> dict:
    excel_path = config["excel"]["path"]
    sheets = pd.read_excel(excel_path, sheet_name=None, header=[0, 1])

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
            payload["data"].append(result)

    return payload


if __name__ == "__main__":
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    CONFIG_PATH = os.path.join(BASE_DIR, "..", "config.json")

    with open(CONFIG_PATH, "r", encoding="utf-8") as f:
        config = json.load(f)

    cache = load_cache()
    payload = parse_dataset(config)

    if payload["data"]:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        save_cache(cache)
        # send_payload(payload, config)  # включай по необходимости
    else:
        print("[INFO] Нет новых данных для вывода")
