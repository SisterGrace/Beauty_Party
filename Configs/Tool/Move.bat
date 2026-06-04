del /f /s /q ..\..\Client\Assets\AssetRaw\Configs\bytes\*.*
del /f /s /q ..\..\Client\Assets\GameScripts\Game\GameProto\GameConfig\*.*
del /f /s /q ..\..\Client\Assets\AssetRaw\Configs\Cfglangue.csv
copy GenerateDatas\bytes ..\..\Client\Assets\AssetRaw\Configs\bytes
XCOPY /S GenerateDatas\code ..\..\Client\Assets\GameScripts\Game\GameProto\GameConfig
copy ..\Excel\Cfglangue.csv  ..\..\Client\Assets\AssetRaw\Configs
pause
