@echo off

Setlocal ENABLEDELAYEDEXPANSION

IF NOT EXIST version.txt (
    GOTO FIRST-RUN-START
) ELSE (
    set /p Build=<version.txt

    IF !Build! NEQ 3.0.30037 (GOTO FIRST-RUN-START) ELSE (GOTO START-RAVENDB)
)

:START-RAVENDB
start "Raven DB" "%~dp0\Server\Raven.Server.exe" --debug --browser
GOTO END

:FIRST-RUN-START
start http://ravendb.net/first-run?type=start^&ver=3.0.30037
echo.|set /p="3.0.30037">version.txt
GOTO START-RAVENDB
GOTO END

:END
