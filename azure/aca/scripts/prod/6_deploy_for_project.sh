#!/bin/bash
acaenvid=/subscriptions/921496dc-987f-410f-bd57-426eb2611356/resourceGroups/suriyak-customer3-chobov2/providers/Microsoft.App/managedEnvironments/suriyak-customer3-acaenv-4
customerumi=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-customer3/providers/Microsoft.ManagedIdentity/userAssignedIdentities/suriyak0customer3-umi

location='eastus2euap'
commonHoBoSubscription='921496dc-987f-410f-bd57-426eb2611356'

name=$(cut -d'/' -f9 <<<$acaenvid)-4
az group deployment create -f forproject.json  --parameters name=$name identity=$customerumi envid=$acaenvid location=$location

#Run these inside the ACA instance for sidecar access
#echo $IDENTITY_ENDPOINT
#echo $IDENTITY_HEADER
#customerumiclientid=1c72b109-734c-4c2f-be1d-4f014f3813b2
#curl "http://localhost:42356/msi/token?resource=https://vault.azure.net&api-version=2019-08-01&client_id=1c72b109-734c-4c2f-be1d-4f014f3813b2" -H "x-identity-header: 428f4729-655a-4f8b-ad52-5cfe8ebfa5d4"