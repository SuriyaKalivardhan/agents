#!/bin/bash
platformnetwork=/subscriptions/6a6fff00-4464-4eab-a6b1-0b533c7202e0/resourceGroups/suriyak-platform/providers/Microsoft.Network/virtualNetworks/suriyak0platform-vnet
customeracasubnetid=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourceGroups/suriyak-customer3/providers/Microsoft.Network/virtualNetworks/suriyak0customer3-vnet/subnets/suriyak0customer3-aca-subnet

location='eastus2euap'
commonHoBoSubscription='921496dc-987f-410f-bd57-426eb2611356'
platcustomersubnetcidr='10.4.0.0/24'

platformsub=$(cut -d'/' -f3 <<<$platformnetwork)
platformrg=$(cut -d'/' -f5 <<<$platformnetwork)
platformvnet=$(cut -d'/' -f9 <<<$platformnetwork)
customersubnetname=suriyak0customer3-aca-subnet4
#$(cut -d'/' -f11 <<<$customeracasubnetid)
platformcustomernsg=$customersubnetname-nsg
platcustomersubnetid=$platformnetwork/subnets/$customersubnetname
commonHoBoResourcegroup=$(cut -d'/' -f5 <<<$customeracasubnetid)-chobov2
acaenvname=$(cut -d'/' -f5 <<<$customeracasubnetid)-acaenv-4

if [ -z $1 ] || [ -z $2 ] || [ -z $3 ]; then
    echo "Using default location, subscription and resource group"
else
    location=$1
    commonHoBoSubscription=$2
    commonHoBoResourcegroup=$3
fi

az configure -d location=$location subscription=$commonHoBoSubscription group=$commonHoBoResourcegroup
echo location=$location subscription=$commonHoBoSubscription resourcegroup=$commonHoBoResourcegroup
az account set -s $commonHoBoSubscription
if [ $? != 0 ]; then
    exit 1
fi

username=$(whoami)
SkipAutoDeleteTill=$(date -d "+100 days" +"%Y-%m-%d")
az group create -n $commonHoBoResourcegroup --tags owner=$username SkipAutoDeleteTill=$SkipAutoDeleteTill
rgid=$(az group show -n $commonHoBoResourcegroup --query id -o tsv)

az group deployment create -f forhub.json --parameters platformsub=$platformsub platformrg=$platformrg platformvnet=$platformvnet platcustomersubnet=$customersubnetname name=$customersubnetname platcustomersubnetcidr=$platcustomersubnetcidr platformcustomernsg=$platformcustomernsg location=$location infrasubnetid=$platcustomersubnetid customersubnetid=$customeracasubnetid name=$acaenvname
acaenvid=$(az containerapp env show --subscription $commonHoBoSubscription -g $commonHoBoResourcegroup -n $acaenvname --query id -o tsv)
internalingressip=$(az containerapp env show --subscription $commonHoBoSubscription -g $commonHoBoResourcegroup -n $acaenvname --query properties.internalIngressIp -o tsv)
echo acaenvid=$acaenvid
echo internalingressip=$internalingressip