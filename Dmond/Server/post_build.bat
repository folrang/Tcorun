@echo off
setlocal

REM echo ====================================
REM echo Post-Build: Protos 및 Certs 복사
REM echo ====================================

set "ProjectDir=%~1"
set "OutDir=%~2"

if not defined ProjectDir (
    echo 오류: ProjectDir 매개변수가 필요합니다.
    exit /b 1
)

if not defined OutDir (
    echo 오류: OutDir 매개변수가 필요합니다.
    exit /b 1
)

rem Protos 폴더 복사
if exist "%ProjectDir%Protos" (
    REM echo Protos 폴더를 %OutDir%Protos로 복사 중...
    xcopy /E /I /Y "%ProjectDir%Protos" "%OutDir%Protos"
    if %ERRORLEVEL% EQU 0 (
        echo [성공] Protos 폴더 복사 완료
    ) else (
        echo [실패] Protos 폴더 복사 실패
        exit /b %ERRORLEVEL%
    )
) else (
    echo [경고] Protos 폴더를 찾을 수 없습니다: %ProjectDir%Protos
)

rem Certs 폴더 복사
if exist "%ProjectDir%Certs" (
    REM echo Certs 폴더를 %OutDir%Certs로 복사 중...
    xcopy /E /I /Y "%ProjectDir%Certs" "%OutDir%Certs"
    if %ERRORLEVEL% EQU 0 (
        echo [성공] Certs 폴더 복사 완료
    ) else (
        echo [실패] Certs 폴더 복사 실패
        exit /b %ERRORLEVEL%
    )
) else (
    echo [경고] Certs 폴더를 찾을 수 없습니다: %ProjectDir%Certs
)

REM echo ====================================
echo Post-Build 완료
REM echo ====================================

exit /b 0