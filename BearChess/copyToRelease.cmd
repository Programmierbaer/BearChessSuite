@echo off
cd ..\..\BearChess\BearChess
copy ..\..\BearChessDev\BearChess\*.cs /s /y
copy ..\..\BearChessDev\BearChess\*.xaml /s /y
copy ..\..\BearChessDev\BearChess\*.resx /s /y
copy ..\..\BearChessDev\BearChess\*.png /s /y
copy ..\..\BearChessDev\BearChess\*.jpg /s /y
copy ..\..\BearChessDev\BearChess\*.ico /s /y
copy ..\..\BearChessDev\BearChess\*.csproj /s /y
copy ..\..\BearChessDev\BearChess\*.csproj.DotSettings
copy ..\..\BearChessDev\BearChess\BearChess.sln /y
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" BearChess.sln
cd ..\..\BearChessDev\BearChess