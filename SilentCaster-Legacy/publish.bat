@echo off
echo 发布 SilentCaster...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
echo 发布完成！
pause 