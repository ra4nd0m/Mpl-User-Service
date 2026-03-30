import cloudscraper
import datetime
import requests
import os
import json
from datetime import datetime, date

class BaseHistoricData:
    def __init__(self, material_code: str, params: str, start_date=None, end_date=None):
        self.material_code = material_code
        self.params = params
        self.referer = "https://www.shfe.com.cn/eng/"
        self.historical_data = self.scrape_data()

    def scrape_data(self) -> dict[str, dict[str, int]]:
        url = (
            f"https://www.shfe.com.cn/data/tradedata/future/delaymarket/"
            f"delayed_market_data_{self.material_code}_history.dat?params=%22{self.params}"
        )

        headers = {
            "User-Agent": (
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0"
            ),
            "Accept": "*/*",
            "Referer": self.referer,
            "Accept-Language": "en-US,en;q=0.9",
            "Origin": "https://www.shfe.com.cn",
            "X-Requested-With": "XMLHttpRequest",
        }
        scraper = cloudscraper.create_scraper()

        try:
            response = scraper.get(url, headers=headers)

            if response.status_code == 200:
                data = response.json()
                ci_data = data.get("ci_data", [])

                results = {}
                for item in ci_data:
                    trading_day = item.get("TradingDay")
                    if trading_day:
                        # преобразуем YYYYMMDD → YYYY-MM-DD
                        trading_day_fmt = datetime.strptime(trading_day, "%Y%m%d").strftime("%Y-%m-%d")
                        results[trading_day_fmt] = {
                            "volume": item.get("VOLUME"),
                            "open_interest": item.get("OPENINTEREST"),
                            "highest_price": item.get("HIGHESTPRICE"),
                            "lowest_price": item.get("LOWESTPRICE")
                        }
                return results
            else:
                print(f"Error: {response.status_code}")
                print(response.text)
                return {}

        except Exception as e:
            print(f"An error occurred: {e}")
            return {}

class AluminiumHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="al", params="17748")


class CopperHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="cu", params="79036")


class NickelHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="ni", params="95694")


class ZincHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="zn", params="90943")


class TinHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="sn", params="99616")


class LeadHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(material_code="pb", params="93730")


class MaterialDataProcessor:
    def __init__(self, config: dict):
        self.config = config
        self.begin_date = date.today().strftime("%Y-%m-%d")
        
        self.materials = {}
        for material in config.get("materials", []):
            material_id = material["materialId"]
            class_name = material["class"]
            self.materials[material_id] = globals()[class_name]

    def process_all_materials(self):
        results = []
        for material_id, material_class in self.materials.items():
            material_data = self._process_single_material(material_class, material_id)
            if material_data:
                results.append(material_data)
        return {"data": results}

    def _process_single_material(self, material_class, material_id):
        try:
            material_data = material_class(start_date=self.begin_date)

            if not material_data.historical_data:
                print(f"Нет данных для материала {material_id}")
                return None

            last_date = sorted(material_data.historical_data.keys())[-1]
            last_data = material_data.historical_data[last_date]

            highest_price = last_data.get("highest_price")
            lowest_price = last_data.get("lowest_price")
            volume = last_data.get("volume")
            open_interest = last_data.get("open_interest")

            if highest_price is None or lowest_price is None:
                print(f"Нет данных о ценах для материала {material_id}")
                return None

            max_value = float(highest_price)
            min_value = float(lowest_price)
            med_value = (min_value + max_value) / 2

            formatted_date = last_date.replace('-', '.')

            property_values = [
                {"propertyId": 2, "value": str(min_value)},
                {"propertyId": 3, "value": str(max_value)},
                {"propertyId": 1, "value": str(med_value)},
                # {"propertyId": 777, "value": str(volume)},
                # {"propertyId": 888, "value": str(open_interest)}
            ]

            return {
                "materialId": material_id,
                "dateValues": [{
                    "date": formatted_date,
                    "propertyValues": property_values
                }]
            }

        except Exception as e:
            print(f"Ошибка при обработке {material_id}: {e}")
            import traceback
            traceback.print_exc()
            return None
    
    def send_payload(self, payload: dict, config: dict):
        server = os.getenv("SERVER_URL") or config["server"]["url"]
        route = os.getenv("SERVER_ROUTE") or config["server"]["route"]
        url = server.rstrip("/") + "/" + route.lstrip("/")

        print(f"[INFO] Отправка на {url}")
        response = requests.post(url, json=payload, timeout=10)
        print(f"[INFO] Ответ сервера: {response.status_code} {response.text}")


if __name__ == "__main__":
    with open("config.json", "r", encoding="utf-8") as f:
        config = json.load(f)

    processor = MaterialDataProcessor(config)

    payload = processor.process_all_materials()

    if payload["data"]:
        processor.send_payload(payload, config)
    else:
        print("[INFO] Нет данных для отправки")

