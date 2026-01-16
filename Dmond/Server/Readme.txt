8080/tcp, 8443/tcp(+UDP 8443 for HTTP/3), 9000/tcp 허용.

HTTP/3는 QUIC(UDP) 이므로 UDP 8443 도 열어야 합니다. [stackoverflow.com]


--

변경 사항 요약
1.	PythonExecutorService: Python 스크립트 실행 엔진 (Process 기반)
2.	PythonController: REST API 엔드포인트 (/api/python/*)
3.	PythonService: gRPC 서비스 (선택 사항)
4.	Program.cs: 서비스 등록 추가
5.	Python 스크립트: 차트, 분석, 데이터 가져오기
6.	appsettings.json: Python 설정 추가
이 구조는 기존 Server 아키텍처와 완벽하게 통합되며, 금융 거래 및 분석 기능을 제공합니다!
