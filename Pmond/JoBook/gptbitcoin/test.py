import os
from dotenv import load_dotenv
load_dotenv()

print("UPBIT_ACCESS_KEY:", os.getenv("UPBIT_ACCESS_KEY"))
print("UPBIT_SECRET_KEY:", os.getenv("UPBIT_SECRET_KEY"))
print("OPENAI_API_KEY:", os.getenv("OPENAI_API_KEY"))

print("Environment variables loaded successfully.")