#az login --use-device-code
#az provider register -n Microsoft.App
location='eastus'
#subscription='6a6fff00-4464-4eab-a6b1-0b533c7202e0'
#subscription='ea4faa5b-5e44-4236-91f6-5483d5b17d14'
subscription='921496dc-987f-410f-bd57-426eb2611356' #Experiments
#resourcegroup='eng-runners'
resourcegroup='agents-customer-rg'

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

uminame=$resourcegroup-umi
storagename=$(echo $resourcegroup"str" | tr '-' '0')
storagepename=$storagename"blobpe"
vnetname="customer-vnet"
computensg="compute-nsg"
computevm="customer-vm"
computesubnet="compute-subnet"
pensg="pe-nsg"
pesubnet="pe-subnet"
acansg="aca-nsg"
acasubnet="aca-subnet"

az identity create -n $uminame
umiid=$(az identity show -n $uminame --query id -o tsv)
umioid=$(az identity show -n $uminame --query principalId -o tsv)

az storage account create -n $storagename --public-network-access Disabled --allow-shared-key-access false
storageid=$(az storage account show -n $storagename --query id -o tsv)

az role assignment create --role "Storage Blob Data Contributor" --assignee-object-id $umioid --assignee-principal-type ServicePrincipal --scope $storageid
#az role assignment create --role "Contributor" --assignee-object-id $umioid --assignee-principal-type ServicePrincipal --scope $resourcegroup
az network vnet create -n $vnetname --address-prefix 10.0.0.0/16

az network nsg create -n $computensg
az network nsg rule create --nsg-name $computensg -n AllowInternetOutboundForPackage --priority 4090 --access Allow --protocol TCP --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges 80 443 --direction Outbound
#az network nsg rule delete --nsg-name $computensg -n AllowInternetOutboundForPackage
az network nsg rule create --nsg-name $computensg -n AllowARMOutbound --priority 4093  --access Allow --protocol Tcp --source-address-prefixes '*' --destination-address-prefixes AzureResourceManager --destination-port-ranges 80 443 --direction Outbound
az network nsg rule create --nsg-name $computensg -n AllowCorpInbound --priority 4094  --access Allow --protocol Tcp --source-address-prefixes CorpNetPublic --destination-address-prefixes '*' --destination-port-ranges 22 80 8080 443 --direction Inbound
az network nsg rule create --nsg-name $computensg -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $computensg -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound
computensgid=$(az network nsg show -n $computensg --query id -o tsv)

az network vnet subnet create --vnet-name $vnetname -n $computesubnet --address-prefixes 10.0.0.0/24 --nsg $computensgid
computesubnetid=$(az network vnet subnet show --vnet-name $vnetname -n $computesubnet --query id -o tsv)

#https://learn.microsoft.com/en-us/cli/azure/vm?view=azure-cli-latest#az-vm-create
username=$(whoami)
az vm create -n $computevm --image Ubuntu2204 --size Standard_F2_v2 --admin-username $username --ssh-key-value ~/.ssh/id_rsa.pub --public-ip-sku Standard --nsg "" --subnet $computesubnetid --assign-identity $umiid

az network nsg create -n $pensg
az network nsg rule create --nsg-name $pensg -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $pensg -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound
pensgid=$(az network nsg show -n $pensg --query id -o tsv)

az network vnet subnet create --vnet-name $vnetname -n $pesubnet --address-prefixes 10.0.1.0/24 --private-endpoint-network-policies Disabled --nsg $pensgid
pesubnetid=$(az network vnet subnet show --vnet-name $vnetname -n $pesubnet --query id -o tsv)

az network private-endpoint create -n $storagepename --vnet-name $vnetname --subnet $pesubnet --private-connection-resource-id $storageid --connection-name $storagepename --group-id blob
az network private-dns zone create --name privatelink.blob.core.windows.net
az network private-dns link vnet create --zone-name privatelink.blob.core.windows.net --name privatevnetlink --virtual-network $vnetname --registration-enabled false
az network private-endpoint dns-zone-group create --endpoint-name $storagepename --name myzonegroup --private-dns-zone privatelink.blob.core.windows.net --zone-name privatelink.blob.core.windows.net

az network nsg create -n $acansg
az network nsg rule create --nsg-name $acansg -n AllowCorpInbound --priority 4094  --access Allow --protocol Tcp --source-address-prefixes CorpNetPublic --destination-address-prefixes '*' --destination-port-ranges 22 80 8080 443 --direction Inbound
az network nsg rule create --nsg-name $acansg -n DenyInternetInbound --priority 4095  --access Deny --protocol '*' --source-address-prefixes Internet --destination-address-prefixes '*' --destination-port-ranges '*' --direction Inbound
az network nsg rule create --nsg-name $acansg -n DenyInternetOutbound --priority 4096  --access Deny --protocol '*' --source-address-prefixes '*' --destination-address-prefixes Internet --destination-port-ranges '*' --direction Outbound
acansgid=$(az network nsg show -n $acansg --query id -o tsv)

az network vnet subnet create --vnet-name $vnetname -n $acasubnet --address-prefixes 10.0.2.0/24 --nsg $acansg --delegations Microsoft.App/environments
acasubnetid=$(az network vnet subnet show --vnet-name $vnetname -n $acasubnet --query id -o tsv)

acaenvname=$resourcegroup"-env"
acaname=$resourcegroup"-aca"
az group deployment create -f env.json --parameters name=$acaenvname identity=$umiid infrasubnetid=$acasubnetid location=$location
acaenvid=$(az containerapp env show -n suriyakenv1 --query id -o tsv)
az group deployment create -f aca.json --parameters name=$acaname identity=$umiid envid=$acaenvid location=$location