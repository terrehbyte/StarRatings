@ECHO OFF

call build.bat

mkdir staging

copy bin\Debug\StarRatings.dll staging\StarRatings.dll
copy extension.yaml staging\extension.yaml
copy icon.png staging\icon.png
copy check.png staging\check.png
copy return.png staging\return.png
copy LICENSE.md staging\LICENSE.md
copy THIRDPARTY.md staging\THIRDPARTY.md

call %USERPROFILE%\AppData\Local\Playnite\Toolbox.exe pack staging\ dist\