import sys
import json
import pandas as pd
import numpy as np
import yfinance as yf

def parse_arguments():
    if '--params' in sys.argv:
        idx = sys.argv.index('--params')
        return json.loads(sys.argv[idx + 1])
    return {}

def calculate_rsi(prices, period=14):
    """RSI 계산"""
    delta = prices.diff()
    gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
    loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()
    rs = gain / loss
    rsi = 100 - (100 / (1 + rs))
    return rsi

def calculate_macd(prices, fast=12, slow=26, signal=9):
    """MACD 계산"""
    ema_fast = prices.ewm(span=fast).mean()
    ema_slow = prices.ewm(span=slow).mean()
    macd = ema_fast - ema_slow
    signal_line = macd.ewm(span=signal).mean()
    histogram = macd - signal_line
    return macd, signal_line, histogram

def calculate_bollinger_bands(prices, period=20, std_dev=2):
    """볼린저 밴드 계산"""
    sma = prices.rolling(window=period).mean()
    std = prices.rolling(window=period).std()
    upper_band = sma + (std * std_dev)
    lower_band = sma - (std * std_dev)
    return upper_band, sma, lower_band

def main():
    params = parse_arguments()
    
    symbol = params.get('symbol', 'AAPL')
    indicators = params.get('indicators', ['RSI', 'MACD', 'BB'])
    
    print(f"Analyzing {symbol} with indicators: {indicators}")
    
    # 데이터 가져오기
    ticker = yf.Ticker(symbol)
    df = ticker.history(period="3mo")
    
    if df.empty:
        print(json.dumps({"success": False, "error": "No data"}))
        sys.exit(1)
    
    results = {
        "symbol": symbol,
        "current_price": float(df['Close'].iloc[-1]),
        "indicators": {}
    }
    
    # 지표 계산
    if 'RSI' in indicators:
        rsi = calculate_rsi(df['Close'])
        results["indicators"]["RSI"] = {
            "current": float(rsi.iloc[-1]),
            "signal": "oversold" if rsi.iloc[-1] < 30 else "overbought" if rsi.iloc[-1] > 70 else "neutral"
        }
    
    if 'MACD' in indicators:
        macd, signal, hist = calculate_macd(df['Close'])
        results["indicators"]["MACD"] = {
            "macd": float(macd.iloc[-1]),
            "signal": float(signal.iloc[-1]),
            "histogram": float(hist.iloc[-1]),
            "trend": "bullish" if hist.iloc[-1] > 0 else "bearish"
        }
    
    if 'BB' in indicators:
        upper, middle, lower = calculate_bollinger_bands(df['Close'])
        current_price = df['Close'].iloc[-1]
        results["indicators"]["BollingerBands"] = {
            "upper": float(upper.iloc[-1]),
            "middle": float(middle.iloc[-1]),
            "lower": float(lower.iloc[-1]),
            "position": "above" if current_price > upper.iloc[-1] else "below" if current_price < lower.iloc[-1] else "within"
        }
    
    print(json.dumps(results, indent=2))

if __name__ == "__main__":
    main()