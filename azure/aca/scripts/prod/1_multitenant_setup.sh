#az login --use-device-code
location='eastus2euap'
subscription='6a6fff00-4464-4eab-a6b1-0b533c7202e0'
resourcegroup='suriyak-platform'

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

prefix=$(echo $resourcegroup | tr '-' '0')
vnetname=$prefix"-vnet"
nsgname=$prefix"-nsg"
subnetname=$prefix"-subnet"
apivm=$prefix"-apivm"

az network vnet create -n $vnetname --address-prefix 10.0.0.0/16
platformnetwork=$(az network vnet show -n $vnetname --query id -o tsv)

az network nsg create -n $nsgname
az network nsg rule create --nsg-name $nsgname -n AllowCorpInbound --priority 4094  --access Allow --protocol Tcp --source-address-prefixes CorpNetPublic --destination-address-prefixes '*' --destination-port-ranges 22 80 8080 443 --direction Inbound
az network nsg rule create --nsg-name $nsgname -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $nsgname -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound

az network vnet subnet create --vnet-name $vnetname -n $subnetname --address-prefixes 10.0.0.0/24 --nsg $nsgname
subnetnameid=$(az network vnet subnet show --vnet-name $vnetname -n $subnetname --query id -o tsv)

#https://learn.microsoft.com/en-us/cli/azure/vm?view=azure-cli-latest#az-vm-create
az vm create -n $apivm --image Ubuntu2204 --admin-username $username --ssh-key-value ~/.ssh/id_rsa.pub --public-ip-sku Standard --nsg "" --subnet $subnetnameid
apipublicip=$(az network public-ip list | jq -r .[].ipAddress)
az vm extension set -n AzureMonitorLinuxAgent --publisher Microsoft.Azure.Monitor --version 1.0 --vm-name $apivm --enable-auto-upgrade true --settings '{"GCS_AUTO_CONFIG":true}'
az vm extension set -n AzureSecurityLinuxAgent --publisher Microsoft.Azure.Security.Monitoring --version 2.0 --vm-name $apivm --enable-auto-upgrade true --settings '{"enableGenevaUpload":true,"enableAutoConfig":true}'

echo location=$location
echo apipublicip=$apipublicip
echo platformnetwork=$platformnetwork