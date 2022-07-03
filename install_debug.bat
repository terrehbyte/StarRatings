@ECHO OFF

call debug.bat

robocopy staging/ %APPDATA%\Playnite\Extensions\StarRatings\ /MIR /E
