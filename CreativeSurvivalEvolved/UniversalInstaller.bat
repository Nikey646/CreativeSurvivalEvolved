@echo off

for /D %%a IN (*) DO (
	echo Installing %%a
	XCOPY /s /q /y "%%a" "%LocalAppData%\ProjectorGames\FortressCraft\Mods\%%a\"
	echo.
)

pause