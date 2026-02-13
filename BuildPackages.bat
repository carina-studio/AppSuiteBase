@echo off

if exist Packages (
	rmdir Packages /s /q
)
mkdir Packages
if %ERRORLEVEL% neq 0 ( 
   exit
)

dotnet build Core.Tests -c Release
if %ERRORLEVEL% neq 0 ( 
   exit
)

dotnet build SyntaxHighlighting -c Release
if %ERRORLEVEL% neq 0 ( 
   exit
)

dotnet pack Core -c Release -o Packages --no-build
dotnet pack Core.Tests -c Release -o Packages --no-build
dotnet pack Fonts -c Release -o Packages --no-build
dotnet pack SyntaxHighlighting -c Release -o Packages --no-build