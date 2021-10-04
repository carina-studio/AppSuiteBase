@echo off

IF not exist Packages (
	mkdir Packages
)

dotnet build Core -c Release
IF %ERRORLEVEL% NEQ 0 ( 
   exit
)

dotnet pack Core -c Release -o Packages