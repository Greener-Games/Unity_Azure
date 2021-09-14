@ECHO OFF
SET mypath=%~dp0
set version=%1
set projectPath=%2
echo %mypath:~0,-1%
echo executing "C:\Program Files\Unity\Hub\Editor\%version%\editor\Unity.exe" -batchmode -logfile -quit -projectPath "%projectPath%" -nographics -executeMethod UnityEditor.SyncVS.SyncSolution
"C:\Program Files\Unity\Hub\Editor\%version%\editor\Unity.exe" -batchmode -logfile -quit -projectPath "%projectPath%" -nographics -executeMethod UnityEditor.SyncVS.SyncSolution -force-free
PAUSE