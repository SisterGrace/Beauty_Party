#!/bin/zsh
cd $(dirname $0);
DATA_PATH=$(dirname $0)/GenerateDatas
cd ..
echo "开始移动"
BYTES_ROOT=$(dirname "$PWD")/Client/Assets/AssetRaw/Configs/bytes
CS_ROOT=$(dirname "$PWD")/Client/Assets/GameScripts/Game/GameProto/GameConfig
rm -rf ${BYTES_ROOT}/*.bytes
rm -rf -r ${CS_ROOT}

cp ${DATA_PATH}/bytes/*.bytes ${BYTES_ROOT}
mkdir ${CS_ROOT}/
cp -r ${DATA_PATH}/code/ ${CS_ROOT}/
