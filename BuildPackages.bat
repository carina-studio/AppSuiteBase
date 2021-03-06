@echo off

IF not exist Packages (
	mkdir Packages
)

dotnet build Core.Tests -c Release
IF %ERRORLEVEL% NEQ 0 ( 
   exit
)

dotnet pack Core -c Release -o Packages
dotnet pack Core.Tests -c Release -o Packages