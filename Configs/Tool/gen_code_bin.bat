set GEN_CLIENT=Tools\Luban.ClientServer\Luban.ClientServer.exe
set CONF_ROOT=../Excel

%GEN_CLIENT% -j cfg --^
 -d %CONF_ROOT%/ARoot/__root__.xml ^
 --input_data_dir %CONF_ROOT% ^
 --output_code_dir GenerateDatas\code ^
 --output_data_dir GenerateDatas\bytes ^
 --gen_types code_cs_unity_bin,data_bin ^
 -s client 

pause