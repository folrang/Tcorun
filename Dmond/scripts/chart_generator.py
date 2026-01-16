import sys
import json
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.dates as mdates
from datetime import datetime, timedelta
import yfinance as yf

def parse_arguments():
    """명령줄 인자 파싱"""
    if '--params' in sys.argv:
        idx = sys.argv.index('--params')
        params_json = sys.argv[idx + 1]
        return json.loads(params_json)
    return {}

def fetch_stock_data(symbol, start_date, end_date):
    """실제 주식 데이터 가져오기 (yfinance)"""
    try:
        ticker = yf.Ticker(symbol)
        df = ticker.history(start=start_date, end=end_date)
        return df
    except Exception as e:
        print(f"Error fetching data: {e}", file=sys.stderr)
        return None

def create_candlestick_chart(df, symbol, output_path):
    """캔들스틱 차트 생성"""
    fig, ax = plt.subplots(figsize=(14, 8))
    
    # 캔들스틱 그리기
    width = 0.6
    width2 = 0.05
    
    up = df[df.Close >= df.Open]
    down = df[df.Close < df.Open]
    
    # 상승 (녹색)
    ax.bar(up.index, up.Close - up.Open, width, bottom=up.Open, color='green', alpha=0.8)
    ax.bar(up.index, up.High - up.Close, width2, bottom=up.Close, color='green')
    ax.bar(up.index, up.Low - up.Open, width2, bottom=up.Open, color='green')
    
    # 하락 (빨강)
    ax.bar(down.index, down.Close - down.Open, width, bottom=down.Open, color='red', alpha=0.8)
    ax.bar(down.index, down.High - down.Open, width2, bottom=down.Open, color='red')
    ax.bar(down.index, down.Low - down.Close, width2, bottom=down.Close, color='red')
    
    # 이동평균선
    df['MA20'] = df['Close'].rolling(window=20).mean()
    df['MA50'] = df['Close'].rolling(window=50).mean()
    ax.plot(df.index, df['MA20'], label='MA20', linewidth=2, alpha=0.7)
    ax.plot(df.index, df['MA50'], label='MA50', linewidth=2, alpha=0.7)
    
    ax.set_title(f'{symbol} Price Chart', fontsize=16, fontweight='bold')
    ax.set_xlabel('Date', fontsize=12)
    ax.set_ylabel('Price (USD)', fontsize=12)
    ax.legend()
    ax.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig(output_path, dpi=300)
    plt.close()
    
    return output_path

def main():
    params = parse_arguments()
    
    symbol = params.get('symbol', 'AAPL')
    start_date = params.get('start_date', (datetime.now() - timedelta(days=365)).strftime('%Y-%m-%d'))
    end_date = params.get('end_date', datetime.now().strftime('%Y-%m-%d'))
    chart_type = params.get('chart_type', 'candlestick')
    output_path = params.get('output_path', f'output/{symbol}_chart.png')
    
    print(f"Generating {chart_type} chart for {symbol}")
    print(f"Period: {start_date} to {end_date}")
    
    # 데이터 가져오기
    df = fetch_stock_data(symbol, start_date, end_date)
    
    if df is None or df.empty:
        print("Failed to fetch data", file=sys.stderr)
        sys.exit(1)
    
    # 차트 생성
    result_path = create_candlestick_chart(df, symbol, output_path)
    
    print(f"Chart saved: {result_path}")
    print(json.dumps({"success": True, "path": result_path}))

if __name__ == "__main__":
    main()