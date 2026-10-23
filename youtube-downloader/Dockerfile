FROM python:3.13-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py .
COPY static ./static

ENV HOST=0.0.0.0 \
    PORT=5000 \
    PYTHONUNBUFFERED=1
EXPOSE 5000

CMD ["python", "app.py"]
