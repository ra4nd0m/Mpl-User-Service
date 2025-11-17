import cloudscraper
import datetime
import requests
import os
import json

class BaseHistoricData:
    def __init__(self, datasource_id, referer, start_date=None, end_date=None):
        self.datasource_id = datasource_id
        self.referer = referer
        self.start_date = start_date or datetime.date.today().strftime("%Y-%m-%d")
        self.end_date = end_date or datetime.date.today().strftime("%Y-%m-%d")
        self.historical_data = self.scrape_data()

    def scrape_data(self) -> dict[int, dict]:
        url = f"https://www.lme.com/api/trading-data/chart-data?datasourceId={self.datasource_id}&startDate={self.start_date}&endDate={self.end_date}"
        headers = {
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Accept": "*/*",
            "Referer": self.referer,
            "Accept-Language": "en-US,en;q=0.9",
            "Origin": "https://www.lme.com",
            "X-Requested-With": "XMLHttpRequest",
        }

        scraper = cloudscraper.create_scraper()

        try:
            response = scraper.get(url, headers=headers)

            if response.status_code == 200:
                data = response.json()
                historic_dates = data["Labels"]

                cash_bid = data["Datasets"][0]['Data']
                cash_offer = data["Datasets"][1]['Data']
                month_bid = data["Datasets"][2]['Data']
                month_offer = data["Datasets"][3]['Data']

                # разбил данные и даты попарно в словари
                data_cash_bid = {label: value for label, value in zip(historic_dates, cash_bid)}
                data_cash_offer = {label: value for label, value in zip(historic_dates, cash_offer)}
                data_month_bid = {label: value for label, value in zip(historic_dates, month_bid)}
                data_month_offer = {label: value for label, value in zip(historic_dates, month_offer)}

                historical_data = {
                    0: data_cash_bid,
                    1: data_cash_offer,
                    2: data_month_bid,
                    3: data_month_offer
                }

                return historical_data
            else:
                print(f"Error: {response.status_code}")
                print(response.text)
                return {}

        except Exception as e:
            print(f"An error occurred: {e}")
            return {}


class AluminiumHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="dddbc815-1a81-4f35-beed-6a193f4c946a",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Aluminium#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class CopperHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="39fabad0-95ca-491b-a733-bcef31818b16",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Copper#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class NickelHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="0ab0e715-84cd-41d1-8318-a96070917a43",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Nickel#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class ZincHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="1a1aca59-3032-4ea6-b22b-18b151514b84",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Zinc#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class TinHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="707be4f9-a4f5-4fe3-8f5b-7bd2886f58e7",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Tin#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class LeadHistoricData(BaseHistoricData):
    def __init__(self, start_date=None, end_date=None):
        super().__init__(
            datasource_id="9f2cf5c9-855d-4f68-939a-387babebe11f",
            referer="https://www.lme.com/en/Metals/Non-ferrous/LME-Lead#Price+graphs",
            start_date=start_date,
            end_date=end_date
        )


class MaterialDataProcessor:
    def __init__(self, config: dict):
        self.config = config
        self.begin_date = self.finish_date = datetime.date.today().strftime("%Y-%m-%d")
        self.materials = {
            103: AluminiumHistoricData,
            104: CopperHistoricData,
            105: NickelHistoricData,
            106: ZincHistoricData,
            107: TinHistoricData,
            108: LeadHistoricData
        }

    def process_all_materials(self):
        results = []
        for material_id, material_class in self.materials.items():
            material_data = self._process_single_material(material_class, material_id)
            if material_data:
                results.append(material_data)
        return {"data": results}


    def _process_single_material(self, material_class, material_id):
        try:
            material_data = material_class(start_date=self.begin_date, end_date=self.finish_date)

            date_min, numeric_min = next(reversed(material_data.historical_data.get(0).items()))
            date_max, numeric_max = next(reversed(material_data.historical_data.get(1).items()))

            min_value = float(numeric_min)
            max_value = float(numeric_max)
            med_value = (min_value + max_value) / 2

            formatted_date = date_min.replace('/', '.')

            property_values = [
                {"propertyId": 2, "value": str(min_value)},
                {"propertyId": 3, "value": str(max_value)},
                {"propertyId": 1, "value": str(med_value)}
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
        processor = MaterialDataProcessor(config)
        processor.send_payload(payload, config)
    else:
        print("[INFO] Нет данных для отправки")

