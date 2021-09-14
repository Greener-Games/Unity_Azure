@ECHO OFF
SET mypath=%~dp0
echo %mypath:~0,-1%
pwsh.exe -Command "& {Install-Module powershell-yaml;Install-Module -Name docfx-toc-generator;import-module docfx-toc-generator;Build-TocHereRecursive;}"
PAUSE