#az login --use-device-code
location='eastus2euap'
subscription='ea4faa5b-5e44-4236-91f6-5483d5b17d14'
resourcegroup='suriyak-customer'

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

SkipAutoDeleteTill=$(date -d "+100 days" +"%Y-%m-%d")
az group create -n $resourcegroup --tags owner=suriyak@microsoft.com SkipAutoDeleteTill=$SkipAutoDeleteTill
rgid=$(az group show -n $resourcegroup --query id -o tsv)

vnetname="customer-vnet"
computensg="compute-nsg"
computesubnet="compute-subnet"
pensg="pe-nsg"
pesubnet="pe-subnet"
acansg="aca-nsg"
acasubnet="aca-subnet"

az network vnet create -n $vnetname --address-prefix 10.0.0.0/16

az network nsg create -n $computensg
az network nsg rule create --nsg-name $computensg -n AllowCorp --priority 4094  --access Allow --protocol Tcp --source-address-prefixes CorpNetPublic --destination-address-prefixes '*' --destination-port-ranges 22 --direction Inbound
az network nsg rule create --nsg-name $computensg -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $computensg -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound
computensgid=$(az network nsg show -n $computensg --query id -o tsv)

az network vnet subnet create --vnet-name $vnetname -n $computesubnet --address-prefixes 10.0.0.0/24 --nsg $computensgid
computesubnetid=$(az network vnet subnet show --vnet-name $vnetname -n $computesubnet --query id -o tsv)
echo $computesubnetid