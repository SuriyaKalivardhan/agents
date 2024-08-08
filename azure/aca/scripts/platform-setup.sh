#az login --use-device-code

customerumi=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-a-customer0/providers/Microsoft.ManagedIdentity/userAssignedIdentities/customer-umi
customeracasubnet=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourceGroups/suriyak-a-customer0/providers/Microsoft.Network/virtualNetworks/customer-vnet/subnets/customer-aca-subnet

location='eastus2euap'
subscription='6a6fff00-4464-4eab-a6b1-0b533c7202e0'
resourcegroup='suriyak-a-platform'

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

vnetname="platform-vnet"
acansg0="platform-aca-nsg0"
acasubnet0="platform-aca-subnet0"

az network vnet create -n $vnetname --address-prefix 10.0.0.0/16

az network nsg create -n $acansg0
az network nsg rule create --nsg-name $acansg0 -n AllowCorpInbound --priority 4094  --access Allow --protocol Tcp --source-address-prefixes CorpNetPublic --destination-address-prefixes '*' --destination-port-ranges 22 80 8080 443 --direction Inbound
az network nsg rule create --nsg-name $acansg0 -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $acansg0 -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound

az network vnet subnet create --vnet-name $vnetname -n $acasubnet0 --address-prefixes 10.0.3.0/24 --nsg $acansg0 --delegations Microsoft.App/environments
acasubnet0id=$(az network vnet subnet show --vnet-name $vnetname -n $acasubnet0 --query id -o tsv)

echo location=$location
echo plat-subnet-cust0=$acasubnet0id

acaenvname0=$resourcegroup"-env0"
acaname0=$resourcegroup"-aca0"
az group deployment create -f env.json --parameters name=$acaenvname0 infrasubnetid=$acasubnet0id location=$location customersubnetid=$customeracasubnet
acaenv0id=$(az containerapp env show -n $acaenvname0 --query id -o tsv)
az group deployment create -f aca.json --parameters name=$acaname0 identity=$customerumi envid=$acaenv0id location=$location