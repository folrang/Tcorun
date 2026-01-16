import sys
import json
import ccxt
import pandas as pd

def parse_arguments():
    if '--params' in sys.argv:
        idx = sys.argv.index('--params')
        return json.loads(sys.argv[idx + 1])
    return {}

def main():
    params = parse_arguments()
    
    exchange_name = params.get('exchange', 'binance')
    symbol = params.get('symbol', 'BTC/USDT')
    timeframe = params.get('timeframe', '1h')
    limit = params.get('limit', 100)
    
    print(f"Fetching {symbol} from {exchange_name}")
    
    try:
        # 거래소 초기화
        exchange_class = getattr(ccxt, exchange_name)
        exchange = exchange_class()
        
        # OHLCV 데이터 가져오기
        ohlcv = exchange.fetch_ohlcv(symbol, timeframe, limit=limit)
        
        # DataFrame 변환
        df = pd.DataFrame(ohlcv, columns=['timestamp', 'open', 'high', 'low', 'close', 'volume'])
        df['timestamp'] = pd.to_datetime(df['timestamp'], unit='ms')
        
        # JSON 결과
        result = {
            "success": True,
            "exchange": exchange_name,
            "symbol": symbol,
            "timeframe": timeframe,
            "data": df.to_dict('records')
        }
        
        print(json.dumps(result, default=str))
        
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}))
        sys.exit(1)

if __name__ == "__main__":
    main()