# from fastapi import FastAPI, Request
# import uvicorn
#
# app = FastAPI()
#
# @app.post("/api/materials/update")
# async def update_materials(request: Request):
#     data = await request.json()
#     print("[SERVER] Получен payload:")
#     print(data)
#     return {"status": "ok", "received": len(data.get("data", []))}
#
# if __name__ == "__main__":
#     uvicorn.run(app, host="0.0.0.0", port=8000)

import requests

payload = {
    "data": [
        {
            "materialId": 123,
            "dateValues": [
                {
                    "date": "2025-09-01",
                    "propertyValues": [
                        {"propertyId": 1, "value": "12.5"},
                        {"propertyId": 2, "value": "10"}
                    ]
                }
            ]
        }
    ]
}

url = "http://localhost:5000/insertBatch"  # сюда должен смотреть твой .NET сервис
response = requests.post(url, json=payload)

print(response.status_code)
print(response.text)
