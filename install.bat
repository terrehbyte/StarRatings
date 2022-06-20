@ECHO OFF

call release.bat

robocopy staging/ %APPDATA%\Playnite\Extensions\StarRatings\ /MIR /E
