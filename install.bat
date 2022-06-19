@ECHO OFF

robocopy staging/ %APPDATA%\Playnite\Extensions\StarRatings\ /MIR /E
