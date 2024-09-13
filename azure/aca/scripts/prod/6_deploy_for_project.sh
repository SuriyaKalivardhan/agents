#!/bin/bash
acaenvid=/subscriptions/921496dc-987f-410f-bd57-426eb2611356/resourceGroups/suriyak-customer3-chobov3/providers/Microsoft.App/managedEnvironments/suriyak-customer3-acaenv-2
internalingressip=10.1.0.200
customerumi=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-customer3/providers/Microsoft.ManagedIdentity/userAssignedIdentities/suriyak0customer3-umi

location='eastus2euap'
commonHoBoSubscription='921496dc-987f-410f-bd57-426eb2611356'

name=$(cut -d'/' -f9 <<<$acaenvid)-0
az group deployment create -f forproject.json  --parameters name=$name identity=$customerumi envid=$acaenvid location=$location
fqdn=$(az containerapp show -n $name  --query properties.configuration.ingress.fqdn  -o tsv)
echo internalingressip=$internalingressip
echo fqdn=$fqdn

#Run these in Platform Multitenant Subnet
#internalingressip=10.1.0.82
#fqdn=suriyak-customer3-acaenv-1-0.icywater-7ee6e26f.eastus2euap.azurecontainerapps.io
#curl $internalingressip/getips -H "HOST: $fqdn"
#curl $internalingressip/gettoken/$customerumiclientid -H "HOST: $fqdn"
#curl $internalingressip/getembeddings/$customerumiclientid/$customerstoragename/$customercontainer/$customerblob -H "HOST: $fqdn"


#Run these inside the ACA instance for sidecar access
#echo $IDENTITY_ENDPOINT
#echo $IDENTITY_HEADER
#customerumiclientid=1c72b109-734c-4c2f-be1d-4f014f3813b2
#curl "http://localhost:42356/msi/token?resource=https://vault.azure.net&api-version=2019-08-01&client_id=1c72b109-734c-4c2f-be1d-4f014f3813b2" -H "x-identity-header: 428f4729-655a-4f8b-ad52-5cfe8ebfa5d4"