location='westcentralus'
subscription='6a6fff00-4464-4eab-a6b1-0b533c7202e0'
resourcegroup='mir-agents'

if [ -z $1 ] || [ -z $2 ] || [ -z $3 ]; then
    echo "Using default location, subscription and resource group"
else
    location=$1
    subscription=$2
    resourcegroup=$3
fi

az configure -d location=$location subscription=$subscription group=$resourcegroup
echo location=$location subscription=$subscription resourcegroup=$resourcegroup
az account set -s $subscription
if [ $? != 0 ]; then
    exit 1
fi

username=$(whoami)
SkipAutoDeleteTill=$(date -d "+100 days" +"%Y-%m-%d")
az group create -n $resourcegroup --tags owner=$username SkipAutoDeleteTill=$SkipAutoDeleteTill
rgid=$(az group show -n $resourcegroup --query id -o tsv)

aoaiaccountname=$resourcegroup"-aoai-"$location
aoaimodeldepname="DRIAssistant"

az cognitiveservices account create --name $aoaiaccountname --kind OpenAI --sku s0
az cognitiveservices account deployment create --name $aoaiaccountname --deployment-name $aoaimodeldepname --model-name gpt-4o --model-version 2024-05-13 --model-format OpenAI --sku-capacity 10 --sku-name "Standard"